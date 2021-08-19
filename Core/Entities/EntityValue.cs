﻿using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Globalization;
using System.Linq;
using System.Reflection;
using System.Runtime.Serialization;
using System.Text.RegularExpressions;
using CSharpFunctionalExtensions;
using Newtonsoft.Json.Linq;
using OneOf;
using Reductech.EDR.Core.Internal;
using Reductech.EDR.Core.Internal.Errors;
using Reductech.EDR.Core.Internal.Serialization;
using Reductech.EDR.Core.Util;

namespace Reductech.EDR.Core.Entities
{

/// <summary>
/// The value of an entity property.
/// </summary>
public abstract record EntityValue(object? ObjectValue)
{
    /// <summary>
    /// The Null value
    /// </summary>
    public record Null : EntityValue
    {
        private Null() : base(null as object) { }

        /// <summary>
        /// The instance
        /// </summary>
        public static Null Instance { get; } = new();

        /// <inheritdoc />
        protected override Result<(EntityValue value, bool changed), IErrorBuilder> TryConvertBase(
            Schema schema,
            string propertyName,
            SchemaProperty schemaProperty,
            Entity entity)
        {
            if (schemaProperty.Multiplicity is Multiplicity.Any or Multiplicity.UpToOne)
                return (this, false);

            return schemaProperty.Type switch
            {
                SCLType.String => (new String(Serialized), true),
                _ => ErrorCode.SchemaViolationUnexpectedNull.ToErrorBuilder(propertyName, entity)
            };
        }

        /// <inheritdoc />
        public override string GetPrimitiveString() => "";

        /// <inheritdoc />
        public override string Serialize() => Serialized;

        private static readonly string Serialized = SerializationMethods.DoubleQuote("");

        /// <inheritdoc />
        public override string GetFormattedString(
            char delimiter,
            string dateTimeFormat) => "";

        /// <inheritdoc />
        protected override Maybe<object> AsType(Type type)
        {
            return Maybe<object>.None;
        }

        /// <inheritdoc />
        public override Result<Maybe<SchemaProperty>, IErrorBuilder> TryCreateSchemaProperty(
            string propertyName)
        {
            return Maybe<SchemaProperty>.None;
        }
    }

    /// <summary>
    /// A string value
    /// </summary>
    public record String(string Value) : EntityValue(Value)
    {
        /// <inheritdoc />
        public override string GetPrimitiveString()
        {
            return Value;
        }

        /// <inheritdoc />
        public override string Serialize()
        {
            return SerializationMethods.DoubleQuote(Value);
        }

        /// <inheritdoc />
        public override string GetFormattedString(
            char delimiter,
            string dateTimeFormat) => Value;

        /// <inheritdoc />
        protected override Maybe<object> AsType(Type type)
        {
            if (type == typeof(string))
                return Value;

            if (type == typeof(StringStream) || type == typeof(object))
                return new StringStream(Value);

            return Maybe<object>.None;
        }

