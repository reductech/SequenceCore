﻿using System;
using CSharpFunctionalExtensions;
using Reductech.EDR.Core.Internal;
using Reductech.EDR.Core.Internal.Errors;

namespace Reductech.EDR.Core.Steps
{

/// <summary>
/// Base class for generic operations
/// </summary>
public abstract class
    GenericBaseOperatorStep<TStep, TElement, TOutput> : BaseOperatorStep<TStep, TElement, TOutput>
    where TStep : BaseOperatorStep<TStep, TElement, TOutput>, new()
{
    /// <inheritdoc />
    public override IStepFactory StepFactory { get; } = new GenericBaseOperatorStepFactory();

    /// <summary>
    /// Compares two items.
    /// </summary>
    public sealed class GenericBaseOperatorStepFactory : GenericStepFactory
    {
        /// <inheritdoc />
        public override Type StepType => typeof(TStep).GetGenericTypeDefinition();

        /// <inheritdoc />
        public override string OutputTypeExplanation => nameof(Boolean);

        /// <inheritdoc />
        protected override ITypeReference
            GetOutputTypeReference(ITypeReference memberTypeReference) =>
            new ActualTypeReference(typeof(bool));

        /// <inheritdoc />
        protected override Result<ITypeReference, IError> GetMemberType(
            FreezableStepData freezableStepData,
            TypeResolver typeResolver)
        {
            var result = freezableStepData
                .TryGetStep(nameof(Terms), StepType)
                .Bind(x => x.TryGetOutputTypeReference(typeResolver))
                .Bind(
                    x => x.TryGetGenericTypeReference(typeResolver, 0)
                        .MapError(e => e.WithLocation(freezableStepData))
                )
                .Map(x => x as ITypeReference);

            return result;
        }

        ///// <inheritdoc />
        //public override IStepSerializer Serializer => new StepSerializer(
        //    TypeName,
        //    new StepComponent(nameof(Compare<int>.Left)),
        //    SpaceComponent.Instance,
        //    new EnumDisplayComponent<CompareOperator>(nameof(Compare<int>.Operator)),
        //    SpaceComponent.Instance,
        //    new StepComponent(nameof(Compare<int>.Right))
        //);
    }
}

}
