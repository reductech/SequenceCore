﻿using System.Collections.Generic;
using System.Net;
using Moq.RestSharp.Helpers;
using Reductech.EDR.Core.Steps.REST;
using Reductech.EDR.Core.TestHarness;
using static Reductech.EDR.Core.TestHarness.StaticHelpers;

namespace Reductech.EDR.Core.Tests.Steps.REST
{

public partial class RESTGetStreamTests : StepTestBase<RESTGetStream, StringStream>
{
    /// <inheritdoc />
    protected override IEnumerable<StepCase> StepCases
    {
        get
        {
            yield return new StepCase(
                    "Basic Case",
                    new RESTGetStream() { URL = Constant("http://www.abc.com") },
                    new StringStream("abc")
                )
                .SetupHTTP(
                    x =>
                        x.MockApiResponse()
                            .WithStatusCode(HttpStatusCode.OK)
                            .ReturnsJsonString("abc")
                            .MockExecuteAsync()
                );
        }
    }
}

}
