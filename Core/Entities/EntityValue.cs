﻿using System;
using System.Collections;
using System.Collections.Immutable;
using System.Globalization;
using System.Linq;
using System.Text.RegularExpressions;
using CSharpFunctionalExtensions;
using Newtonsoft.Json.Linq;
using OneOf;
using Reductech.EDR.Core.Internal;
using Reductech.EDR.Core.Internal.Errors;
using Reductech.EDR.Core.Util;

namespace Reductech.EDR.Core.Entities
{

/// <summary>
/// The value of an entity property.
/// </summary>
public sealed class EntityValue : OneOfBase<DBNull, string, int, double, bool, Enumeration, DateTime
                                    , Entity, ImmutableList<EntityValue>
                                  >, IEquatable<EntityValue>
{
    /// <summary>
    /// Create a new entityValue
    /// </summary>
    public EntityValue(
        OneOf<DBNull, string, int, double, bool, Enumeration, DateTime, Entity,
            ImmutableList<EntityValue>> value,
        string? outputFormat = null) : base(value) => OutputFormat = outputFormat;

    /// <summary>
    /// The output format if this is a DateTime and this has been set by a schema
    /// </summary>
    public readonly string? OutputFormat;

    /// <summary>
    /// The DateOutputFormat to use
    /// </summary>
    public string DateOutputFormat => string.IsNullOrWhiteSpace(OutputFormat)
        ? Constants.DateTimeFormat
        : OutputFormat;

    /// <summary>
    /// Create an entity from an object
    /// </summary>
    public static EntityValue CreateFromObject(object? argValue, char? multiValueDelimiter = null)
    {
        switch (argValue)
        {
            case null:             return new EntityValue(DBNull.Value);
            case StringStream ss1: return Create(ss1.GetString(), multiValueDelimiter);
            case string s:         return Create(s,               multiValueDelimiter);
            case int i:            return new EntityValue(i);
            case bool b:           return new EntityValue(b);
            case double d:         return new EntityValue(d);
            case long ln when ln < int.MaxValue && ln > int.MinValue:
                return new EntityValue(Convert.ToInt32(ln));
            case Enumeration e1: return new EntityValue(e1);
            case DateTime dt: return new EntityValue(dt);
            case Enum e: return new EntityValue(new Enumeration(e.GetType().Name, e.ToString()));
            case JValue jv: return CreateFromObject(jv.Value, multiValueDelimiter);
            case JObject jo: return new EntityValue(Entity.Create(jo));
            case Entity entity: return new EntityValue(entity);
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
                return new EntityValue(entity);
            }
            case IEnumerable e2:
            {
                var newEnumerable = e2.Cast<object>()
                    .Select(v => CreateFromObject(v, multiValueDelimiter))
                    .ToImmutableList();

                if (!newEnumerable.Any())
                    return new EntityValue(DBNull.Value);

                return new EntityValue(newEnumerable);
            }
            case IResult:
                throw new ArgumentException(
                    "Attempt to set EntityValue to a Result - you should check the result for failure and then set it to the value of the result",
                    nameof(argValue)
                );
            default:
                throw new ArgumentException(
                    $"Attempt to set EntityValue to {argValue.GetType().Name}"
                );
        }

        static EntityValue Create(string s, char? multiValueDelimiter)
        {
            if (multiValueDelimiter == null)
                return new EntityValue(s);

            var stringArray = s.Split(multiValueDelimiter.Value);

            if (stringArray.Length > 1)
                return new EntityValue(
                    stringArray.Select(s => new EntityValue(s)).ToImmutableList()
                );

            return new EntityValue(s);
        }
    }

    /// <summary>
    /// Combine this with another EntityValue
    /// </summary>
    public EntityValue Combine(EntityValue other)
    {
        ImmutableList<EntityValue> newList;

        if (TryPickT8(out var thisList, out _))
        {
            if (other.TryPickT8(out var otherList, out _))
                newList = thisList.AddRange(otherList);
            else
                newList = thisList.Add(other);
        }
        else
        {
            if (other.TryPickT8(out var otherList, out _))
                newList = otherList.Insert(0, this);
            else
                newList = ImmutableList.Create(this, other);
        }

        return new EntityValue(newList);
    }

