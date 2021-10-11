﻿using System.Collections.Generic;
using System.Net;
using Moq.RestSharp.Helpers;
using Reductech.EDR.Core.Steps.REST;
using Reductech.EDR.Core.TestHarness;
using Reductech.EDR.Core.Util;

namespace Reductech.EDR.Core.Tests.Steps.REST
{

public partial class RESTPutTests : StepTestBase<RESTPut, Unit>
{
    /// <inheritdoc />
    protected override IEnumerable<StepCase> StepCases
    {
        get
        {
            yield return new StepCase(
                    "Basic Case",
                    new RESTPut()
                    {
                        URL    = StaticHelpers.Constant("http://www.abc.com"),
                        Entity = StaticHelpers.Constant(Entity.Create(("a", 123)))
                    },
                    Unit.Default
                )
                .SetupHTTP(
                    x =>
                        x.MockApiResponse()
                            .WithStatusCode(HttpStatusCode.OK)
                            .MockExecuteAsync()
                );
        }
    }
}

}
