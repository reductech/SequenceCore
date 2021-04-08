﻿using System;
using System.Collections.Generic;
using Reductech.EDR.Core.Internal;
using Reductech.EDR.Core.Internal.Errors;
using Reductech.EDR.Core.Steps;
using Reductech.EDR.Core.TestHarness;
using Reductech.EDR.Core.Util;
using static Reductech.EDR.Core.TestHarness.StaticHelpers;

namespace Reductech.EDR.Core.Tests.Steps
{

public partial class SequenceTests : StepTestBase<Sequence<StringStream>, StringStream>
{
    /// <inheritdoc />
    protected override IEnumerable<StepCase> StepCases
    {
        get
        {
            yield return new StepCase(
                "No initial steps",
                new Sequence<StringStream>
                {
                    InitialSteps = new List<IStep<Unit>>(), FinalStep = Constant("Goodbye")
                },
                "Goodbye"
            );

            yield return new StepCase(
                "Nested Sequence",
                new Sequence<StringStream>
                {
                    InitialSteps = new List<IStep<Unit>>(),
                    FinalStep = new Sequence<StringStream>
                    {
                        InitialSteps = new List<IStep<Unit>>(),
                        FinalStep    = Constant("Goodbye")
                    }
                },
                "Goodbye"
            );

            yield return new StepCase(
                "Log then Log",
                new Sequence<StringStream>
                {
                    InitialSteps = new List<IStep<Unit>>
                    {
                        new Log<StringStream> { Value = Constant("Hello") },
                        new Log<StringStream> { Value = Constant("World") }
                    },
                    FinalStep = Constant("Goodbye")
                },
                "Goodbye",
                "Hello",
                "World"
            );
        }
    }

    /// <inheritdoc />
    protected override IEnumerable<DeserializeCase> DeserializeCases
    {
        get
        {
            yield return new DeserializeCase(
                "Log then Log",
                "- Log Value: 'Hello'\n- Log Value: 'World'",
                (Unit.Default),
                "Hello",
                "World"
            );
        }
    }

    /// <inheritdoc />
    protected override IEnumerable<SerializeCase> SerializeCases
    {
        get
        {
            yield return new SerializeCase(
                "Short form",
                new Sequence<StringStream>
                {
                    InitialSteps = new List<IStep<Unit>>
                    {
                        new DoNothing(), new DoNothing(), new DoNothing()
                    },
                    FinalStep = Constant("Hello World")
                },
                $"- DoNothing{Environment.NewLine}- DoNothing{Environment.NewLine}- DoNothing{Environment.NewLine}- \"Hello World\"{Environment.NewLine}"
            );
        }
    }

    /// <inheritdoc />
    protected override IEnumerable<ErrorCase> ErrorCases
    {
        get
        {
            yield return new ErrorCase(
                "Initial steps error",
                new Sequence<StringStream>
                {
                    InitialSteps =
                        new List<IStep<Unit>>
                        {
                            new FailStep<Unit> { ErrorMessage = "Initial step Fail" }
                        },
                    FinalStep = Constant("Final")
                },
                new SingleError(
                    ErrorLocation.EmptyLocation,
                    ErrorCode.Test,
                    "Initial step Fail"
                )
            );

            yield return new ErrorCase(
                "Final steps error",
                new Sequence<StringStream>
                {
                    InitialSteps = new List<IStep<Unit>> { new DoNothing() },
                    FinalStep    = new FailStep<StringStream> { ErrorMessage = "Final step Fail" }
                },
                new SingleError(
                    ErrorLocation.EmptyLocation,
                    ErrorCode.Test,
                    "Final step Fail"
                )
            );
        }
    }
}

}