        /// <inheritdoc />
        protected override Result<(EntityValue value, bool changed), IErrorBuilder> TryConvertBase(
            Schema schema,
            string propertyName,
            SchemaProperty schemaProperty,
            Entity entity)
        {
            switch (schemaProperty.Type)
            {
                case SCLType.String: return (this, false);
                case SCLType.Integer:
                {
                    if (int.TryParse(Value, out var i))
                        return (new Integer(i), true);

                    break;
                }
                case SCLType.Double:
                {
                    if (double.TryParse(Value, out var d))
                        return (new Double(d), true);

                    break;
                }
                case SCLType.Enum:
                {
                    if (string.IsNullOrWhiteSpace(schemaProperty.EnumType))
                        return new ErrorBuilder(ErrorCode.SchemaInvalidMissingEnum);

                    if (schemaProperty.Values == null || !schemaProperty.Values.Any())
                        return new ErrorBuilder(ErrorCode.SchemaInvalidNoEnumValues);

                    if (schemaProperty.Values.Contains(Value, StringComparer.OrdinalIgnoreCase))
                        return (
                            new EnumerationValue(new Enumeration(schemaProperty.EnumType, Value)),
                            true);

                    break;
                }
                case SCLType.Bool:
                {
                    if (bool.TryParse(Value, out var b))
                        return (new Boolean(b), true);

                    break;
                }
                case SCLType.Date:
                {
                    var inputFormats = schemaProperty.DateInputFormats
                                    ?? schema.DefaultDateInputFormats;

                    if (inputFormats is not null &&
                        DateTime.TryParseExact(
                            Value,
                            inputFormats.ToArray(),
                            null,
                            DateTimeStyles.None,
                            out var dt1
                        ))

                    {
                        return (
                            new Date(
                                dt1,
                                schemaProperty.DateOutputFormat ?? schema.DefaultDateOutputFormat
                            ), true);
                    }

                    if (DateTime.TryParse(Value, out var dt))
                        return (
                            new Date(
                                dt,
                                schemaProperty.DateOutputFormat ?? schema.DefaultDateOutputFormat
                            ), true);

                    break;
                }
                default: return CouldNotConvert(Value, schemaProperty, entity);
            }

            return CouldNotConvert(Value, schemaProperty, entity);
        }

        /// <inheritdoc />
        public override Result<Maybe<SchemaProperty>, IErrorBuilder> TryCreateSchemaProperty(
            string propertyName)
        {
            return Maybe<SchemaProperty>.From(
                    new SchemaProperty
                    {
                        Type = SCLType.String, Multiplicity = Multiplicity.ExactlyOne
                    }
                )
                ;
        }
    }

    /// <summary>
    /// An integer value
    /// </summary>
    public record Integer(int Value) : EntityValue(Value)
    {
        /// <inheritdoc />
        public override string GetFormattedString(
            char delimiter,
            string dateTimeFormat) => Value.ToString();

        /// <inheritdoc />
        public override string GetPrimitiveString()
        {
            return Value.ToString();
        }

        /// <inheritdoc />
        public override string Serialize() => Value.ToString();

        /// <inheritdoc />
        protected override Maybe<object> AsType(Type type)
        {
            if (type == typeof(int) || type == typeof(Double))
                return Value;

            return Maybe<object>.None;
        }

        /// <inheritdoc />
        protected override Result<(EntityValue value, bool changed), IErrorBuilder> TryConvertBase(
            Schema schema,
            string propertyName,
            SchemaProperty schemaProperty,
            Entity entity)
        {
            return schemaProperty.Type switch
            {
                SCLType.String  => (new String(Value.ToString()), true),
                SCLType.Integer => (this, false),
                SCLType.Double  => (new Double(Convert.ToDouble(Value)), true),
                _               => CouldNotConvert(Value, schemaProperty, entity)
            };
        }

        /// <inheritdoc />
        public override Result<Maybe<SchemaProperty>, IErrorBuilder> TryCreateSchemaProperty(
            string propertyName)
        {
            return Maybe<SchemaProperty>.From(
                new SchemaProperty
                {
                    Type = SCLType.Integer, Multiplicity = Multiplicity.ExactlyOne
                }
            );
        }
    }

    /// <summary>
    /// A double precision floating point value
    /// </summary>
    public record Double(double Value) : EntityValue(Value)
    {
        /// <inheritdoc />
        public override string GetFormattedString(
            char delimiter,
            string dateTimeFormat) => Value.ToString(Constants.DoubleFormat);

        /// <inheritdoc />
        public override string GetPrimitiveString() => Value.ToString("R");

        /// <inheritdoc />
        public override string Serialize() => Value.ToString(Constants.DoubleFormat);

        /// <inheritdoc />
        protected override Maybe<object> AsType(Type type)
        {
            if (type == typeof(double))
                return Value;

            return Maybe<object>.None;
        }

