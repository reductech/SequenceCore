﻿using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using CSharpFunctionalExtensions;
using Reductech.EDR.Core.Internal;
using Reductech.EDR.Core.Internal.Errors;
using Reductech.EDR.Core.Parser;
using Reductech.EDR.Core.Serialization;

using Option = OneOf.OneOf<System.DBNull, string, int, double, bool, Reductech.EDR.Core.Internal.Enumeration, System.DateTime, Reductech.EDR.Core.Entity, System.Collections.Immutable.ImmutableList<Reductech.EDR.Core.Entities.EntityValue>>;


namespace Reductech.EDR.Core.Entities
{
    /// <summary>
    /// The value of an entity property.
    /// </summary>
    public sealed class EntityValue : IEquatable<EntityValue>
    {
        /// <summary>
        /// Create a new entityValue
        /// </summary>
        /// <param name="value"></param>
        public EntityValue(Option value) => Value = value;

        /// <summary>
        /// The Value
        /// </summary>
        public Option Value { get; }


        ///// <summary>
        ///// Create a new EntityValue from an original string.
        ///// </summary>
        //public static EntityValue Create(string? original, char? multiValueDelimiter = null)
        //{
        //    if(string.IsNullOrWhiteSpace(original))
        //        return new EntityValue(DBNull.Value);

        //    if (multiValueDelimiter.HasValue)
        //    {
        //        var splits = original.Split(multiValueDelimiter.Value);
        //        if (splits.Length > 1)
        //        {
        //            var values = splits.Select(EntitySingleValue.Create).ToList();
        //            return new EntityValue(values);
        //        }
        //    }

        //    return new EntityValue(EntitySingleValue.Create(original));
        //}

        /// <summary>
        /// Create an entity from an object
        /// </summary>
        public static EntityValue CreateFromObject(object? argValue)
        {
            if (argValue == null) //TODO convert to switch statement (this is not supported by stryker at the moment)
            {
                return new EntityValue(DBNull.Value);
            }
            else if (argValue is StringStream ss1)
            {
                return new EntityValue(ss1.GetString());
            }
            else if (argValue is string s)
            {
                return new EntityValue(s);
            }
            else if (argValue is int i)
            {
                return new EntityValue(i);
            }
            else if (argValue is bool b)
            {
                return new EntityValue(b);
            }
            else if (argValue is double d)
            {
                return new EntityValue(d);
            }
            else if (argValue is Enumeration e1)
            {
                return new EntityValue(e1);
            }
            else if (argValue is DateTime dt)
            {
                return new EntityValue(dt);
            }
            else if (argValue is Entity entity)
            {
                return new EntityValue(entity);
            }
            else if (argValue is IEnumerable e2)
            {
                var newEnumerable = e2.Cast<object>().Select(CreateFromObject).ToImmutableList();
                if(!newEnumerable.Any())
                    return new EntityValue(DBNull.Value);
                return new EntityValue(newEnumerable);
            }
            else if (argValue is IResult)
            {
                throw new ArgumentException(
                    "Attempt to set EntityValue to a Result - you should check the result for failure and then set it to the value of the result",
                    nameof(argValue));
            }
            else
            {
                return new EntityValue(argValue.ToString()!);
            }
        }





