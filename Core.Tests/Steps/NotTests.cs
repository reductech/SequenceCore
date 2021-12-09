﻿using System.Collections.Generic;
using Reductech.EDR.Core.Steps;
using Reductech.EDR.Core.TestHarness;
using static Reductech.EDR.Core.TestHarness.StaticHelpers;

namespace Reductech.EDR.Core.Tests.Steps;

public partial class NotTests : StepTestBase<Not, bool>
{
    /// <inheritdoc />
    protected override IEnumerable<StepCase> StepCases
    {
        get
        {
            yield return new StepCase("Not True",  new Not { Boolean = Constant(true) },  false);
            yield return new StepCase("Not False", new Not { Boolean = Constant(false) }, true);
        }
    }

    /// <inheritdoc />
    protected override IEnumerable<DeserializeCase> DeserializeCases
    {
        get
        {
            yield return new DeserializeCase("Ordered argument", "not true",          false);
            yield return new DeserializeCase("Named argument",   "not boolean: true", false);
        }
    }
}