        /// <inheritdoc />
        protected override Result<(EntityValue value, bool changed), IErrorBuilder> TryConvertBase(
            Schema schema,
            string propertyName,
            SchemaProperty schemaProperty,
            Entity entity)
        {
            return schemaProperty.Type switch
            {
                SCLType.String => (
                    new String(Value.ToString(Constants.DoubleFormat)), false),
                SCLType.Double => (this, true),
                _              => CouldNotConvert(Value, schemaProperty, entity)
            };
        }

        /// <inheritdoc />
        public override Result<Maybe<SchemaProperty>, IErrorBuilder> TryCreateSchemaProperty(
            string propertyName)
        {
            return Maybe<SchemaProperty>.From(
                new SchemaProperty { Type = SCLType.Double, Multiplicity = Multiplicity.ExactlyOne }
            );
        }
    }

    /// <summary>
    /// A boolean value
    /// </summary>
    public record Boolean(bool Value) : EntityValue(Value)
    {
        /// <inheritdoc />
        public override string GetPrimitiveString() => Value.ToString();

        /// <inheritdoc />
        public override string Serialize() => Value.ToString();

        /// <inheritdoc />
        public override string GetFormattedString(
            char delimiter,
            string dateTimeFormat) => Value.ToString();

        /// <inheritdoc />
        protected override Result<(EntityValue value, bool changed), IErrorBuilder> TryConvertBase(
            Schema schema,
            string propertyName,
            SchemaProperty schemaProperty,
            Entity entity)
        {
            return schemaProperty.Type switch
            {
                SCLType.String => (new String(Value.ToString()), true),
                SCLType.Bool   => (this, false),
                _              => CouldNotConvert(Value, schemaProperty, entity)
            };
        }

        /// <inheritdoc />
        protected override Maybe<object> AsType(Type type)
        {
            if (type == typeof(bool))
                return Value;

            return Maybe<object>.None;
        }

        /// <inheritdoc />
        public override Result<Maybe<SchemaProperty>, IErrorBuilder> TryCreateSchemaProperty(
            string propertyName)
        {
            return Maybe<SchemaProperty>.From(
                new SchemaProperty { Type = SCLType.Bool, Multiplicity = Multiplicity.ExactlyOne }
            );
        }
    } //TODO constant values

    /// <summary>
    /// An enumeration value
    /// </summary>
    public record EnumerationValue(Enumeration Value) : EntityValue(Value)
    {
        /// <inheritdoc />
        public override string GetPrimitiveString() => Value.ToString();

        /// <inheritdoc />
        public override string Serialize() => Value.ToString();

        /// <inheritdoc />
        public override string GetFormattedString(
            char delimiter,
            string dateTimeFormat) => Value.ToString();

        /// <inheritdoc />
        protected override Maybe<object> AsType(Type type)
        {
            return Maybe<object>.None;
        }

        /// <inheritdoc />
        protected override Result<(EntityValue value, bool changed), IErrorBuilder> TryConvertBase(
            Schema schema,
            string propertyName,
            SchemaProperty schemaProperty,
            Entity entity)
        {
            switch (schemaProperty.Type)
            {
                case SCLType.String: return (this, false);
                case SCLType.Enum:
                {
                    if (schemaProperty.EnumType == null)
                        return new ErrorBuilder(ErrorCode.SchemaInvalidMissingEnum);

                    if (schemaProperty.Values == null || !schemaProperty.Values.Any())
                        return new ErrorBuilder(ErrorCode.SchemaInvalidNoEnumValues);

                    if (schemaProperty.Values.Contains(
                        Value.Value,
                        StringComparer.OrdinalIgnoreCase
                    ))
                    {
                        if (schemaProperty.EnumType == Value.Type)
                            return (this, false);

                        return (
                            new EnumerationValue(
                                new Enumeration(schemaProperty.EnumType, Value.Value)
                            ),
                            true);
                    }

                    return CouldNotConvert(Value, schemaProperty, entity);
                }
                default: return CouldNotConvert(Value, schemaProperty, entity);
            }
        }

