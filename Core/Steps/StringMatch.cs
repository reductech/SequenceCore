﻿using System.Text.RegularExpressions;

namespace Reductech.EDR.Core.Steps;

/// <summary>
/// Returns true if a string is matched by a particular regular expression
/// </summary>
[SCLExample("StringMatch String: 'aaaabbbbccc' Pattern: 'a+b+c+'", "True")]
[SCLExample("IsMatch String: 'abracadabra' Regex: 'ab\\w+?ab'",    "True")]
[Alias("IsMatch")]
[Alias("RegexMatch")]
public sealed class StringMatch : CompoundStep<bool>
{
    /// <inheritdoc />
    protected override async Task<Result<bool, IError>> Run(
        IStateMonad stateMonad,
        CancellationToken cancellationToken)
    {
        var stringResult =
            await String.Run(stateMonad, cancellationToken).Map(x => x.GetStringAsync());

        if (stringResult.IsFailure)
            return stringResult.ConvertFailure<bool>();

        var patternResult =
            await Pattern.Run(stateMonad, cancellationToken).Map(x => x.GetStringAsync());

        if (patternResult.IsFailure)
            return patternResult.ConvertFailure<bool>();

        var ignoreCaseResult = await IgnoreCase.Run(stateMonad, cancellationToken);

        if (ignoreCaseResult.IsFailure)
            return ignoreCaseResult.ConvertFailure<bool>();

        var regexOptions = RegexOptions.None;

        if (ignoreCaseResult.Value)
            regexOptions |= RegexOptions.IgnoreCase;

        var isMatch = Regex.IsMatch(stringResult.Value, patternResult.Value, regexOptions);

        return isMatch;
    }

    /// <summary>
    /// The string to match
    /// </summary>
    [StepProperty(1)]
    [Required]
    public IStep<StringStream> String { get; set; } = null!;

    /// <summary>
    /// The regular expression pattern.
    /// Uses the .net flavor
    /// </summary>
    [StepProperty(2)]
    [Required]
    [Alias("Regex")]
    public IStep<StringStream> Pattern { get; set; } = null!;

    /// <summary>
    /// Whether the regex should ignore case.
    /// </summary>
    [StepProperty()]
    [DefaultValueExplanation("False")]
    public IStep<bool> IgnoreCase { get; set; } = new BoolConstant(false);

    /// <inheritdoc />
    public override IStepFactory StepFactory { get; } = new SimpleStepFactory<StringMatch, bool>();
}
