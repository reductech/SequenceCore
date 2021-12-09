﻿using System.Collections.Generic;
using Reductech.EDR.Core.Enums;
using Reductech.EDR.Core.Steps;
using Reductech.EDR.Core.TestHarness;
using static Reductech.EDR.Core.TestHarness.StaticHelpers;

namespace Reductech.EDR.Core.Tests.Steps;

public partial class StringToCaseTests : StepTestBase<StringToCase, StringStream>
{
    /// <inheritdoc />
    protected override IEnumerable<StepCase> StepCases
    {
        get
        {
            yield return new StepCase(
                "To upper",
                new StringToCase() { Case = Constant(TextCase.Upper), String = Constant("hello") },
                "HELLO"
            );

            yield return new StepCase(
                "To lower",
                new StringToCase() { Case = Constant(TextCase.Lower), String = Constant("HELLO") },
                "hello"
            );

            yield return new StepCase(
                "To title",
                new StringToCase() { Case = Constant(TextCase.Title), String = Constant("hELLo") },
                "Hello"
            );
        }
    }

    /// <inheritdoc />
    protected override IEnumerable<DeserializeCase> DeserializeCases
    {
        get
        {
            yield return new DeserializeCase(
                "To title",
                "StringToCase Case: TextCase.Title String: 'hELLo'",
                "Hello"
            );
        }
    }
}