        /// <inheritdoc />
        public override Result<Maybe<SchemaProperty>, IErrorBuilder> TryCreateSchemaProperty(
            string propertyName)
        {
            return Maybe<SchemaProperty>.From(
                new SchemaProperty
                {
                    Type         = SCLType.Enum,
                    Multiplicity = Multiplicity.ExactlyOne,
                    EnumType     = Value.Type
                }
            );
        }
    }

    /// <summary>
    /// A date time value
    /// </summary>
    public record Date(DateTime Value, string? DateOutputFormat) : EntityValue(Value)
    {
        /// <inheritdoc />
        public override string GetPrimitiveString() =>
            Value.ToString(DateOutputFormat ?? Constants.DateTimeFormat);

        /// <inheritdoc />
        public override string Serialize() =>
            Value.ToString(DateOutputFormat ?? Constants.DateTimeFormat);

        /// <inheritdoc />
        public override string GetFormattedString(
            char delimiter,
            string dateTimeFormat) => Value.ToString(dateTimeFormat);

        /// <inheritdoc />
        protected override Maybe<object> AsType(Type type)
        {
            if (type == typeof(DateTime))
                return Value;

            return Maybe<object>.None;
        }

        /// <inheritdoc />
        public override string ToString() =>
            Value.ToString(DateOutputFormat ?? Constants.DateTimeFormat);

        /// <inheritdoc />
        protected override Result<(EntityValue value, bool changed), IErrorBuilder> TryConvertBase(
            Schema schema,
            string propertyName,
            SchemaProperty schemaProperty,
            Entity entity)
        {
            return schemaProperty.Type switch
            {
                SCLType.String => (
                    new String(
                        Value.ToString(
                            DateOutputFormat
                         ?? Constants
                                .DateTimeFormat //Note: we don't use the schema date output format here
                        )
                    ), true),
                SCLType.Date => (this, false),
                _            => CouldNotConvert(Value, schemaProperty, entity)
            };
        }

        /// <inheritdoc />
        public override Result<Maybe<SchemaProperty>, IErrorBuilder> TryCreateSchemaProperty(
            string propertyName)
        {
            return Maybe<SchemaProperty>.From(
                new SchemaProperty
                {
                    Type             = SCLType.Date,
                    Multiplicity     = Multiplicity.ExactlyOne,
                    DateOutputFormat = DateOutputFormat
                }
            );
        }
    }

    /// <summary>
    /// A nested entity value
    /// </summary>
    public record NestedEntity(Entity Value) : EntityValue(Value)
    {
        /// <inheritdoc />
        public override string GetPrimitiveString() => Value.ToString();

        /// <inheritdoc />
        public override string Serialize() => Value.Serialize();

        /// <inheritdoc />
        public override string GetFormattedString(
            char delimiter,
            string dateTimeFormat) => Value.ToString();

        /// <inheritdoc />
        protected override Maybe<object> AsType(Type type)
        {
            if (type == typeof(Entity))
                return Value;

            return Maybe<object>.None;
        }

        /// <inheritdoc />
        public override string ToString() => Value.ToString();

        /// <inheritdoc />
        protected override Result<(EntityValue value, bool changed), IErrorBuilder> TryConvertBase(
            Schema schema,
            string propertyName,
            SchemaProperty schemaProperty,
            Entity entity)
        {
            return schemaProperty.Type switch
            {
                SCLType.String => (new String(Value.ToString()), true),
                SCLType.Enum   => (this, false),
                _              => CouldNotConvert(Value, schemaProperty, entity)
            };
        }

        /// <inheritdoc />
        public override Result<Maybe<SchemaProperty>, IErrorBuilder> TryCreateSchemaProperty(
            string propertyName)
        {
            return Maybe<SchemaProperty>.From(
                new SchemaProperty
                {
                    Type = SCLType.Entity, Multiplicity = Multiplicity.ExactlyOne,
                }
            );
        }
    }

