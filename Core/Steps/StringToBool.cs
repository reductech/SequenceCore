﻿namespace Reductech.EDR.Core.Steps;

/// <summary>
/// Converts a string to a boolean
/// </summary>
[Alias("ToBool")]
[SCLExample("StringToBool 'true'",  "True")]
[SCLExample("StringToBool 'false'", "False")]
public sealed class StringToBool : CompoundStep<SCLBool>
{
    /// <summary>
    /// The string to convert to an integer
    /// </summary>
    [StepProperty(1)]
    [Required]
    [Alias("String")]
    public IStep<StringStream> Boolean { get; set; } = null!;

    /// <inheritdoc />
    protected override async Task<Result<bool, IError>> Run(
        IStateMonad stateMonad,
        CancellationToken cancellationToken)
    {
        var result = await Boolean.WrapStringStream().Run(stateMonad, cancellationToken);

        if (result.IsFailure)
            return result.ConvertFailure<bool>();

        if (bool.TryParse(result.Value, out var i))
        {
            return i;
        }

        return ErrorCode.CouldNotParse.ToErrorBuilder(result.Value, SCLType.Bool.ToString())
            .WithLocationSingle(this);
    }

    /// <inheritdoc />
    public override IStepFactory StepFactory { get; } = new SimpleStepFactory<StringToBool, bool>();
}
