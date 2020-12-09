﻿using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using CSharpFunctionalExtensions;
using OneOf;
using Reductech.EDR.Core.Internal;
using Reductech.EDR.Core.Internal.Errors;
using Reductech.EDR.Core.Serialization;

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
        public EntityValue(OneOf<DBNull, EntitySingleValue, IReadOnlyCollection<EntitySingleValue>> value) => Value = value;

        /// <summary>
        /// Create a new EntityValue from an original string.
        /// </summary>
        public static EntityValue Create(string? original, char? multiValueDelimiter = null)
        {
            if(string.IsNullOrWhiteSpace(original))
                return new EntityValue(DBNull.Value);

            if (multiValueDelimiter.HasValue)
            {
                var splits = original.Split(multiValueDelimiter.Value);
                if (splits.Length > 1)
                {
                    var values = splits.Select(EntitySingleValue.Create).ToList();
                    return new EntityValue(values);
                }
            }

            return new EntityValue(EntitySingleValue.Create(original));
        }

        /// <summary>
        /// Create an entity from an object
        /// </summary>
        public static EntityValue CreateFromObject(object? argValue, char? multiValueDelimiter = null)
        {
            switch (argValue)
            {
                case null: return new EntityValue(DBNull.Value);
                case string s: return Create(s, multiValueDelimiter);
                case int i: return new EntityValue(new EntitySingleValue(i, argValue.ToString()!));
                case bool b: return new EntityValue(new EntitySingleValue(b, argValue.ToString()!));
                case double d: return new EntityValue(new EntitySingleValue(d, argValue.ToString()!));
                case Enumeration e: return new EntityValue(new EntitySingleValue(e, argValue.ToString()!));
                case DateTime dt: return new EntityValue(new EntitySingleValue(dt, argValue.ToString()!));
                case Entity entity: return new EntityValue(new EntitySingleValue(entity, entity.ToString()));
                case IEnumerable e:
                {
                    var newEnumerable  = e.Cast<object>().Select(x=>x.ToString()!);
                    return Create(newEnumerable);
                }
                default:
                    return Create(argValue.ToString(), multiValueDelimiter);
            }
        }



        /// <summary>
        /// Create a new EntityValue from some number of original strings.
        /// </summary>
        public static EntityValue Create(IEnumerable<string> originals)
        {
            var values = originals.Select(EntitySingleValue.Create).ToList();

            return values.Count switch
            {
                0 => new EntityValue(DBNull.Value),
                1 => new EntityValue(values.Single()),
                _ => new EntityValue(values)
            };
        }

        /// <summary>
        /// The Value
        /// </summary>
        public OneOf<DBNull, EntitySingleValue, IReadOnlyCollection<EntitySingleValue>> Value { get; }

        /// <summary>
        /// Tries to convert the value so it matches the schema.
        /// </summary>
        public Result<EntityValue, IErrorBuilder> TryConvert(SchemaProperty schemaProperty)
        {
            var r =

                Value.Match(_ =>
                    {
                        if (schemaProperty.Multiplicity == Multiplicity.Any ||
                            schemaProperty.Multiplicity == Multiplicity.UpToOne)
                            return Result.Success<EntityValue, IErrorBuilder>(this);
                        return new ErrorBuilder("Unexpected null", ErrorCode.SchemaViolation);


                    },
                    singleValue =>
                    {
                        if (singleValue.Type == schemaProperty.Type && singleValue.Obeys(schemaProperty))
                            return this;
                        return singleValue.TryConvert(schemaProperty).Map(x => new EntityValue(x));
                    },
                    multiValue =>
                    {
                        if (schemaProperty.Multiplicity == Multiplicity.Any || schemaProperty.Multiplicity == Multiplicity.AtLeastOne)
                        {
                            if(multiValue.All(x=>x.Type == schemaProperty.Type && x.Obeys(schemaProperty)))
                                return Result.Success<EntityValue, IErrorBuilder>(this);

                            var result = multiValue.Select(x=>x.TryConvert(schemaProperty))
                                .Combine(ErrorBuilderList.Combine)
                                .Map(x => new EntityValue(x.ToList()));

                            return result;
                        }

                        return new ErrorBuilder("Unexpected list", ErrorCode.SchemaViolation);
                    });

            return r;
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