    /// <summary>
    /// Tries to convert the value so it matches the schema.
    /// </summary>
    public Result<(EntityValue value, bool changed), IErrorBuilder> TryConvert(
        SchemaProperty schemaProperty)
    {
        if (schemaProperty.Regex != null)
        {
            var s = this.GetString();

            if (!Regex.IsMatch(s, schemaProperty.Regex))
                return new ErrorBuilder(
                    ErrorCode.SchemaViolationUnmatchedRegex,
                    s,
                    schemaProperty.Regex
                );
        }

        ErrorBuilder CouldNotConvert(object o) => new(
            ErrorCode.SchemaViolationWrongType,
            o,
            schemaProperty.Type
        );

        var r = Match(
            MatchDbNull,
            MatchString,
            MatchInt,
            MatchDouble,
            MatchBool,
            MatchEnum,
            MatchDateTime,
            MatchEntity,
            MatchList
        );

        return r;

        Result<(EntityValue value, bool changed), IErrorBuilder> MatchDbNull(DBNull dbNull)
        {
            if (schemaProperty.Multiplicity == Multiplicity.Any ||
                schemaProperty.Multiplicity == Multiplicity.UpToOne)
                return (this, false);

            return new ErrorBuilder(ErrorCode.SchemaViolationUnexpectedNull);
        }

        Result<(EntityValue value, bool changed), IErrorBuilder> MatchString(string s)
        {
            switch (schemaProperty.Type)
            {
                case SchemaPropertyType.String: return (this, false);
                case SchemaPropertyType.Integer:
                {
                    if (int.TryParse(s, out var i))
                        return (new EntityValue(i), true);

                    break;
                }
                case SchemaPropertyType.Double:
                {
                    if (double.TryParse(s, out var d))
                        return (new EntityValue(d), true);

                    break;
                }
                case SchemaPropertyType.Enum:
                {
                    if (string.IsNullOrWhiteSpace(schemaProperty.EnumType))
                        return new ErrorBuilder(ErrorCode.SchemaInvalidMissingEnum);

                    if (schemaProperty.Values == null || !schemaProperty.Values.Any())
                        return new ErrorBuilder(ErrorCode.SchemaInvalidNoEnumValues);

                    if (schemaProperty.Values.Contains(s, StringComparer.OrdinalIgnoreCase))
                        return (new EntityValue(new Enumeration(schemaProperty.EnumType, s)), true);

                    break;
                }
                case SchemaPropertyType.Bool:
                {
                    if (bool.TryParse(s, out var b))
                        return (new EntityValue(b), true);

                    break;
                }
                case SchemaPropertyType.Date:
                {
                    if (schemaProperty.DateInputFormats != null &&
                        DateTime.TryParseExact(
                            s,
                            schemaProperty.DateInputFormats.ToArray(),
                            null,
                            DateTimeStyles.None,
                            out var dt1
                        ))

                    {
                        return (new EntityValue(dt1, schemaProperty.DateOutputFormat), true);
                    }

                    if (DateTime.TryParse(s, out var dt))
                        return (new EntityValue(dt, schemaProperty.DateOutputFormat), true);

                    break;
                }
                default: return CouldNotConvert(s);
            }

            return CouldNotConvert(s);
        }

        Result<(EntityValue value, bool changed), IErrorBuilder> MatchInt(int i)
        {
            return schemaProperty.Type switch
            {
                SchemaPropertyType.String  => (new EntityValue(i.ToString()), true),
                SchemaPropertyType.Integer => (this, false),
                SchemaPropertyType.Double  => (new EntityValue(Convert.ToDouble(i)), true),
                _                          => CouldNotConvert(i)
            };
        }

        Result<(EntityValue value, bool changed), IErrorBuilder> MatchDouble(double d)
        {
            return schemaProperty.Type switch
            {
                SchemaPropertyType.String => (
                    new EntityValue(d.ToString(Constants.DoubleFormat)), false),
                SchemaPropertyType.Double => (this, true),
                _                         => CouldNotConvert(d)
            };
        }

        Result<(EntityValue value, bool changed), IErrorBuilder> MatchBool(bool b)
        {
            return schemaProperty.Type switch
            {
                SchemaPropertyType.String => (new EntityValue(b.ToString()), true),
                SchemaPropertyType.Bool   => (this, false),
                _                         => CouldNotConvert(b)
            };
        }

        Result<(EntityValue value, bool changed), IErrorBuilder> MatchEnum(Enumeration e)
        {
            switch (schemaProperty.Type)
            {
                case SchemaPropertyType.String: return (this, false);
                case SchemaPropertyType.Enum:
                {
                    if (schemaProperty.EnumType == null)
                        return new ErrorBuilder(ErrorCode.SchemaInvalidMissingEnum);

                    if (schemaProperty.Values == null || !schemaProperty.Values.Any())
                        return new ErrorBuilder(ErrorCode.SchemaInvalidNoEnumValues);

                    if (schemaProperty.Values.Contains(e.Value, StringComparer.OrdinalIgnoreCase))
                    {
                        if (schemaProperty.EnumType == e.Type)
                            return (this, false);

                        return (new EntityValue(new Enumeration(schemaProperty.EnumType, e.Value)),
                                true);
                    }

                    return CouldNotConvert(e);
                }
                default: return CouldNotConvert(e);
            }
        }

        Result<(EntityValue value, bool changed), IErrorBuilder> MatchDateTime(DateTime dt)
        {
            return schemaProperty.Type switch
            {
                SchemaPropertyType.String => (
                    new EntityValue(dt.ToString(DateOutputFormat)), true),
                SchemaPropertyType.Date => (this, false),
                _                       => CouldNotConvert(dt)
            };
        }

        Result<(EntityValue value, bool changed), IErrorBuilder> MatchEntity(Entity e)
        {
            return schemaProperty.Type switch
            {
                SchemaPropertyType.String => (new EntityValue(e.ToString()), true),
                SchemaPropertyType.Enum   => (this, false),
                _                         => CouldNotConvert(e)
            };
        }

        Result<(EntityValue value, bool changed), IErrorBuilder> MatchList(
            ImmutableList<EntityValue> list)
        {
            if (schemaProperty.Multiplicity == Multiplicity.UpToOne ||
                schemaProperty.Multiplicity == Multiplicity.ExactlyOne)
            {
                if (list.Count == 1)
                    return list.Single().TryConvert(schemaProperty);

                if (list.Count == 0 && schemaProperty.Multiplicity == Multiplicity.UpToOne)
                    return (new EntityValue(DBNull.Value), true);

                return new ErrorBuilder(ErrorCode.SchemaViolationUnexpectedList);
            }

            var sp = new SchemaProperty
            {
                EnumType         = schemaProperty.EnumType,
                Values           = schemaProperty.Values,
                ErrorBehaviour   = schemaProperty.ErrorBehaviour,
                DateInputFormats = schemaProperty.DateInputFormats,
                DateOutputFormat = schemaProperty.DateOutputFormat,
                Multiplicity     = Multiplicity.ExactlyOne,
                Regex            = schemaProperty.Regex,
                Type             = schemaProperty.Type
            };

            var newList = list.Select(x => x.TryConvert(sp))
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
                            ? (new EntityValue(x.list, schemaProperty.DateOutputFormat), true)
                            : (this, false)
                );

            return newList;
        }
    }

    /// <inheritdoc />
    public bool Equals(EntityValue? other)
    {
        if (other is null)
            return false;

        if (Index != other.Index)
            return false;

        if (OutputFormat != other.OutputFormat)
            return false;

        var r = Match(
            _ => other.IsT0,
            a => a.Equals(other.AsT1),
            a => a.Equals(other.AsT2),
            a => a.Equals(other.AsT3),
            a => a.Equals(other.AsT4),
            a => a.Equals(other.AsT5),
            a => a.Equals(other.AsT6),
            a => a.Equals(other.AsT7),
            a => a.SequenceEqual(other.AsT8)
        );

        return r;
    }

    /// <inheritdoc />
    public override bool Equals(object? obj)
    {
        if (obj is null)
            return false;

        if (ReferenceEquals(this, obj))
            return true;

        return obj is EntityValue ev && Equals(ev);
    }

    /// <inheritdoc />
    public override int GetHashCode() =>
        Value.GetHashCode(); //TODO get hash code from immutable list

    /// <inheritdoc />
    public override string ToString()
    {
        return Match(
            _ => "Empty",
            x => x,
            x => x.ToString(),
            x => x.ToString(Constants.DoubleFormat),
            x => x.ToString(),
            x => x.ToString(),
            x => x.ToString(DateOutputFormat),
            x => x.ToString(),
            x => x.Count + " elements"
        );
    }
}

}
