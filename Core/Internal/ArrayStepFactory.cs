﻿using System;
using CSharpFunctionalExtensions;
using Reductech.EDR.Core.Internal.Errors;

namespace Reductech.EDR.Core.Internal
{

/// <summary>
/// Step factory for types which manipulate arrays
/// </summary>
public abstract class ArrayStepFactory : GenericStepFactory
{
    /// <summary>
    /// Possible array output types
    /// </summary>
    protected abstract Result<TypeReference, IErrorBuilder> GetExpectedArrayTypeReference(
        CallerMetadata callerMetadata);

    /// <summary>
    /// The name of the array property
    /// </summary>
    protected abstract string ArrayPropertyName { get; }

    /// <summary>
    /// The name of the lambda property
    /// </summary>
    protected abstract string? LambdaPropertyName { get; }

    /// <inheritdoc />
    protected override Result<TypeReference, IError> GetGenericTypeParameter(
        CallerMetadata callerMetadata,
        FreezableStepData freezableStepData,
        TypeResolver typeResolver)
    {
        var expectedMemberTypeReference =
            GetExpectedArrayTypeReference(callerMetadata)
                .MapError(x => x.WithLocation(freezableStepData));

        if (expectedMemberTypeReference.IsFailure)
            return expectedMemberTypeReference.ConvertFailure<TypeReference>();

        var step = freezableStepData.TryGetStep(ArrayPropertyName, StepType);

        if (step.IsFailure)
            return step.ConvertFailure<TypeReference>();

        var nestedCallerMetadata = new CallerMetadata(
            TypeName,
            ArrayPropertyName,
            expectedMemberTypeReference.Value
        );

        var outputTypeReference = step.Value.TryGetOutputTypeReference(
            nestedCallerMetadata,
            typeResolver
        );

        if (outputTypeReference.IsFailure)
            return outputTypeReference.ConvertFailure<TypeReference>();

        var arrayMemberType = outputTypeReference.Value
            .TryGetArrayMemberTypeReference(typeResolver)
            .MapError(e => e.WithLocation(freezableStepData));

        if (LambdaPropertyName is null)
            return arrayMemberType.Value;

        var lambda = freezableStepData.TryGetLambda(LambdaPropertyName, StepType);

        if (lambda.IsFailure)
            return lambda.ConvertFailure<TypeReference>();

        var nestedTypeResolver = typeResolver.TryCloneWithScopedLambda(
            lambda.Value,
            arrayMemberType.Value,
            callerMetadata
        );

        if (nestedTypeResolver.IsFailure)
            return nestedTypeResolver.ConvertFailure<TypeReference>();

        var realType = nestedTypeResolver.Value.Dictionary[lambda.Value.VariableNameOrItem];

        if (realType is null)
            throw new Exception("Could not expected type from type resolver");

        return realType;
    }
}

}