        /// <summary>
        /// Tries to convert the value so it matches the schema.
        /// </summary>
        public Result<EntityValue, IErrorBuilder> TryConvert(SchemaProperty schemaProperty)
        {

            var r = Value.Match(MatchDbNull,
                MatchString, MatchInt, MatchDouble, MatchBool, MatchEnum,
                MatchDateTime, MatchEntity, MatchList



            );


            return r;

            static ErrorBuilder CreateErrorBuilder(object o, SchemaPropertyType schemaPropertyType)
            {
                return new ErrorBuilder($"Could not convert '{o}' to '{schemaPropertyType}'.", ErrorCode.SchemaViolation);
            }

            Result<EntityValue, IErrorBuilder> MatchDbNull(DBNull dbNull)
            {
                if (schemaProperty.Multiplicity == Multiplicity.Any ||
                            schemaProperty.Multiplicity == Multiplicity.UpToOne)
                    return Result.Success<EntityValue, IErrorBuilder>(this);
                return new ErrorBuilder("Unexpected null", ErrorCode.SchemaViolation);
            }

            Result<EntityValue, IErrorBuilder> MatchString(string s)
            {
                switch (schemaProperty.Type)
                {
                    case SchemaPropertyType.String: return this;
                    case SchemaPropertyType.Integer:
                    {
                        if (int.TryParse(s, out var i))
                            return new EntityValue(i);
                        break;
                    }
                    case SchemaPropertyType.Double:
                        {
                            if (double.TryParse(s, out var d))
                                return new EntityValue(d);
                            break;
                        }
                    case SchemaPropertyType.Enum:
                        {
                            throw new NotImplementedException();
                            if (int.TryParse(s, out var i))
                                return new EntityValue(i);
                            break;
                        }
                    case SchemaPropertyType.Bool:
                        {
                            if (bool.TryParse(s, out var b))
                                return new EntityValue(b);
                            break;
                        }
                    case SchemaPropertyType.Date:
                        {
                            if (DateTime.TryParse(s, out var dt)) //TODO format
                                return new EntityValue(dt);
                            break;
                        }
                    case SchemaPropertyType.Entity:
                        break;
                    default:
                        throw new ArgumentOutOfRangeException();
                }

                return CreateErrorBuilder(s, schemaProperty.Type);
            }

            Result<EntityValue, IErrorBuilder> MatchInt(int i)
            {
            }

            Result<EntityValue, IErrorBuilder> MatchDouble(double d)
            {
            }

            Result<EntityValue, IErrorBuilder> MatchBool(bool b)
            {
            }

            Result<EntityValue, IErrorBuilder> MatchEnum(Enumeration e)
            {
            }

            Result<EntityValue, IErrorBuilder> MatchDateTime(DateTime dt)
            {
            }
            Result<EntityValue, IErrorBuilder> MatchEntity(Entity e)
            {
            }

            Result<EntityValue, IErrorBuilder> MatchList(ImmutableList<EntityValue> list)
            {
            }


            //var r =

            //    Value.Match(_ =>
            //        {



            //        },
            //        singleValue =>
            //        {
            //            if (singleValue.Type == schemaProperty.Type && singleValue.Obeys(schemaProperty))
            //                return this;
            //            return singleValue.TryConvert(schemaProperty).Map(x => new EntityValue(x));
            //        },
            //        multiValue =>
            //        {
            //            if (schemaProperty.Multiplicity == Multiplicity.Any || schemaProperty.Multiplicity == Multiplicity.AtLeastOne)
            //            {
            //                if(multiValue.All(x=>x.Type == schemaProperty.Type && x.Obeys(schemaProperty)))
            //                    return Result.Success<EntityValue, IErrorBuilder>(this);

            //                var result = multiValue.Select(x=>x.TryConvert(schemaProperty))
            //                    .Combine(ErrorBuilderList.Combine)
            //                    .Map(x => new EntityValue(x.ToList()));

            //                return result;
            //            }

            //            return new ErrorBuilder("Unexpected list", ErrorCode.SchemaViolation);
            //        });

            //return r;
        }

        /// <inheritdoc />
        public bool Equals(EntityValue? other)
        {
            return !(other is null)  && Value.Equals(other.Value);
        }

        /// <inheritdoc />
        public override bool Equals(object? obj)
        {
            if (obj is null) return false;
            if (ReferenceEquals(this, obj)) return true;

            return obj is EntityValue ev && Equals(ev);
        }

        /// <inheritdoc />
        public override int GetHashCode()
        {
            return Value.Match(_ => 0,
                v => v.Value.GetHashCode(),
                l => l.Count switch
                {
                    0 => 0,
                    1 => l.Single().GetHashCode(),
                    _ => HashCode.Combine(l.Count, l.First())
                }
            );
        }

        /// <inheritdoc />
        public override string ToString()
        {
            return Value.Match(x => "Empty", x => x.ToString(), x => string.Join(", ", x));
        }

        /// <summary>
        /// Serialize this EntityValue
        /// </summary>
        /// <returns></returns>
        public string Serialize()
        {
            return
            Value.Match(_=> "", x=>
                x.Serialize(),
                x=>
                    SerializationMethods.SerializeList(x.Select(y=>y.Serialize())));
        }
    }
}