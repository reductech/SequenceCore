﻿using System.Collections.Generic;
using Reductech.EDR.Core.Entities;
using Reductech.EDR.Core.Steps;
using Reductech.EDR.Core.TestHarness;
using Reductech.EDR.Core.Util;
using Xunit.Abstractions;

namespace Reductech.EDR.Core.Tests.Steps
{
    public class SelectTests : StepTestBase<EntityMap, EntityStream>
    {
        /// <inheritdoc />
        public SelectTests(ITestOutputHelper testOutputHelper) : base(testOutputHelper) {}

        /// <inheritdoc />
        protected override IEnumerable<StepCase> StepCases
        {
            get
            {
                yield return new StepCase("Add property",
                    new EntityForEach
                    {
                        Action  = new Print<Entity> {Value = GetEntityVariable},

                        EntityStream = new EntityMap
                        {
                            EntityStream = Constant(EntityStream.Create(
                            CreateEntity(("Foo", "Hello")),
                            CreateEntity(("Foo", "Hello 2")))),

                            Function = new SetProperty<string>
                            {
                                Entity = GetEntityVariable,
                                Property = Constant("Bar"),
                                Value = Constant("World")
                            }
                        }
                    }, Unit.Default,
                    "Foo: Hello, Bar: World",
                    "Foo: Hello 2, Bar: World");

                yield return new StepCase("Change property",
                    new EntityForEach
                    {
                        Action = new Print<Entity> { Value = GetEntityVariable },

                        EntityStream = new EntityMap
                        {
                            EntityStream = Constant(EntityStream.Create(
                            CreateEntity(("Foo", "Hello"), ("Bar", "Earth")),
                            CreateEntity(("Foo", "Hello 2"), ("Bar", "Earth")))),

                            Function = new SetProperty<string>
                            {
                                Entity = GetEntityVariable,
                                Property = Constant("Bar"),
                                Value = Constant("World")
                            }
                        }
                    }, Unit.Default,
                    "Foo: Hello, Bar: World",
                    "Foo: Hello 2, Bar: World");

            }
        }

        /// <inheritdoc />
        protected override IEnumerable<SerializeCase> SerializeCases
        {
            get
            {
                yield return new SerializeCase("Default",
                    CreateStepWithDefaultOrArbitraryValues().step,
                    @"Do: EntityMap
EntityStream:
- (Prop1 = 'Val0',Prop2 = 'Val1')
- (Prop1 = 'Val2',Prop2 = 'Val3')
- (Prop1 = 'Val4',Prop2 = 'Val5')
Function: (Prop1 = 'Val6',Prop2 = 'Val7')"

                    );

            }
        }

    }
}