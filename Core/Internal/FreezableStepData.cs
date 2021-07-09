﻿using System;
using System.Collections.Generic;
using System.Linq;
using CSharpFunctionalExtensions;
using Reductech.EDR.Core.Internal.Errors;
using StepParameterDict =
    System.Collections.Generic.IReadOnlyDictionary<
        Reductech.EDR.Core.Internal.StepParameterReference,
        Reductech.EDR.Core.Internal.FreezableStepProperty>;

namespace Reductech.EDR.Core.Internal
{

/// <summary>
/// The data used by a Freezable Step.
/// </summary>
public sealed class FreezableStepData
{
    /// <summary>
    /// Creates a new FreezableStepData
    /// </summary>
    public FreezableStepData(StepParameterDict stepProperties, TextLocation? location)
    {
        StepProperties = stepProperties;
        Location       = location;
    }

    /// <summary>
    /// The step properties.
    /// </summary>
    public StepParameterDict StepProperties { get; }

    /// <summary>
    /// The location where this data comes from.
    /// </summary>
    public TextLocation? Location { get; }

    private Result<T, IError> TryGetValue<T>(
        string propertyName,
        Type stepType,
        Func<FreezableStepProperty, Result<T, IError>> extractValue)
    {
        var property = stepType.GetProperty(propertyName);

        if (property == null)
            throw new Exception($"{stepType.Name} does not have property {propertyName}");

        foreach (var reference in StepParameterReference.GetPossibleReferences(property))
            if (StepProperties.TryGetValue(reference, out var value))
                return extractValue(value);

        return Result.Failure<T, IError>(
            ErrorCode.MissingParameter.ToErrorBuilder(propertyName)
                .WithLocation(new ErrorLocation(stepType.Name, Location))
        );
    }

    /// <summary>
    /// Gets a variable name.
    /// </summary>
    public Result<VariableName, IError> TryGetVariableName(string propertyName, Type stepType) =>
        TryGetValue(
            propertyName,
            stepType,
            x =>
                x.AsVariableName(propertyName)
        );

    /// <summary>
    /// Gets a step argument
    /// </summary>
    public Result<IFreezableStep, IError> TryGetStep(string propertyName, Type stepType) =>
        TryGetValue(
            propertyName,
            stepType,
            x =>
                Result.Success<IFreezableStep, IError>(x.ConvertToStep())
        );

    /// <summary>
    /// Gets a step argument
    /// </summary>
    public Result<FreezableStepProperty.Lambda, IError> TryGetLambda(
        string propertyName,
        Type stepType) => TryGetValue(
        propertyName,
        stepType,
        x =>
            Result.Success<FreezableStepProperty.Lambda, IError>(x.ConvertToLambda())
    );

    /// <summary>
    /// Gets a variable name.
    /// </summary>
    public Result<IReadOnlyList<IFreezableStep>, IError> TryGetStepList(
        string propertyName,
        Type stepType) => TryGetValue(propertyName, stepType, x => x.AsStepList(propertyName));

    /// <inheritdoc />
    public override string ToString()
    {
        var keyString = string.Join("; ", StepProperties);

        if (string.IsNullOrWhiteSpace(keyString))
            return "Empty";

        return keyString;
    }

    /// <summary>
    /// Gets the variables set by steps in this FreezableStepData.
    /// </summary>
    public Result<IReadOnlyCollection<(VariableName variableName, TypeReference)>, IError>
        GetVariablesSet(string stepName, CallerMetadata callerMetadata, TypeResolver typeResolver)
    {
        var variables = new List<(VariableName variableName, TypeReference)>();
        var errors    = new List<IError>();

        foreach (var (_, freezableStepProperty) in StepProperties)
        {
            switch (freezableStepProperty)
            {
                case FreezableStepProperty.Step step:
                    LocalGetVariablesSet(step.FreezableStep);
                    break;
                case FreezableStepProperty.StepList stepList:
                {
                    foreach (var step in stepList.List)
                        LocalGetVariablesSet(step);

                    break;
                }
                case FreezableStepProperty.Lambda lambda:
                {
                    GetVariablesSetByLambda(lambda.FreezableStep, lambda.VName);
                    break;
                }
                case FreezableStepProperty.Variable _: break;
                default:                               throw new ArgumentOutOfRangeException();
            }
        }

        if (errors.Any())
            return Result
                .Failure<IReadOnlyCollection<(VariableName variableName, TypeReference)>,
                    IError>(ErrorList.Combine(errors));

        return variables;

        void LocalGetVariablesSet(IFreezableStep freezableStep)
        {
            var variablesSet = freezableStep.GetVariablesSet(callerMetadata, typeResolver);

            if (variablesSet.IsFailure)
                errors.Add(variablesSet.Error);
            else
                variables.AddRange(variablesSet.Value);
        }

        void GetVariablesSetByLambda(IFreezableStep freezableStep, VariableName? lambdaVariable)
        {
            var vn           = lambdaVariable ?? VariableName.Item;
            var variablesSet = freezableStep.GetVariablesSet(callerMetadata, typeResolver);

            if (variablesSet.IsFailure)
                errors.Add(variablesSet.Error);
            else
                variables.AddRange(variablesSet.Value.Where(x => x.variableName != vn));
        }
    }
}

}
