﻿using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.ComponentModel.DataAnnotations;
using System.Threading;
using System.Threading.Tasks;
using CSharpFunctionalExtensions;
using Reductech.EDR.Core.Attributes;
using Reductech.EDR.Core.Internal;
using Reductech.EDR.Core.Internal.Errors;

namespace Reductech.EDR.Core.Steps
{

/// <summary>
/// Removes duplicate entities.
/// </summary>
[Alias("Distinct")]
[SCLExample("ArrayDistinct [1,2,2,3] (<>=> $\"{<>}\")", "[1, 2, 3]")]
[SCLExample(
    "- ArrayDistinct <a> (<>=> $\"{<>}\")",
    "[1, 2, 3]",
    null,
    new[] { "a" },
    new[] { "[1,2,2,3]" }
)]
public sealed class ArrayDistinct<T> : CompoundStep<Array<T>>
{
    /// <inheritdoc />
    protected override async Task<Result<Array<T>, IError>> Run(
        IStateMonad stateMonad,
        CancellationToken cancellationToken)
    {
        var entityStreamResult = await Array.Run(stateMonad, cancellationToken);

        if (entityStreamResult.IsFailure)
            return entityStreamResult.ConvertFailure<Array<T>>();

        var ignoreCaseResult = await IgnoreCase.Run(stateMonad, cancellationToken);

        if (ignoreCaseResult.IsFailure)
            return ignoreCaseResult.ConvertFailure<Array<T>>();

        IEqualityComparer<string> comparer = ignoreCaseResult.Value
            ? StringComparer.OrdinalIgnoreCase
            : StringComparer.Ordinal;

        HashSet<string> usedKeys = new(comparer);

        var currentState = stateMonad.GetState().ToImmutableDictionary();

        async IAsyncEnumerable<T> Filter(T element)
        {
            await using var scopedMonad = new ScopedStateMonad(
                stateMonad,
                currentState,
                KeySelector.VariableNameOrItem,
                new KeyValuePair<VariableName, object>(KeySelector.VariableNameOrItem, element!)
            );

            var result = await KeySelector.StepTyped.Run(scopedMonad, cancellationToken)
                .Map(async x => await x.GetStringAsync());

            if (result.IsFailure)
                throw new ErrorException(result.Error);

            if (usedKeys.Add(result.Value))
                yield return element;
        }

        var newStream = entityStreamResult.Value.SelectMany(Filter);

        return newStream;
    }

    /// <summary>
    /// The array to sort
    /// </summary>
    [StepProperty(1)]
    [Required]
    public IStep<Array<T>> Array { get; set; } = null!;

    /// <summary>
    /// A function that gets the key to distinct by from the variable
    /// To distinct by multiple properties, concatenate several keys
    /// </summary>
    [FunctionProperty(2)]
    [Required]
    public LambdaFunction<T, StringStream> KeySelector { get; set; } = null!;

    /// <summary>
    /// Whether to ignore case when comparing strings.
    /// </summary>
    [StepProperty(3)]
    [DefaultValueExplanation("False")]
    public IStep<bool> IgnoreCase { get; set; } = new BoolConstant(false);

    /// <inheritdoc />
    public override IStepFactory StepFactory => ArrayDistinctStepFactory.Instance;

    /// <summary>
    /// Removes duplicate entities.
    /// </summary>
    private sealed class ArrayDistinctStepFactory : ArrayStepFactory
    {
        private ArrayDistinctStepFactory() { }

        /// <summary>
        /// The Instance
        /// </summary>
        public static GenericStepFactory Instance { get; } = new ArrayDistinctStepFactory();

        /// <inheritdoc />
        protected override TypeReference
            GetOutputTypeReference(TypeReference memberTypeReference) =>
            new TypeReference.Array(memberTypeReference);

        /// <inheritdoc />
        protected override Result<TypeReference, IErrorBuilder> GetExpectedArrayTypeReference(
            CallerMetadata callerMetada)
        {
            return callerMetada.ExpectedType;
        }

        /// <inheritdoc />
        protected override string ArrayPropertyName => nameof(ArrayDistinct<object>.Array);

        /// <inheritdoc />
        protected override string LambdaPropertyName => nameof(ArrayDistinct<object>.KeySelector);

        /// <inheritdoc />
        public override Type StepType => typeof(ArrayDistinct<>);

        /// <inheritdoc />
        public override string OutputTypeExplanation => "Array of T";
    }
}

}
