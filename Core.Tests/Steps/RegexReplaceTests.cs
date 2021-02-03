﻿using System.Collections.Generic;
using Reductech.EDR.Core.Internal;
using Reductech.EDR.Core.Steps;
using Reductech.EDR.Core.TestHarness;
using static Reductech.EDR.Core.TestHarness.StaticHelpers;

namespace Reductech.EDR.Core.Tests.Steps
{

public partial class RegexReplaceTests : StepTestBase<RegexReplace, string>
{
    /// <inheritdoc />
    protected override IEnumerable<StepCase> StepCases
    {
        get
        {
            yield return new StepCase(
                "Simple Replace",
                new RegexReplace()
                {
                    String     = Constant("Number 1"),
                    Pattern    = Constant(@"\d+"),
                    IgnoreCase = Constant(false),
                    Function = new StringJoin()
                    {
                        Strings = new ArrayNew<StringStream>
                        {
                            Elements = new List<IStep<StringStream>>
                            {
                                Constant("_"),
                                GetVariable<StringStream>(VariableName.Match),
                                Constant("_")
                            }
                        }
                    }
                },
                "Number _1_"
            );
        }
    }
}

}
