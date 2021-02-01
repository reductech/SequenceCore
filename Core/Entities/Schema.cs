﻿using System;
using System.Collections.Generic;
using System.Linq;
using CSharpFunctionalExtensions;
using Microsoft.Extensions.Logging;
using Reductech.EDR.Core.Attributes;
using Reductech.EDR.Core.Enums;
using Reductech.EDR.Core.Internal.Errors;
using Reductech.EDR.Core.Internal.Logging;
using Reductech.EDR.Core.Util;

namespace Reductech.EDR.Core.Entities
{

/// <summary>
/// An entity schema.
/// Enforces that the entity matches certain constraints.
/// </summary>
public sealed class Schema
{
    /// <summary>
    /// The name of the schema.
    /// </summary>
    [ConfigProperty(1)]
    public string Name { get; set; } = null!;

    /// <summary>
    /// The schema properties.
    /// </summary>
    [ConfigProperty(2)]
    public Dictionary<string, SchemaProperty> Properties { get; set; } =
        null!; //public setter for deserialization

    /// <summary>
    /// Whether properties other than the explicitly defined properties are allowed.
    /// </summary>
    [ConfigProperty(3)]
    public bool AllowExtraProperties { get; set; } = true;

    /// <summary>
    /// The default error behaviour. This can be overriden by the individual properties or by the value passed to the EnforceSchema method.
    /// </summary>
    [ConfigProperty(4)]
    public ErrorBehaviour DefaultErrorBehaviour { get; set; } = ErrorBehaviour.Fail;

    /// <inheritdoc />
    public override string ToString()
    {
        // ReSharper disable once ConstantNullCoalescingCondition
        return Name ?? "Schema";
    }

    /// <summary>
    /// Attempts to apply this schema to an entity.
    /// </summary>
    public Result<Maybe<Entity>, IErrorBuilder> ApplyToEntity(
        Entity entity,
        ILogger logger,
        Maybe<ErrorBehaviour> errorBehaviourOverride)
    {
        var remainingProperties = Properties
            .ToDictionary(
                x => x.Key,
                x => x.Value,
                StringComparer.OrdinalIgnoreCase
            );

        var newProperties = new List<EntityProperty>();
        var errors        = new List<IErrorBuilder>();
        var warnings      = new List<IErrorBuilder>();
        var changed       = false;
        var returnEntity  = true;

        void HandleError(IErrorBuilder error, ErrorBehaviour eb)
        {
            switch (eb)
            {
                case ErrorBehaviour.Fail:
                    errors.Add(error);
                    break;
                case ErrorBehaviour.Error:
                    warnings.Add(error);
                    returnEntity = false;
                    break;
                case ErrorBehaviour.Warning:
                    warnings.Add(error);
                    break;
                case ErrorBehaviour.Skip:
                    returnEntity = false;
                    break;
                case ErrorBehaviour.Ignore: break;
                default: throw new ArgumentOutOfRangeException(nameof(eb), eb, null);
            }
        }

        ErrorBehaviour generalErrorBehaviour;

        if (errorBehaviourOverride.HasValue)
            generalErrorBehaviour = errorBehaviourOverride.Value;
        else
            generalErrorBehaviour = this.DefaultErrorBehaviour;

        foreach (var entityProperty in entity)
        {
            if (remainingProperties.Remove(entityProperty.Name, out var schemaProperty))
            {
                var convertResult = entityProperty.BestValue.TryConvert(schemaProperty);

                if (convertResult.IsSuccess)
                {
                    if (convertResult.Value.changed)
                    {
                        changed = true;

                        var newProperty = new EntityProperty(
                            entityProperty.Name,
                            entityProperty.BaseValue,
                            convertResult.Value.value,
                            entityProperty.Order
                        );

                        newProperties.Add(newProperty);
                    }
                    else
                    {
                        newProperties.Add(entityProperty);
                    }
                }

                else
                {
                    ErrorBehaviour errorBehaviour;

                    if (errorBehaviourOverride.HasValue)
                        errorBehaviour = errorBehaviourOverride.Value;
                    else if (schemaProperty.ErrorBehaviour != null)
                        errorBehaviour = schemaProperty.ErrorBehaviour.Value;
                    else
                        errorBehaviour = DefaultErrorBehaviour;

                    HandleError(convertResult.Error, errorBehaviour);
                }
            }
            else if (AllowExtraProperties) //This entity has a property that is not in the schema
                newProperties.Add(entityProperty);
            else
            {
                var errorBuilder = new ErrorBuilder(
                    ErrorCode.SchemaViolationUnexpectedProperty,
                    entityProperty.Name
                );

                HandleError(errorBuilder, generalErrorBehaviour);
            }
        }

        foreach (var (key, _) in remainingProperties
            .Where(
                x => x.Value.Multiplicity == Multiplicity.ExactlyOne
                  || x.Value.Multiplicity == Multiplicity.AtLeastOne
            ))
        {
            var error = new ErrorBuilder(ErrorCode.SchemaViolationMissingProperty, key);
            HandleError(error, generalErrorBehaviour);
        }

        if (errors.Any())
        {
            var errorList = ErrorBuilderList.Combine(errors);
            return Result.Failure<Maybe<Entity>, IErrorBuilder>(errorList);
        }

        if (warnings.Any())
        {
            var warningList = ErrorBuilderList.Combine(warnings);

            logger.LogSituation(LogSituation.SchemaViolation, new[] { warningList.AsString });
        }

        if (!returnEntity)
            return Maybe<Entity>.None;

        if (!changed)
            return Maybe<Entity>.From(entity);

        var resultEntity = new Entity(newProperties);

        return Maybe<Entity>.From(resultEntity);
    }

