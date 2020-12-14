﻿using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions.Common;
using Namotion.Reflection;
using Reductech.EDR.Core.Attributes;
using Reductech.EDR.Core.Entities;
using Reductech.EDR.Core.Internal;
using Reductech.EDR.Core.Parser;
using Reductech.EDR.Core.Serialization;
using Reductech.EDR.Core.Steps;
using Reductech.EDR.Core.Util;
using Xunit.Sdk;
using Task = System.Threading.Tasks.Task;
using static Reductech.EDR.Core.TestHarness.StaticHelpers;

namespace Reductech.EDR.Core.TestHarness
{
    public abstract partial class StepTestBase<TStep, TOutput>
    {
        protected static async Task<(TStep step, Dictionary<string, string> values)> CreateStepWithDefaultOrArbitraryValuesAsync()
        {
            var instance = new TStep();

            var index = 0;

            var values = new Dictionary<string, string>();

            foreach (var propertyInfo in typeof(TStep).GetProperties()
                .Select(propertyInfo => (propertyInfo,
                        attribute: propertyInfo.GetCustomAttribute<StepPropertyBaseAttribute>()))
                    .Where(x => x.attribute != null)
                    .OrderBy(x => x.attribute!.Order)
                .Select(x=>x.propertyInfo)
            )
                await MatchStepPropertyInfoAsync(propertyInfo, SetVariableName, SetStep, SetStepList);

            return (instance, values);


            Task SetVariableName(PropertyInfo property)
            {
                var vn = new VariableName("Foo" + index);
                index++;
                property.SetValue(instance, vn);
                values.Add(property.Name, $"<{vn.Name}>");
                return Task.CompletedTask;
            }

            async Task SetStep(PropertyInfo property)
            {
                var currentValue = property.GetValue(instance);
                if (currentValue == null)
                {
                    var (step, value, newIndex) = await CreateSimpleStep(property.PropertyType, index);
                    index = newIndex;
                    values.Add(property.Name, value);

                    try
                    {
                        property.SetValue(instance, step);
                    }
                    catch (Exception e)
                    {
                        Console.WriteLine(e);
                        throw;
                    }
                }
                else
                {
                    var s = await GetStringAsync((IStep) currentValue);
                    values.Add(property.Name, s);
                }

            }

            async Task SetStepList(PropertyInfo property)
            {
                if (property.GetValue(instance) is IReadOnlyList<IStep> currentValue)
                {
                    var elements = new List<string>();

                    foreach (var step in currentValue)
                    {
                        var s = await GetStringAsync(step);
                        elements.Add(s);
                    }


                    var currentValueString = CreateArrayString(elements
                            );

                    values.Add(property.Name, currentValueString);
                }
                else
                {
                    var newValue = CreateStepListOfType(property.PropertyType, 3, ref index);
                    var elements = new List<string>();

                    foreach (var step in newValue)
                    {
                        var e = await GetStringAsync(step);
                        elements.Add(e);
                    }

                    var newValueString = CreateArrayString(elements);

                    values.Add(property.Name, newValueString);
                    property.SetValue(instance, newValue);
                }

                static string CreateArrayString(IEnumerable<string> elements)
                {
                    return $"[{string.Join(", ", elements)}]";
                }
            }
        }

        private static async Task<string> GetStringAsync(IStep step)
        {
            var freezable = step.Unfreeze();

            if (freezable is IConstantFreezableStep cfs)
            {

                return await cfs.SerializeAsync(CancellationToken.None);
            }

            else if (step is DoNothing)
            {
                return DoNothingStepFactory.Instance.TypeName;
            }
            else if (freezable is CompoundFreezableStep cs && freezable.StepName == ArrayStepFactory.Instance.TypeName)
            {

                var constants = cs.FreezableStepData
                    .StepProperties[new StepParameterReference(nameof(Steps.Array<object>.Elements))]
                    .StepList.Value.Cast<IConstantFreezableStep>();

                var list = new List<string>();

                foreach (var constantFreezableStep in constants)
                {
                    var s = await constantFreezableStep.SerializeAsync(CancellationToken.None);
                    list.Add(s);
                }

                return SerializationMethods.SerializeList(list);
            }

            throw new NotImplementedException("Cannot get string from step");
        }

