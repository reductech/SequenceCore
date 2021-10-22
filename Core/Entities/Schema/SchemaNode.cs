﻿using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics.Contracts;
using System.Linq;
using System.Text.Json;
using CSharpFunctionalExtensions;
using Json.Schema;
using Reductech.EDR.Core.Internal.Errors;

namespace Reductech.EDR.Core.Entities.Schema
{

/// <summary>
/// A schema node with one additional item of data
/// </summary>
public abstract record SchemaNode<TData1, TData2>(
    EnumeratedValuesNodeData EnumeratedValuesNodeData,
    TData1 Data1,
    TData2 Data2) : SchemaNode(EnumeratedValuesNodeData)
    where TData1 : NodeData<TData1>
    where TData2 : NodeData<TData2>
{
    /// <inheritdoc />
    public override IEnumerable<INodeData> NodeData
    {
        get
        {
            yield return EnumeratedValuesNodeData;
            yield return Data1;
            yield return Data2;
        }
    }

    /// <inheritdoc />
    public override SchemaNode Combine(SchemaNode other)
    {
        if (other.IsMorePermissive(this))
            return other;

        if (IsMorePermissive(other))
            return this;

        if (other is SchemaNode<TData1, TData2> otherNode)
        {
            if (this.CanCombineWith(otherNode))
            {
                var evndCombined =
                    EnumeratedValuesNodeData.Combine(otherNode.EnumeratedValuesNodeData);

                var data1Combined = Data1.Combine(otherNode.Data1);
                var data2Combined = Data2.Combine(otherNode.Data2);

                return this with
                {
                    EnumeratedValuesNodeData = evndCombined,
                    Data1 = data1Combined,
                    Data2 = data2Combined
                };
            }
            else if (otherNode.CanCombineWith(this))
            {
                var evndCombined =
                    otherNode.EnumeratedValuesNodeData.Combine(EnumeratedValuesNodeData);

                var data1Combined = otherNode.Data1.Combine(Data1);
                var data2Combined = otherNode.Data2.Combine(Data2);

                return otherNode with
                {
                    EnumeratedValuesNodeData = evndCombined,
                    Data1 = data1Combined,
                    Data2 = data2Combined
                };
            }
        }

        return TrueNode.Instance;
    }
}

/// <summary>
/// A schema node with one additional item of data
/// </summary>
public abstract record SchemaNode<TData1>(
    EnumeratedValuesNodeData EnumeratedValuesNodeData,
    TData1 Data1) : SchemaNode(EnumeratedValuesNodeData) where TData1 : NodeData<TData1>
{
    /// <inheritdoc />
    public override IEnumerable<INodeData> NodeData
    {
        get
        {
            yield return EnumeratedValuesNodeData;
            yield return Data1;
        }
    }

    /// <inheritdoc />
    public override SchemaNode Combine(SchemaNode other)
    {
        if (other.IsMorePermissive(this))
            return other;

        if (IsMorePermissive(other))
            return this;

        if (other is SchemaNode<TData1> otherNode)
        {
            if (this.CanCombineWith(otherNode))
            {
                var evndCombined =
                    EnumeratedValuesNodeData.Combine(otherNode.EnumeratedValuesNodeData);

                var dataCombined = Data1.Combine(otherNode.Data1);

                return this with { EnumeratedValuesNodeData = evndCombined, Data1 = dataCombined };
            }
            else if (otherNode.CanCombineWith(this))
            {
                var evndCombined =
                    otherNode.EnumeratedValuesNodeData.Combine(EnumeratedValuesNodeData);

                var dataCombined = otherNode.Data1.Combine(Data1);

                return otherNode with
                {
                    EnumeratedValuesNodeData = evndCombined, Data1 = dataCombined
                };
            }
        }

        return TrueNode.Instance;
    }
}

/// <summary>
/// A Node in a schema
/// </summary>
public abstract record SchemaNode(EnumeratedValuesNodeData EnumeratedValuesNodeData)
{
    /// <summary>
    /// Create a schema node from a Json Schema
    /// </summary>
    [Pure]
    public static SchemaNode Create(JsonSchema schema)
    {
        if (schema.Keywords is null)
            return TrueNode.Instance;

        var type = schema.Keywords.OfType<TypeKeyword>().FirstOrDefault()
                ?? new TypeKeyword(SchemaValueType.Object);

        EnumeratedValuesNodeData enumeratedValuesNodeData;

        var constantValue = schema.Keywords.OfType<ConstKeyword>()
            .Select(x => x.Value.Clone() as JsonElement?)
            .FirstOrDefault();

        if (constantValue is not null)
            enumeratedValuesNodeData =
                new EnumeratedValuesNodeData(new[] { EntityValue.Create(constantValue.Value), });
        else
        {
            var enumValues = schema.Keywords.OfType<EnumKeyword>()
                .Select(x => x.Values)
                .FirstOrDefault();

            if (enumValues is not null)
                enumeratedValuesNodeData =
                    new EnumeratedValuesNodeData(enumValues.Select(EntityValue.Create).ToList());

            else
                enumeratedValuesNodeData = EnumeratedValuesNodeData.Empty;
        }

        switch (type.Type)
        {
            case SchemaValueType.Object:
            {
                var allowExtra = schema.Keywords?.OfType<AdditionalPropertiesKeyword>()
                    .Select(x => Create(x.Schema))
                    .FirstOrDefault() ?? TrueNode.Instance;

                var requiredProperties =
                    schema.Keywords?.OfType<RequiredKeyword>()
                        .SelectMany(x => x.Properties)
                        .ToHashSet() ?? new HashSet<string>();

                var nodes = schema.Keywords!.OfType<PropertiesKeyword>()
                    .SelectMany(x => x.Properties)
                    .ToDictionary(
                        x => x.Key,
                        x => (
                            Create(x.Value),
                            requiredProperties.Contains(x.Key)),
                        StringComparer.OrdinalIgnoreCase
                    );

                return new EntityNode(
                    enumeratedValuesNodeData,
                    new EntityAdditionalItems(allowExtra),
                    new EntityPropertiesData(nodes)
                );
            }
            case SchemaValueType.Array:
            {
                var prefixItems = schema.Keywords!.OfType<PrefixItemsKeyword>()
                    .SelectMany(x => x.ArraySchemas)
                    .Select(Create)
                    .ToImmutableList();

                var additionalItems = schema.Keywords!.OfType<AdditionalItemsKeyword>()
                    .Select(x => x.Schema)
                    .Select(Create)
                    .FirstOrDefault() ?? TrueNode.Instance;

                return new ArrayNode(
                    enumeratedValuesNodeData,
                    new ItemsData(prefixItems, additionalItems)
                );
            }
            case SchemaValueType.Boolean: return BooleanNode.Default;
            case SchemaValueType.String:
            {
                var format = StringFormat.Create(
                    schema.Keywords!.OfType<FormatKeyword>()
                        .Select(x => x.Value.Key)
                        .FirstOrDefault()
                );

                var restrictions = StringRestrictions.Create(schema);

                return new StringNode(enumeratedValuesNodeData, format, restrictions);
            }
            case SchemaValueType.Number:
                return new IntegerNode(enumeratedValuesNodeData, NumberRestrictions.Create(schema));
            case SchemaValueType.Integer:
                return new IntegerNode(enumeratedValuesNodeData, NumberRestrictions.Create(schema));
            case SchemaValueType.Null: return NullNode.Instance;
            default:                   throw new ArgumentOutOfRangeException(type.Type.ToString());
        }
    }

