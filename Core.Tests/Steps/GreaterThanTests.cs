﻿using Reductech.EDR.Core.TestHarness;

namespace Reductech.EDR.Core.Tests.Steps;

public partial class GreaterThanTests : StepTestBase<GreaterThan<int>, bool>
{
    /// <inheritdoc />
    protected override IEnumerable<StepCase> StepCases
    {
        get
        {
            yield return new StepCase(
                "One number",
                new GreaterThan<int>() { Terms = StaticHelpers.Array(2) },
                true
            );

            yield return new StepCase(
                "Two numbers false",
                new GreaterThan<int>() { Terms = StaticHelpers.Array(2, 3) },
                false
            );

            yield return new StepCase(
                "Two numbers true",
                new GreaterThan<int>() { Terms = StaticHelpers.Array(3, 2) },
                true
            );

            yield return new StepCase(
                "Three numbers true",
                new GreaterThan<int>() { Terms = StaticHelpers.Array(3, 2, 1) },
                true
            );
        }
    }

    /// <inheritdoc />
    protected override IEnumerable<SerializeCase> SerializeCases
    {
        get
        {
            var (step, _) = CreateStepWithDefaultOrArbitraryValues();

            yield return new SerializeCase(
                "Default",
                step,
                @"0 > 1 > 2"
            );
        }
    }
}