    /// <summary>
    /// A list of values
    /// </summary>
    public record NestedList(ImmutableList<EntityValue> Value) : EntityValue(Value)
    {
        /// <inheritdoc />
        public override string GetPrimitiveString() =>
            SerializationMethods.SerializeList(Value.Select(y => y.Serialize()));

        /// <inheritdoc />
        public override string Serialize()
        {
            return SerializationMethods.SerializeList(Value.Select(y => y.Serialize()));
        }

        /// <inheritdoc />
        public override string GetFormattedString(
            char delimiter,
            string dateTimeFormat) => string.Join(
            delimiter,
            Value.Select(ev1 => ev1.GetFormattedString(delimiter, dateTimeFormat))
        );

        /// <inheritdoc />
        public virtual bool Equals(NestedList? other)
        {
            return other is not null && Value.SequenceEqual(other.Value);
        }

        /// <inheritdoc />
        public override int GetHashCode()
        {
            if (Value.IsEmpty)
                return 0;

            return HashCode.Combine(Value.First(), Value.Last(), Value.Count);
        }

        /// <inheritdoc />
        public override string ToString()
        {
            return Value.Count + " elements";
        }

        /// <inheritdoc />
        public override Result<Maybe<SchemaProperty>, IErrorBuilder> TryCreateSchemaProperty(
            string propertyName)
        {
            var members = Value.Select(x => x.TryCreateSchemaProperty(propertyName))
                .Combine(ErrorBuilderList.Combine);

            if (members.IsFailure)
                return members.ConvertFailure<Maybe<SchemaProperty>>();

            return SchemaProperty.Combine(propertyName, members.Value, Value.Count);
        }

        /// <inheritdoc />
        protected override Maybe<object> AsType(Type type)
        {
            if (type.IsGenericType && type.GetGenericTypeDefinition() == typeof(Array<>))
            {
                var genericType = type.GenericTypeArguments[0];

                var elements = Value.Select(x => x.TryGetValue(genericType))
                    .Combine(ErrorBuilderList.Combine);

                if (elements.IsFailure)
                    return elements.ConvertFailure<object>();

                var createArrayMethod =
                    typeof(ArrayHelper).GetMethod(nameof(ArrayHelper.CreateArray))!
                        .MakeGenericMethod(genericType);

                var arrayInstance = createArrayMethod.Invoke(
                    null,
                    new object?[] { elements.Value }
                );

                return arrayInstance!;
            }

            return Maybe<object>.None;
        }

        /// <inheritdoc />
        protected override Result<(EntityValue value, bool changed), IErrorBuilder> TryConvertBase(
            Schema schema,
            string propertyName,
            SchemaProperty schemaProperty,
            Entity entity)
        {
            if (schemaProperty.Multiplicity is Multiplicity.UpToOne or Multiplicity.ExactlyOne)
            {
                if (Value.Count == 1)
                    return Value.Single().TryConvert(schema, propertyName, schemaProperty, entity);

                if (Value.Count == 0 && schemaProperty.Multiplicity == Multiplicity.UpToOne)
                    return (Null.Instance, true);

                return ErrorCode.SchemaViolationUnexpectedList.ToErrorBuilder(propertyName, entity);
            }

            var sp = new SchemaProperty
            {
                EnumType         = schemaProperty.EnumType,
                Values           = schemaProperty.Values,
                ErrorBehavior    = schemaProperty.ErrorBehavior,
                DateInputFormats = schemaProperty.DateInputFormats,
                DateOutputFormat = schemaProperty.DateOutputFormat,
                Multiplicity     = Multiplicity.ExactlyOne,
                Regex            = schemaProperty.Regex,
                Type             = schemaProperty.Type
            };

            var newList = Value.Select(x => x.TryConvert(schema, propertyName, sp, entity))
                .Combine(ErrorBuilderList.Combine)
                .Map(
                    x => x.Aggregate(
                        (list: ImmutableList<EntityValue>.Empty, changed: false),
                        (a, b) => (a.list.Add(b.value), a.changed || b.changed)
                    )
                )
                .Map(
                    x =>
                        x.changed
                            ? (new NestedList(x.list) as EntityValue, true)
                            : (this, false)
                );

            return newList;
        }
    }

