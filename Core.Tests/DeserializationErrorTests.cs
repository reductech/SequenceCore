﻿using System.Collections.Generic;
using System.Linq;
using CSharpFunctionalExtensions;
using FluentAssertions;
using Reductech.EDR.Core.Internal;
using Reductech.EDR.Core.Internal.Errors;
using Reductech.EDR.Core.Internal.Parser;
using Reductech.EDR.Core.Internal.Serialization;
using Reductech.EDR.Core.Parser;
using Reductech.Utilities.Testing;
using Xunit;
using Xunit.Abstractions;

namespace Reductech.EDR.Core.Tests
{

public class DeserializationErrorTests : DeserializationErrorTestCases
{
    public DeserializationErrorTests(ITestOutputHelper testOutputHelper) =>
        TestOutputHelper = testOutputHelper;

    /// <inheritdoc />
    [Theory()]
    [ClassData(typeof(DeserializationErrorTestCases))]
    public override void Test(string key) => base.Test(key);
}

public class DeserializationErrorTestCases : TestBase
{
    /// <inheritdoc />
    protected override IEnumerable<ITestBaseCase> TestCases
    {
        get
        {
            yield return new DeserializationErrorCase(
                "",
                ("SCL is empty.", EntireSequenceLocation.Instance.AsString)
            );

            yield return new DeserializationErrorCase(
                "\"Print 123\"",
                ("An SCL Sequence should have a final return type of Unit. Try wrapping your sequence with 'Print'.",
                 "Print 123")
            );

            yield return new DeserializationErrorCase(
                "'Print 123'",
                ("An SCL Sequence should have a final return type of Unit. Try wrapping your sequence with 'Print'.",
                 "Print 123")
            );

            yield return new DeserializationErrorCase(
                "Print Value: 'Hello' Value: 'World'",
                ("Duplicate Parameter: Value.",
                 "Line: 1, Col: 0, Idx: 0 - Line: 1, Col: 34, Idx: 34 Text: Print Value: 'Hello' Value: 'World'")
            );

            yield return new DeserializationErrorCase(
                "Print Value: 'hello' Term: 'world'",
                ("Unexpected Parameter 'Term' in 'Print'",
                 "Line: 1, Col: 0, Idx: 0 - Line: 1, Col: 33, Idx: 33 Text: Print Value: 'hello' Term: 'world'")
            );

            yield return new DeserializationErrorCase(
                "Print(['abc', '123'] == ['abc', '123'])",
                ("Type ArrayOfStringStream is not comparable and so cannot be used for sorting.",
                 "Line: 1, Col: 6, Idx: 6 - Line: 1, Col: 37, Idx: 37 Text: ['abc', '123'] == ['abc', '123']")
            );

            yield return new DeserializationErrorCase(
                "MyMegaFunction true",
                ("The step 'MyMegaFunction' does not exist",
                 "Line: 1, Col: 0, Idx: 0 - Line: 1, Col: 18, Idx: 18 Text: MyMegaFunction true")
            );

            yield return new DeserializationErrorCase(
                "Print (2 + 2",
                ("Syntax Error: missing ')' at '<EOF>'",
                 "Line: 1, Col: 12, Idx: 12 - Line: 1, Col: 11, Idx: 11 Text: <EOF>")
            );

            yield return new DeserializationErrorCase(
                "Foreach ['one', 'two'] (Print (<Entity> + 1))",
                ("'Left' cannot take the value 'Get <Entity>'", "ApplyMathOperator")
            );

            yield return new DeserializationErrorCase(
                "Foreach ['one', 'two'] (Print (<Num> + 1)) <Num>",
                ("'Left' cannot take the value 'Get <Num>'", "ApplyMathOperator")
            );
        }
    }

    private class DeserializationErrorCase : ITestBaseCase
    {
        private readonly (string error, string location)[] _expectedErrors;

        public DeserializationErrorCase(
            string scl,
            params (string error, string location)[] expectedErrors)
        {
            Name            = scl;
            _expectedErrors = expectedErrors;
        }

        /// <inheritdoc />
        public string Name { get; }

        /// <inheritdoc />
        public void Execute(ITestOutputHelper testOutputHelper)
        {
            var sfs = StepFactoryStore.CreateUsingReflection(typeof(IFreezableStep));

            var result = SCLParsing.ParseSequence(Name)
                .Bind(x => x.TryFreeze(sfs))
                .Bind(SCLRunner.ConvertToUnitStep);

            result.IsFailure.Should().BeTrue("Case should fail");

            var realErrorPairs =
                result.Error.GetAllErrors().Select(x => (x.Message, x.Location.AsString)).ToArray();

            realErrorPairs.Should().BeEquivalentTo(_expectedErrors);
        }
    }
}

}