        private static async Task MatchStepPropertyInfoAsync(PropertyInfo stepPropertyInfo,
            Func<PropertyInfo, Task> variableNameAction,
            Func<PropertyInfo, Task> stepPropertyAction,
            Func<PropertyInfo, Task> stepListAction)
        {
            var actionsToDo = new List<Func<PropertyInfo, Task>>();

            if (stepPropertyInfo.IsDecoratedWith<VariableNameAttribute>())
                actionsToDo.Add(variableNameAction);

            if (stepPropertyInfo.IsDecoratedWith<StepPropertyAttribute>())
                actionsToDo.Add(stepPropertyAction);

            if (stepPropertyInfo.IsDecoratedWith<StepListPropertyAttribute>())
                actionsToDo.Add(stepListAction);

            switch (actionsToDo.Count)
            {
                case 0: throw new XunitException($"{stepPropertyInfo.Name} does not have a valid attribute");
                case 1: await actionsToDo.Single()(stepPropertyInfo);
                    return;
                default:
                    throw new XunitException(
                        $"{stepPropertyInfo.Name} has more than one step property base attribute");
            }
        }

        private static void MatchStepPropertyInfo(PropertyInfo stepPropertyInfo,
            Action<PropertyInfo> variableNameAction,
            Action<PropertyInfo> stepPropertyAction,
            Action<PropertyInfo> stepListAction)
        {
            var actionsToDo = new List<Action<PropertyInfo>>();

            if (stepPropertyInfo.IsDecoratedWith<VariableNameAttribute>())
                actionsToDo.Add(variableNameAction);

            if (stepPropertyInfo.IsDecoratedWith<StepPropertyAttribute>())
                actionsToDo.Add(stepPropertyAction);

            if (stepPropertyInfo.IsDecoratedWith<StepListPropertyAttribute>())
                actionsToDo.Add(stepListAction);

            switch (actionsToDo.Count)
            {
                case 0: throw new XunitException($"{stepPropertyInfo.Name} does not have a valid attribute");
                case 1:
                     actionsToDo.Single()(stepPropertyInfo);
                    return;
                default:
                    throw new XunitException(
                        $"{stepPropertyInfo.Name} has more than one step property base attribute");
            }
        }