    /// <summary>
    /// Create an entity from structured entity properties
    /// </summary>
    public static EntityValue CreateFromProperties(
        IReadOnlyList<(Maybe<EntityPropertyKey> key, object? argValue)> properties,
        char? multiValueDelimiter)
    {
        if (properties.Count == 0)
            return Null.Instance;

        if (properties.Count == 1 && properties.Single().key.HasNoValue)
            return CreateFromObject(properties.Single().argValue, multiValueDelimiter);

        var entityProperties =
            new Dictionary<string, EntityProperty>(StringComparer.OrdinalIgnoreCase);

        void SetEntityProperty(string key, EntityValue ev)
        {
            EntityProperty newProperty;

            if (entityProperties.TryGetValue(key, out var existingValue))
            {
                if (ev is NestedEntity nestedEntity)
                {
                    if (existingValue.BestValue is NestedEntity existingNestedEntity)
                    {
                        var nEntity = existingNestedEntity.Value.Combine(nestedEntity.Value);

                        newProperty = new EntityProperty(
                            key,
                            new NestedEntity(nEntity),
                            null,
                            existingValue.Order
                        );
                    }
                    else
                    {
                        //Ignore the old property
                        newProperty = new EntityProperty(key, ev, null, existingValue.Order);
                    }
                }
                else if (existingValue.BestValue is NestedEntity existingNestedEntity)
                {
                    var nEntity =
                        existingNestedEntity.Value.WithProperty(Entity.PrimitiveKey, ev);

                    newProperty = new EntityProperty(
                        key,
                        new NestedEntity(nEntity),
                        null,
                        existingValue.Order
                    );
                }
                else //overwrite the existing property
                    newProperty = new EntityProperty(key, ev, null, existingValue.Order);
            }
            else //New property
                newProperty = new EntityProperty(key, ev, null, entityProperties.Count);

            entityProperties[key] = newProperty;
        }

        foreach (var (key, argValue) in properties)
        {
            if (key.HasNoValue)
            {
                var ev = CreateFromObject(argValue, multiValueDelimiter);

                if (ev is NestedEntity ne)
                    foreach (var (nestedKey, value) in ne.Value.Dictionary)
                        SetEntityProperty(nestedKey, value.BestValue);
                else
                    SetEntityProperty(Entity.PrimitiveKey, ev);
            }
            else
            {
                var (firstKey, remainder) = key.Value.Split();

                var ev = CreateFromProperties(
                    new[] { (remainder, argValue) },
                    multiValueDelimiter
                );

                SetEntityProperty(firstKey, ev);
            }
        }

        var newEntity = new Entity(entityProperties.ToImmutableDictionary());

        return new NestedEntity(newEntity);
    }