    /// <summary>
    /// Tries to create a schema from an entity.
    /// Ignores unexpected properties.
    /// </summary>
    public static Result<Schema, IErrorBuilder> TryCreateFromEntity(Entity entity)
    {
        var results = new List<Result<Unit, IErrorBuilder>>();
        var schema  = new Schema();

        results.Add(entity.TrySetString(false, nameof(Name), s => schema.Name = s));

        results.Add(
            entity.TrySetBoolean(
                true,
                nameof(AllowExtraProperties),
                s => schema.AllowExtraProperties = s
            )
        );

        results.Add(
            entity.TrySetEnum<ErrorBehaviour>(
                true,
                nameof(DefaultErrorBehaviour),
                eb => schema.DefaultErrorBehaviour = eb
            )
        );

        results.Add(
            entity.TrySetDictionary(
                true,
                nameof(Properties),
                ev =>
                {
                    var childEntity = ev.TryGetEntity();

                    if (childEntity.HasValue)
                        return SchemaProperty.TryCreateFromEntity(childEntity.Value);

                    return new ErrorBuilder(
                        ErrorCode.InvalidCast,
                        ev,
                        nameof(SchemaProperty)
                    );
                },
                d => schema.Properties = d
            )
        );

        var r = results.Combine(ErrorBuilderList.Combine)
            .Map(_ => schema);

        return r;
    }

    /// <summary>
    /// Converts a schema to an entity for deserialization
    /// </summary>
    public Entity ConvertToEntity()
    {
        var propertiesEntity =
            new Entity(
                Properties.Select(
                    (x, i) =>
                        new EntityProperty(
                            x.Key,
                            new EntityValue(x.Value.ConvertToEntity()),
                            null,
                            i
                        )
                )
            );

        var topProperties = new[]
        {
            (nameof(Name), EntityValue.CreateFromObject(Name)),
            (nameof(AllowExtraProperties), EntityValue.CreateFromObject(AllowExtraProperties)),
            (nameof(DefaultErrorBehaviour),
             EntityValue.CreateFromObject(DefaultErrorBehaviour)),
            (nameof(Properties), EntityValue.CreateFromObject(propertiesEntity)),
        }.Select((x, i) => new EntityProperty(x.Item1, x.Item2, null, i));

        var entity = new Entity(topProperties);

        return entity;
    }
}

}
