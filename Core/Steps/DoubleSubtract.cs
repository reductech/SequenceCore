﻿using System.Collections.Generic;
using CSharpFunctionalExtensions;
using Reductech.EDR.Core.Internal.Errors;

namespace Reductech.EDR.Core.Steps;

/// <summary>
/// Subtract a list of numbers from a number
/// </summary>
public sealed class DoubleSubtract : BaseOperatorStep<DoubleSubtract, double, double>
{
    /// <inheritdoc />
    protected override Result<double, IErrorBuilder> Operate(IEnumerable<double> terms)
    {
        double total = 0;
        var    first = true;

        foreach (var number in terms)
        {
            if (first)
            {
                total += number;
                first =  false;
            }
            else
            {
                total -= number;
            }
        }

        return total;
    }

    /// <inheritdoc />
    public override string Operator => "-";
}