    /// <summary>
    /// Create an entity from an object
    /// </summary>
    public static EntityValue CreateFromObject(object? argValue, char? multiValueDelimiter = null)
    {
        switch (argValue)
        {
            case null:             return Null.Instance;
            case EntityValue ev:   return ev;
            case StringStream ss1: return Create(ss1.GetString(), multiValueDelimiter);
            case string s:         return Create(s,               multiValueDelimiter);
            case int i:            return new Integer(i);
            case byte @byte:       return new Integer(@byte);
            case short @short:     return new Integer(@short);
            case bool b:           return new Boolean(b);
            case double d:         return new Double(d);
            case long ln and < int.MaxValue and > int.MinValue:
                return new Integer(Convert.ToInt32(ln));
            case Enumeration e1: return new EnumerationValue(e1);
            case DateTime dt:    return new Date(dt, null);
            case Enum e:
                return new EnumerationValue(new Enumeration(e.GetType().Name, e.ToString()));
            case JValue jv:             return CreateFromObject(jv.Value, multiValueDelimiter);
            case JObject jo:            return new NestedEntity(Entity.Create(jo));
            case Entity entity:         return new NestedEntity(entity);
            case IEntityConvertible ec: return new NestedEntity(ec.ConvertToEntity());
            case Version version:       return new String(version.ToString());

            case IDictionary dict:
            {
                var builder = ImmutableDictionary<string, EntityProperty>.Empty.ToBuilder();
                var i       = 0;

                foreach (DictionaryEntry dictionaryEntry in dict)
                {
                    var val = dictionaryEntry.Value;
                    var ev  = CreateFromObject(val);
                    var ep  = new EntityProperty(dictionaryEntry.Key.ToString()!, ev, null, i);
                    builder.Add(dictionaryEntry.Key.ToString()!, ep);
                    i++;
                }

                var entity = new Entity(builder.ToImmutable());
                return new NestedEntity(entity);
            }
            case IEnumerable e2:
            {
                var newEnumerable = e2.Cast<object>()
                    .Select(v => CreateFromObject(v, multiValueDelimiter))
                    .ToImmutableList();

                if (!newEnumerable.Any())
                    return Null.Instance;

                return new NestedList(newEnumerable);
            }
            case IResult:
                throw new ArgumentException(
                    "Attempt to set EntityValue to a Result - you should check the result for failure and then set it to the value of the result",
                    nameof(argValue)
                );

            default:

            {
                if (argValue.GetType()
                        .GetCustomAttributes(true)
                        .OfType<SerializableAttribute>()
                        .Any() ||
                    argValue.GetType()
                        .GetCustomAttributes(true)
                        .OfType<DataContractAttribute>()
                        .Any()
                )
                {
                    var entity = EntityConversionHelpers.ConvertToEntity(argValue);
                    return new NestedEntity(entity);
                }

                throw new ArgumentException(
                    $"Attempt to set EntityValue to {argValue.GetType().Name}"
                );
            }
        }

        static EntityValue Create(string s, char? multiValueDelimiter)
        {
            if (string.IsNullOrWhiteSpace(s))
                return Null.Instance;

            if (multiValueDelimiter == null)
                return new String(s);

            var stringArray = s.Split(multiValueDelimiter.Value);

            if (stringArray.Length > 1)
                return new NestedList(
                    stringArray.Select(s => new String(s)).ToImmutableList<EntityValue>()
                );

            return new String(s);
        }
    }

    /// <summary>
    /// Tries to convert a value to match the schema
    /// </summary>
    protected abstract Result<(EntityValue value, bool changed), IErrorBuilder> TryConvertBase(
        Schema schema,
        string propertyName,
        SchemaProperty schemaProperty,
        Entity entity);

    /// <summary>
    /// Error returned when a value cannot be converted
    /// </summary>
    protected static ErrorBuilder CouldNotConvert(
        object o,
        SchemaProperty schemaProperty,
        Entity entity) => ErrorCode.SchemaViolationWrongType.ToErrorBuilder(
        o,
        schemaProperty.Type,
        entity
    );

    /// <summary>
    /// Tries to convert the value so it matches the schema.
    /// </summary>
    public Result<(EntityValue value, bool changed), IErrorBuilder> TryConvert(
        Schema schema,
        string propertyName,
        SchemaProperty schemaProperty,
        Entity entity)
    {
        if (schemaProperty.Regex != null)
        {
            var primitiveString = GetPrimitiveString();

            if (!Regex.IsMatch(primitiveString, schemaProperty.Regex))
                return new ErrorBuilder(
                    ErrorCode.SchemaViolationUnmatchedRegex,
                    primitiveString,
                    schemaProperty.Regex,
                    entity
                );
        }

        return TryConvertBase(schema, propertyName, schemaProperty, entity);
    }

