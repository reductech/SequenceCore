﻿using Reductech.EDR.Core.Enums;

namespace Reductech.EDR.Core.Steps;

/// <summary>
/// Reads the Console standard input
/// </summary>
[Alias("FromStandardIn")]
[Alias("ReadStandardIn")]
[Alias("FromStdIn")]
[Alias("ReadStdIn")]
[Alias("StdInRead")]
public class StandardInRead : CompoundStep<StringStream>
{
    /// <inheritdoc />
    protected override async Task<Result<StringStream, IError>> Run(
        IStateMonad stateMonad,
        CancellationToken cancellationToken)
    {
        await ValueTask.CompletedTask;

        var ss = new StringStream(
            stateMonad.ExternalContext.Console.OpenStandardInput(),
            EncodingEnum.UTF8
        );

        return ss;
    }

    /// <inheritdoc />
    public override IStepFactory StepFactory { get; } =
        new SimpleStepFactory<StandardInRead, StringStream>();
}