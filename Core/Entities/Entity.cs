﻿using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading;
using System.Threading.Tasks;
using CSharpFunctionalExtensions;
using Reductech.EDR.Core.Entities;
using Reductech.EDR.Core.Internal;
using Reductech.EDR.Core.Internal.Errors;
using Reductech.EDR.Core.Internal.Serialization;

// ReSharper disable once CheckNamespace - we want this namespace to prevent clash with FunctionalExtensions
namespace Reductech.EDR.Core
{

/// <summary>
/// A piece of data.
/// </summary>
[JsonConverter(typeof(EntityJsonConverter))]
public sealed class Entity : IEnumerable<EntityProperty>, IEquatable<Entity>
{
    /// <summary>
    /// Constructor
    /// </summary>
    public Entity(ImmutableDictionary<string, EntityProperty> dictionary) =>
        Dictionary = dictionary.WithComparers(StringComparer.OrdinalIgnoreCase);

    /// <summary>
    /// Constructor
    /// </summary>
    public Entity(IEnumerable<EntityProperty> entityProperties)
    {
        Dictionary = entityProperties.ToImmutableDictionary(
            x => x.Name,
            StringComparer.OrdinalIgnoreCase
        );
    }

    /// <summary>
    /// Empty entity
    /// </summary>
    public static Entity Empty { get; } = new(ImmutableDictionary<string, EntityProperty>.Empty);

    /// <summary>
    /// The dictionary of property values.
    /// </summary>
    public ImmutableDictionary<string, EntityProperty> Dictionary { get; }

    /// <summary>
    /// The default property name if the Entity represents a single primitive.
    /// </summary>
    public const string PrimitiveKey = "value";

    /// <summary>
    /// Creates a new Entity
    /// </summary>
    public static Entity Create(params (string key, object? value)[] properties) => Create(
        properties.Select(x => (new EntityPropertyKey(x.key), x.value))
    );

    /// <summary>
    /// Create an entity from a JsonElement
    /// </summary>
    public static Entity Create(JsonElement element)
    {
        var ev = EntityValue.Create(element);

        if (ev is EntityValue.NestedEntity nestedEntity)
            return nestedEntity.Value;

        return new Entity(
            ImmutableDictionary<string, EntityProperty>.Empty
                .Add(PrimitiveKey, new EntityProperty(PrimitiveKey, ev, 0))
        );
    }

    /// <summary>
    /// Convert this Entity to a Json Element
    /// </summary>
    public JsonElement ToJsonElement()
    {
        var options = new JsonSerializerOptions()
        {
            Converters = { new JsonStringEnumConverter() }
        };

        var stream = new MemoryStream();
        var writer = new Utf8JsonWriter(stream);

        EntityJsonConverter.Instance.Write(writer, this, options);

        var reader = new Utf8JsonReader(stream.ToArray());

        using JsonDocument document = JsonDocument.ParseValue(ref reader);
        return document.RootElement.Clone();
    }

    /// <summary>
    /// Creates a new Entity
    /// </summary>
    public static Entity Create(
        IEnumerable<(EntityPropertyKey key, object? value)> properties,
        char? multiValueDelimiter = null)
    {
        var dict =
            properties.Select(
                    p =>
                    {
                        (string firstKey, var remainder) = p.key.Split();
                        return (firstKey, remainder, p.value);
                    }
                )
                .GroupBy(x => x.firstKey, x => (x.remainder, x.value))
                .Select(
                    (group, i) =>
                    {
                        var ev = EntityValue.CreateFromProperties(
                            group.ToList(),
                            multiValueDelimiter
                        );

                        return new EntityProperty(group.Key, ev, i);
                    }
                )
                .ToImmutableDictionary(x => x.Name);

        return new Entity(dict);
    }

