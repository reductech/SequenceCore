﻿using System.ComponentModel.DataAnnotations;
using System.Threading;
using System.Threading.Tasks;
using CSharpFunctionalExtensions;
using Reductech.EDR.Core.Attributes;
using Reductech.EDR.Core.Internal;
using Reductech.EDR.Core.Internal.Errors;
using Reductech.EDR.Core.Util;

namespace Reductech.EDR.Core.Steps
{

/// <summary>
/// Do an action for each value of &lt;i&gt; in a range.
/// </summary>
public sealed class For : CompoundStep<Unit>
{
    /// <summary>
    /// The first value of the variable to use.
    /// </summary>
    [StepProperty(1)]
    [Required]
    public IStep<int> From { get; set; } = null!;

    /// <summary>
    /// The highest value of the variable to use
    /// </summary>
    [StepProperty(2)]
    [Required]
    public IStep<int> To { get; set; } = null!;

    /// <summary>
    /// The amount to increment by each iteration.
    /// </summary>
    [StepProperty(3)]
    [Required]
    public IStep<int> Increment { get; set; } = null!;

    /// <summary>
    /// The action to perform repeatedly.
    /// </summary>
    [StepProperty(4)]
    [ScopedFunction]
    [Required]
    public IStep<Unit> Action { get; set; } = null!;

    /// <summary>
    /// The name of the variable to use within the action.
    /// </summary>
    [VariableName(5)]
    [DefaultValueExplanation("<i>")]

    public VariableName Variable { get; set; } = VariableName.Index;

    /// <inheritdoc />
    protected override async Task<Result<Unit, IError>> Run(
        IStateMonad stateMonad,
        CancellationToken cancellationToken)
    {
        var variableName = VariableName.Index;

        var from = await From.Run(stateMonad, cancellationToken);

        if (from.IsFailure)
            return from.ConvertFailure<Unit>();

        var to = await To.Run(stateMonad, cancellationToken);

        if (to.IsFailure)
            return to.ConvertFailure<Unit>();

        var increment = await Increment.Run(stateMonad, cancellationToken);

        if (increment.IsFailure)
            return increment.ConvertFailure<Unit>();

        var currentValue = from.Value;

        var setResult = await stateMonad.SetVariableAsync(variableName, currentValue, false, this);

        if (setResult.IsFailure)
            return setResult.ConvertFailure<Unit>();

        if (increment.Value == 0)
            return new SingleError(new ErrorLocation(this), ErrorCode.DivideByZero);

        while (increment.Value > 0 ? currentValue <= to.Value : currentValue >= to.Value)
        {
            var r = await Action.Run(stateMonad, cancellationToken);

            if (r.IsFailure)
                return r;

            var currentValueResult = stateMonad.GetVariable<int>(variableName)
                .MapError(e => e.WithLocation(this));

            if (currentValueResult.IsFailure)
                return currentValueResult.ConvertFailure<Unit>();

            currentValue =  currentValueResult.Value;
            currentValue += increment.Value;

            var setResult2 = await stateMonad.SetVariableAsync(
                variableName,
                currentValue,
                false,
                this
            );

            if (setResult2.IsFailure)
                return setResult.ConvertFailure<Unit>();
        }

        await stateMonad.RemoveVariableAsync(VariableName.Index, false, this);

        return Unit.Default;
    }

    /// <inheritdoc />
    public override Result<TypeResolver, IError> TryGetScopedTypeResolver(
        TypeResolver baseTypeResolver,
        IFreezableStep scopedStep)
    {
        return baseTypeResolver.TryCloneWithScopedStep(
            Variable,
            TypeReference.Actual.Integer,
            TypeReference.Unit.Instance,
            scopedStep,
            new ErrorLocation(this)
        );
    }

    /// <inheritdoc />
    public override IStepFactory StepFactory { get; } = new SimpleStepFactory<For, Unit>();
}

}
