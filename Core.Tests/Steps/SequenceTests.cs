﻿using System.Collections.Generic;
using Reductech.EDR.Core.Internal;
using Reductech.EDR.Core.Steps;
using Reductech.EDR.Core.TestHarness;
using Reductech.EDR.Core.Util;
using Xunit.Abstractions;

namespace Reductech.EDR.Core.Tests.Steps
{
    public class SequenceTests : StepTestBase<Sequence, Unit>
    {
        /// <inheritdoc />
        public SequenceTests(ITestOutputHelper testOutputHelper) : base(testOutputHelper)
        {
        }

        /// <inheritdoc />
        protected override IEnumerable<StepCase> StepCases
        {
            get
            {
                yield return new StepCase("Print then print", new Sequence
                {
                    Steps = new List<IStep<Unit>>
                    {
                        new Print<string> {Value = Constant("Hello")},
                        new Print<string> {Value = Constant("World")}
                    }
                }, Unit.Default, "Hello", "World" );


            }
        }

        /// <inheritdoc />
        protected override IEnumerable<DeserializeCase> DeserializeCases
        {
            get
            {
                yield return new DeserializeCase("Print then print", "- Print(Value = 'Hello')\n- Print(Value = 'World')", Unit.Default, "Hello", "World");
            }

        }

    }
}