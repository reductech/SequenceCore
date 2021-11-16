﻿using System.Collections.Generic;
using Generator.Equals;

namespace Reductech.EDR.Core.Internal.Analytics
{

/// <summary>
/// Detailed analysis of a Sequence Run
/// </summary>
[Equatable]
public partial record DetailedAnalysis(
    [property: OrderedEquality] IReadOnlyList<DetailedStepAnalysis> Steps)
{
    /// <inheritdoc />
    public override string ToString()
    {
        return string.Join("\r\n", Steps);
    }
}

}