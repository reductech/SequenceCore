﻿using System.Collections.Generic;
using Reductech.EDR.Core.Steps;
using Reductech.EDR.Core.TestHarness;
using Xunit.Abstractions;

namespace Reductech.EDR.Core.Tests.Steps
{
    public class JoinStringsTests : StepTestBase<JoinStrings, string>
    {
        /// <inheritdoc />
        public JoinStringsTests(ITestOutputHelper testOutputHelper) : base(testOutputHelper)
        {
        }

        /// <inheritdoc />
        protected override IEnumerable<StepCase> StepCases
        {
            get
            {
                yield return new StepCase("Join some strings",
                    new JoinStrings
                    {
                        Delimiter = Constant(", "),
                        List = Array("Hello", "World")
                    }, "Hello, World"
                    );

            }
        }

        /// <inheritdoc />
        protected override IEnumerable<DeserializeCase> DeserializeCases
        {
            get
            {
                yield return new DeserializeCase("Join some strings",
                    "JoinStrings(Delimiter = ', ', List = ['Hello', 'World'])"
                    ,"Hello, World"
                );



            }

        }

    }
}