        private static async Task<(IStep step, string value, int newIndex)> CreateSimpleStep(Type tStep, int index)
        {
            var outputType = tStep.GenericTypeArguments.First();
            IStep step;

            if (outputType == typeof(Unit))
            {
                step = new DoNothing();
            }

            else if (outputType == typeof(StringStream))
            {
                var s = "Bar" + index;
                index++;
                step = Constant(s);
            }
            else if (outputType == typeof(string))
            {
                throw new Exception($"{tStep.Name} should not have output type 'String' - it should be 'StringStream'");
            }

            else if (outputType == typeof(bool))
            {
                var b = true;
                step =  Constant(b);
            }
            else if (outputType == typeof(int))
            {
                var i = index;
                index++;
                step =  Constant(i);
            }

            else if (outputType == typeof(double))
            {
                double d = index;
                index++;
                step =  Constant(d);
            }

            else if (outputType == typeof(List<StringStream>))
            {
                var list = new List<string>();
                for (var i = 0; i < 3; i++)
                {
                    list.Add("Foo" + index);
                    index++;
                }

                step = Array(list.ToArray());
            }
            else if (outputType == typeof(List<int>))
            {
                var list = new List<int>();
                for (var i = 0; i < 3; i++)
                {
                    list.Add(index);
                    index++;
                }

                step =  Array(list.ToArray());
            }
            else if (outputType == typeof(List<EntityStream>))
            {
                var entityStreamList = new List<IStep<EntityStream> >
                {
                    Constant(CreateSimpleEntityStream(ref index)),
                    Constant(CreateSimpleEntityStream(ref index)),
                    Constant(CreateSimpleEntityStream(ref index))
                };


                step = new Array<EntityStream>(){Elements = entityStreamList};
            }

            else if (outputType.IsEnum)
            {
                var v = Enum.GetValues(outputType).OfType<object>().First();

                step = EnumConstantFreezable.TryCreateEnumConstant(v).Value;
            }

            else if (outputType == typeof(Stream))
            {
                throw new Exception($"{tStep.Name} should not have output type 'Stream' - it should be 'StringStream'");
                //var s = "Baz" + index;
                //index++;

                //byte[] byteArray = Encoding.UTF8.GetBytes(s);
                //Stream stream = new MemoryStream(byteArray); //special case so we don't read the stream early

                //step = Constant(stream);
                //var asString = await GetStringAsync(Constant(s));

                //return (step, asString, index);
            }
            else if (outputType == typeof(Entity))
            {
                var entity = CreateSimpleEntity(ref index);

                step = Constant(entity);
            }
            else if (outputType == typeof(EntityStream))
            {
                var entityStream = CreateSimpleEntityStream(ref index);

                step = Constant(entityStream);
            }
            else if (outputType == typeof(StringStream))
            {
                var s = "DataStream" + index;
                index++;

                step = new StringConstant(new StringStream(s));
            }
            else if (outputType == typeof(Schema))
            {
                throw new Exception($"{tStep.Name} should not have output type 'Schema' - it should be 'Entity'");
                //var schema = new Schema
                //{
                //    Name = "Schema" + index,
                //    Properties = new Dictionary<string, SchemaProperty>()
                //};
                //index++;
                //schema.Properties.Add("MyProp" + index, new SchemaProperty{Multiplicity = Multiplicity.Any, Type = SchemaPropertyType.Integer});
                //index++;
                //step = new Constant<Schema>(schema);
            }
            else
                throw new XunitException($"Cannot create a constant step with type {outputType.GetDisplayName()}");

            var newString = await GetStringAsync(step);

            return (step, newString, index);


            static EntityStream CreateSimpleEntityStream(ref int index1)
            {
                var entityList = new List<Entity>
                {
                    CreateSimpleEntity(ref index1), CreateSimpleEntity(ref index1), CreateSimpleEntity(ref index1)
                };

                var entityStream = EntityStream.Create(entityList);

                return entityStream;
            }

            static Entity CreateSimpleEntity(ref int index1)
            {
                var pairs = new List<(string, object)>
                {
                    ("Prop1", $"Val{index1}")
                };

                index1++;
                pairs.Add(("Prop2", $"Val{index1}"));
                index1++;

                var entity = Entity.Create(pairs);
                return entity;
            }
        }


        private static IEnumerable<IStep> CreateStepListOfType(Type tList, int numberOfElements, ref int index)
        {
            var stepType = tList.GenericTypeArguments.First();
            var outputType = stepType.GenericTypeArguments.First();

            var listType = typeof(List<>).MakeGenericType(stepType);
            var list = Activator.CreateInstance(listType)!;

            var addMethod = list.GetType().GetMethod(nameof(List<object>.Add))!;

            Func<int, object> getElement;


            if (outputType == typeof(Unit)) getElement = _ => new DoNothing();


            else if (outputType == typeof(string)) getElement = i => Constant("Bar" + i);

            else if (outputType == typeof(bool)) getElement = i => Constant(i % 2 == 0);
            else if (outputType == typeof(int)) getElement = Constant;
            else if (outputType == typeof(Entity)) getElement = i => Constant(CreateEntity(("Foo", $"Val + {i}")));
            else throw new XunitException($"Cannot create default value for {outputType.GetDisplayName()} List");

            for (var i = 0; i < numberOfElements; i++)
            {
                var e = getElement(index);
                index++;
                addMethod.Invoke(list, new[]{e});
            }

            return (IEnumerable<IStep>) list;

        }
    }
}