    /// <summary>
    /// Create a schema property that could contain this entity value
    /// </summary>
    /// <returns></returns>
    public abstract Result<Maybe<SchemaProperty>, IErrorBuilder> TryCreateSchemaProperty(
        string propertyName);

    /// <summary>
    /// If this is a primitive, get a string representation
    /// </summary>
    public abstract string GetPrimitiveString();

    /// <summary>
    /// Serialize this value as it will appear in SCL
    /// </summary>
    public abstract string Serialize();

    /// <summary>
    /// Gets a string with the given format
    /// </summary>
    public abstract string GetFormattedString(
        char delimiter,
        string dateTimeFormat);

    /// <summary>
    /// Gets the default entity value for a particular type
    /// </summary>
    public static T GetDefaultValue<T>()
    {
        if (Entity.Empty is T tEntity)
            return tEntity;

        if (StringStream.Empty is T tStringStream)
            return tStringStream;

        if ("" is T tString)
            return tString;

        if (typeof(T).IsAssignableTo(typeof(IArray)) && typeof(T).IsGenericType)
        {
            var param = typeof(T).GenericTypeArguments[0];
            var array = typeof(EagerArray<>).MakeGenericType(param);

            var arrayInstance = Activator.CreateInstance(array);

            return (T)arrayInstance!;
        }

        return default!;
    }

    /// <summary>
    /// Returns the value, if it is of a particular type
    /// </summary>
    protected abstract Maybe<object> AsType(Type type);

    private Result<object, IErrorBuilder> TryGetValue(Type type)
    {
        var maybeObject = AsType(type);

        if (maybeObject.HasValue)
            return maybeObject.Value;

        var primitive = GetPrimitiveString();

        if (type == typeof(int))
        {
            if (int.TryParse(primitive, out var i))
                return i;
        }

        else if (type == typeof(double))
        {
            if (double.TryParse(primitive, out var d))
                return d;
        }
        else if (type == typeof(bool))
        {
            if (bool.TryParse(primitive, out var b))
                return b;
        }
        else if (type.IsEnum)
        {
            if (this is EnumerationValue enumeration)
            {
                if (Enum.TryParse(type, enumeration.Value.Value, true, out var ro))
                    return ro!;
            }
            else if (!int.TryParse(primitive, out _) && //prevent int conversion
                     Enum.TryParse(type, primitive, true, out var r)
            )
                return r!;
        }
        else if (type == typeof(DateTime))
        {
            if (!double.TryParse(primitive, out _) && //prevent double conversion
                DateTime.TryParse(primitive, out var d))
                return d;
        }
        else if (type == typeof(string))
        {
            var ser = Serialize();

            return ser;
        }
        else if (type == typeof(StringStream) || type == typeof(object))
        {
            var ser = new StringStream(GetPrimitiveString());

            return ser;
        }
        else if (type.GetInterfaces().Contains(typeof(IOneOf)))
        {
            var i = 0;

            foreach (var typeGenericTypeArgument in type.GenericTypeArguments)
            {
                var value = TryGetValue(typeGenericTypeArgument);

                if (value.IsSuccess)
                {
                    var methodName = $"FromT{i}";

                    var method = type.GetMethod(
                        methodName,
                        BindingFlags.Static | BindingFlags.Public
                    )!;

                    var oneOfThing = method.Invoke(null, new[] { value.Value })!;

                    return Result.Success<object, IErrorBuilder>(oneOfThing);
                }

                i++;
            }
        }

        return ErrorCode.CouldNotConvertEntityValue.ToErrorBuilder(type.Name);
    }

    /// <summary>
    /// Tries to get the value if it is a particular type
    /// </summary>
    public Result<T, IErrorBuilder> TryGetValue<T>()
    {
        var r = TryGetValue(typeof(T));

        if (r.IsFailure)
            return r.ConvertFailure<T>();

        return (T)r.Value!;
    }
}

}
