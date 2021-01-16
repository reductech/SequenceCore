﻿using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Reductech.EDR.Core.Enums;
using Reductech.EDR.Core.ExternalProcesses;
using Reductech.EDR.Core.Internal;
using Reductech.EDR.Core.Internal.Errors;
using Reductech.EDR.Core.Steps;
using Reductech.EDR.Core.Util;
using Xunit;
using AutoTheory;
using Xunit.Abstractions;
using static Reductech.EDR.Core.TestHarness.StaticHelpers;

namespace Reductech.EDR.Core.Tests
{

public partial class RunErrorTests
{
    [GenerateAsyncTheory("ExpectError")]
    public IEnumerable<ErrorTestFunction> TestCases
    {
        get
        {
            yield return new ErrorTestFunction(
                "Get Missing Variable",
                new GetVariable<StringStream> { Variable = FooString },
                new ErrorBuilder(ErrorCode.MissingVariable, "<Foo>")
            );

            yield return new ErrorTestFunction(
                "ValueIf assert",
                new AssertTrue { Boolean = Constant(false) },
                new ErrorBuilder(ErrorCode.AssertionFailed, "False")
            );

            yield return new ErrorTestFunction(
                "Get variable with wrong type",
                new Sequence<Unit>
                {
                    InitialSteps = new IStep<Unit>[]
                    {
                        new SetVariable<int> { Variable = FooString, Value = Constant(42) },
                        new Print<bool>
                        {
                            Value = new GetVariable<bool> { Variable = FooString }
                        }
                    },
                    FinalStep = new DoNothing()
                },
                new ErrorBuilder(ErrorCode.WrongVariableType, "<Foo>", nameof(Boolean))
                    .WithLocation(new GetVariable<bool> { Variable = FooString })
            );

            yield return new ErrorTestFunction(
                "Assert Error with succeeding step",
                new AssertError { Step = new AssertTrue { Boolean = Constant(true) } },
                new ErrorBuilder(ErrorCode.AssertionFailed, nameof(AssertTrue))
            );

            yield return new ErrorTestFunction(
                "Divide by zero",
                new ApplyMathOperator
                {
                    Left     = Constant(1),
                    Right    = Constant(0),
                    Operator = Constant(MathOperator.Divide)
                },
                new ErrorBuilder(ErrorCode.DivideByZero)
            );

            yield return new ErrorTestFunction(
                "Array Index minus one",
                new ElementAtIndex<bool> { Array = Array(true), Index = Constant(-1) },
                new ErrorBuilder(ErrorCode.IndexOutOfBounds)
            );

            yield return new ErrorTestFunction(
                "Array Index out of bounds",
                new ElementAtIndex<bool>
                {
                    Array = new ArrayNew<bool>
                    {
                        Elements = new[] { Constant(true), Constant(false) }
                    },
                    Index = Constant(5)
                },
                new ErrorBuilder(ErrorCode.IndexOutOfBounds)
            );

            yield return new ErrorTestFunction(
                "Get letter minus one",
                new CharAtIndex { Index = Constant(-1), String = Constant("Foo") },
                new ErrorBuilder(ErrorCode.IndexOutOfBounds)
            );

            yield return new ErrorTestFunction(
                "Get letter out of bounds",
                new CharAtIndex { Index = Constant(5), String = Constant("Foo") },
                new ErrorBuilder(ErrorCode.IndexOutOfBounds)
            );

            yield return new ErrorTestFunction(
                "Get substring minus one",
                new GetSubstring
                {
                    Index = Constant(-1), String = Constant("Foo"), Length = Constant(10)
                },
                new ErrorBuilder(ErrorCode.IndexOutOfBounds)
            );

            yield return new ErrorTestFunction(
                "Get substring out of bounds",
                new GetSubstring
                {
                    Index = Constant(5), String = Constant("Foo"), Length = Constant(10)
                },
                new ErrorBuilder(ErrorCode.IndexOutOfBounds)
            );
        }
    }

    public static readonly VariableName FooString = new("Foo");

    public record ErrorTestFunction : IAsyncTestInstance
    {
        public ErrorTestFunction(string name, IStep process, IErrorBuilder expectedErrors)
        {
            Name           = name;
            Process        = process;
            ExpectedErrors = expectedErrors.WithLocation(process);
        }

        public ErrorTestFunction(string name, IStep process, IError expectedErrors)
        {
            Name           = name;
            Process        = process;
            ExpectedErrors = expectedErrors;
        }

        public string Name { get; }

        public IStep Process { get; }

        public IError ExpectedErrors { get; }

        /// <inheritdoc />
        public async Task RunAsync(ITestOutputHelper testOutputHelper)
        {
            var spf  = StepFactoryStore.CreateUsingReflection(typeof(IStep));
            var repo = new MockRepository(MockBehavior.Strict);

            using var state = new StateMonad(
                NullLogger.Instance,
                EmptySettings.Instance,
                repo.Create<IExternalProcessRunner>().Object,
                repo.Create<IFileSystemHelper>().Object,
                spf
            );

            var r = await Process.Run<object>(state, CancellationToken.None);

            r.IsFailure.Should().BeTrue("Step should have failed");

            r.Error.GetAllErrors().Should().BeEquivalentTo(ExpectedErrors.GetAllErrors());
        }

        /// <inheritdoc />
        public void Deserialize(IXunitSerializationInfo info) =>
            throw new System.NotImplementedException();

        /// <inheritdoc />
        public void Serialize(IXunitSerializationInfo info) => info.AddValue(nameof(Name), Name);
    }
}

}
