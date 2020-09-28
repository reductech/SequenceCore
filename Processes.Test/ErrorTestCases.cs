﻿using System.Collections.Generic;
using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using Reductech.EDR.Processes.General;
using Reductech.EDR.Processes.Internal;
using Reductech.EDR.Processes.Test.Extensions;
using Reductech.EDR.Processes.Util;
using Xunit.Abstractions;
using ITestCase = Reductech.EDR.Processes.Test.Extensions.ITestCase;

namespace Reductech.EDR.Processes.Test
{
    public class ErrorTestCases : TestBase
    {
        /// <inheritdoc />
        protected override IEnumerable<ITestCase> TestCases {
            get
            {
                yield return new ErrorTestCase("Get Missing Variable",
                    new GetVariable<string>
                    {
                        VariableName = FooString
                    },
                    new RunError($"Variable '<Foo>' does not exist.", "<Foo>", null, ErrorCode.MissingVariable));

                yield return new ErrorTestCase("Test assert",
                    new AssertTrue
                    {
                        Test = new Constant<bool>(false)
                    }, new RunError($"Assertion Failed '{false}'", "AssertTrue(Test: False)", null, ErrorCode.IndexOutOfBounds));


                yield return new ErrorTestCase("Get variable with wrong type",
                    new Sequence
                    {
                        Steps = new IStep<Unit>[]
                        {
                            new SetVariable<int>
                            {
                                VariableName = FooString,
                                Value = new Constant<int>(42)
                            },

                            new Print<bool>
                            {
                                Value = new GetVariable<bool>()
                                {
                                    VariableName =FooString
                                }
                            }
                        }
                    },
                    new RunError("Variable '<Foo>' does not have type 'System.Boolean'.", "<Foo>", null, ErrorCode.WrongVariableType)
                );

                yield return new ErrorTestCase("Assert Error with succeeding step",
                    new AssertError
                    {
                        Test = new AssertTrue
                        {
                            Test = new Constant<bool>(true)
                        }
                    },new RunError("Expected an error but step was successful.", "AssertError(Test: AssertTrue(Test: True))", null, ErrorCode.AssertionFailed));


            }
        }


        public static readonly VariableName FooString = new VariableName("Foo");

        private class ErrorTestCase : ITestCase
        {
            public ErrorTestCase(string name, IStep process, IRunErrors expectedErrors)
            {
                Name = name;
                Process = process;
                ExpectedErrors = expectedErrors;
            }

            /// <inheritdoc />
            public string Name { get; }


            public IStep Process { get; }

            public IRunErrors ExpectedErrors { get; }


            /// <inheritdoc />
            public void Execute(ITestOutputHelper testOutputHelper)
            {
                var state = new StateMonad(NullLogger.Instance, EmptySettings.Instance, ExternalProcessRunner.Instance);

                var r = Process.Run<object>(state);

                r.IsFailure.Should().BeTrue("Step should have failed");

                r.Error.AllErrors.Should().BeEquivalentTo(ExpectedErrors.AllErrors);

            }
        }
    }
}