    /// <summary>
    /// Convert this to a Json Schema
    /// </summary>
    /// <returns></returns>
    [Pure]
    public JsonSchema ToJsonSchema()
    {
        var builder = new JsonSchemaBuilder().Type(SchemaValueType);

        foreach (var nodeData in NodeData)
        {
            nodeData.SetBuilder(builder);
        }

        return builder.Build();
    }

    /// <summary>
    /// Gets all node data for this Schema
    /// </summary>
    [Pure]
    public abstract IEnumerable<INodeData> NodeData { get; }

    /// <summary>
    /// The Schema Value type
    /// </summary>
    [Pure]
    public abstract SchemaValueType SchemaValueType { get; }

    /// <summary>
    /// Is this more general or equal to the other schema node
    /// </summary>
    [Pure]
    public abstract bool IsMorePermissive(SchemaNode other);

    /// <summary>
    /// Can this combine with the other node
    /// </summary>
    protected virtual bool CanCombineWith(SchemaNode other)
    {
        return GetType() == other.GetType();
    }

    /// <summary>
    /// Try to combine this node with another schema node
    /// </summary>
    [Pure]
    public virtual SchemaNode Combine(SchemaNode otherNode)
    {
        if (otherNode.IsMorePermissive(this))
            return otherNode;

        if (IsMorePermissive(otherNode))
            return this;

        if (CanCombineWith(otherNode))
        {
            var combineResult = EnumeratedValuesNodeData
                .Combine(otherNode.EnumeratedValuesNodeData);

            return this with { EnumeratedValuesNodeData = combineResult };
        }
        else if (otherNode.CanCombineWith(this))
        {
            var combineResult = otherNode.EnumeratedValuesNodeData
                .Combine(EnumeratedValuesNodeData);

            return otherNode with { EnumeratedValuesNodeData = combineResult };
        }

        return TrueNode.Instance;
    }

    /// <summary>
    /// Try to transform this entity to match this schema
    /// </summary>
    [Pure]
    public Result<Maybe<EntityValue>, IErrorBuilder> TryTransform(
        string propertyName,
        EntityValue entityValue,
        TransformSettings transformSettings)
    {
        if (!EnumeratedValuesNodeData.Allow(entityValue, transformSettings))
            return ErrorCode.SchemaViolation.ToErrorBuilder(
                $"Value not allowed: {entityValue}",
                propertyName
            );

        var r = TryTransform1(propertyName, entityValue, transformSettings);

        return r;
    }

    /// <summary>
    /// Try to transform the entity value
    /// </summary>
    protected abstract Result<Maybe<EntityValue>, IErrorBuilder> TryTransform1(
        string propertyName,
        EntityValue entityValue,
        TransformSettings transformSettings);
}

}