    /// <summary>
    /// Creates a copy of this with the property added or updated
    /// </summary>
    public Entity WithProperty(string key, EntityValue newValue)
    {
        EntityProperty newProperty;

        if (Dictionary.TryGetValue(key, out var ep))
        {
            EntityValue newValue2;

            if (ep.Value is EntityValue.NestedEntity existingEntity)
            {
                Entity combinedEntity;

                if (newValue is EntityValue.NestedEntity newEntity)
                    combinedEntity = existingEntity.Value.Combine(newEntity.Value);
                else
                    combinedEntity = existingEntity.Value.WithProperty(PrimitiveKey, newValue);

                newValue2 = new EntityValue.NestedEntity(combinedEntity);
            }
            else
            {
                //overwrite the property
                newValue2 = newValue;
            }

            newProperty = new EntityProperty(ep.Name, newValue2, ep.Order);
        }
        else
        {
            newProperty = new EntityProperty(key, newValue, Dictionary.Count);
        }

        ImmutableDictionary<string, EntityProperty> newDict = Dictionary.SetItem(key, newProperty);

        return new Entity(newDict);
    }

    /// <summary>
    /// Returns a copy of this entity with the specified property removed
    /// </summary>
    public Entity RemoveProperty(string propertyName)
    {
        if (!Dictionary.ContainsKey(propertyName))
            return this; //No property to remove

        var newDict = Dictionary.Remove(propertyName);

        return new Entity(newDict);
    }

    /// <summary>
    /// Combine two entities.
    /// Properties on the other entity take precedence.
    /// </summary>
    public Entity Combine(Entity other)
    {
        var current = this;

        foreach (var ep in other)
            current = current.WithProperty(ep.Name, ep.Value);

        return current;
    }

    /// <summary>
    /// Try to get the value of a particular property
    /// </summary>
    public Maybe<EntityValue> TryGetValue(string key) => TryGetValue(new EntityPropertyKey(key));

    /// <summary>
    /// Try to get the value of a particular property
    /// </summary>
    public Maybe<EntityValue> TryGetValue(EntityPropertyKey key)
    {
        var (firstKey, remainder) = key.Split();

        if (!Dictionary.TryGetValue(firstKey, out var ep))
            return Maybe<EntityValue>.None;

        if (remainder.HasNoValue)
            return ep.Value;

        if (ep.Value is EntityValue.NestedEntity nestedEntity)
            return nestedEntity.Value.TryGetValue(remainder.GetValueOrThrow());
        //We can't get the nested property as this is not an entity

        return
            Maybe<EntityValue>.None;
    }

    /// <inheritdoc />
    public IEnumerator<EntityProperty> GetEnumerator() =>
        Dictionary.Values.OrderBy(x => x.Order).GetEnumerator();

    /// <inheritdoc />
    public bool Equals(Entity? other)
    {
        if (other is null)
            return false;

        if (!Dictionary.Keys.SequenceEqual(other.Dictionary.Keys))
            return false;

        var r = Dictionary.Values.SequenceEqual(other.Dictionary.Values);

        return r;
    }

    /// <inheritdoc />
    public override bool Equals(object? obj)
    {
        if (obj is null)
            return false;

        if (ReferenceEquals(this, obj))
            return true;

        return obj is Entity e && Equals(e);
    }

    /// <inheritdoc />
    public override int GetHashCode()
    {
        return Dictionary.Count switch
        {
            0 => 0,
            1 => Dictionary.Single().Value.GetHashCode(),
            _ => HashCode.Combine(Dictionary.Count, Dictionary.First().Value.GetHashCode())
        };
    }

    /// <inheritdoc />
    public override string ToString() => this.Serialize();

    /// <inheritdoc />
    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
}

/// <summary>
/// Contains methods for helping with entities.
/// </summary>
public static class EntityHelper
{
    /// <summary>
    /// Tries to convert an object into one suitable as an entity property.
    /// </summary>
    public static async Task<Result<object?, IError>> TryUnpackObjectAsync(
        object? o,
        CancellationToken cancellation)
    {
        if (o is IArray list)
        {
            var r = await list.GetObjectsAsync(cancellation);

            if (r.IsFailure)
                return r.ConvertFailure<object?>();

            var q = await r.Value.Select(x => TryUnpackObjectAsync(x, cancellation))
                .Combine(ErrorList.Combine)
                .Map(x => x.ToList());

            return q;
        }

        return Result.Success<object?, IError>(o);
    }
}

}
