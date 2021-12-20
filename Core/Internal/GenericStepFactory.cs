namespace Reductech.Sequence.Core.Internal;

/// <summary>
/// Step factory for generic types.
/// </summary>
public abstract class GenericStepFactory : StepFactory
{
    /// <inheritdoc />
    public override Result<TypeReference, IError> TryGetOutputTypeReference(
        CallerMetadata callerMetadata,
        FreezableStepData freezableStepData,
        TypeResolver typeResolver) => GetGenericTypeParameter(
            callerMetadata,
            freezableStepData,
            typeResolver
        )
        .Map(GetOutputTypeReference);

    /// <summary>
    /// Gets the output type from the member type.
    /// </summary>
    protected abstract TypeReference GetOutputTypeReference(TypeReference memberTypeReference);

    /// <inheritdoc />
    protected override Result<ICompoundStep, IError> TryCreateInstance(
        CallerMetadata callerMetadata,
        FreezableStepData freezeData,
        TypeResolver typeResolver)
    {
        var genericTypeParameter = GetGenericTypeParameter(
            callerMetadata,
            freezeData,
            typeResolver
        );

        if (genericTypeParameter.IsFailure)
        {
            return genericTypeParameter.ConvertFailure<ICompoundStep>();
        }

        var result = genericTypeParameter.Value.TryGetType(typeResolver)
            .Bind(x => TryCreateGeneric(StepType, x))
            .MapError(e => e.WithLocation(freezeData));

        return result;
    }

    /// <summary>
    /// Gets the type
    /// </summary>
    protected abstract Result<TypeReference, IError> GetGenericTypeParameter(
        CallerMetadata callerMetadata,
        FreezableStepData freezableStepData,
        TypeResolver typeResolver);
}
