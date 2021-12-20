using Reductech.Sequence.Core.Steps;
using StepParameterDict =
    System.Collections.Generic.Dictionary<Reductech.Sequence.Core.Internal.StepParameterReference,
        Reductech.Sequence.Core.Internal.FreezableStepProperty>;

namespace Reductech.Sequence.Core.Internal;

/// <summary>
/// Methods to create freezable types
/// </summary>
public static class FreezableFactory
{
    /// <summary>
    /// Create a new Freezable EntityGetValue
    /// </summary>
    public static IFreezableStep CreateFreezableArrayAccess(
        IFreezableStep entityOrArray,
        IFreezableStep indexer,
        TextLocation location)
    {
        var entityGetValueDict = new StepParameterDict
        {
            {
                new StepParameterReference.Named(nameof(EntityGetValue<SCLInt>.Entity)),
                new FreezableStepProperty.Step(entityOrArray, location)
            },
            {
                new StepParameterReference.Named(nameof(EntityGetValue<SCLInt>.Property)),
                new FreezableStepProperty.Step(indexer, location)
            },
        };

        var entityGetValueData = new FreezableStepData(entityGetValueDict, location);

        var entityGetValueStep = new CompoundFreezableStep(
            "EntityGetValue",
            entityGetValueData,
            location
        );

        var elementAtIndexDict = new StepParameterDict
        {
            {
                new StepParameterReference.Named(nameof(ArrayElementAtIndex<ISCLObject>.Array)),
                new FreezableStepProperty.Step(entityOrArray, location)
            },
            {
                new StepParameterReference.Named(nameof(ArrayElementAtIndex<ISCLObject>.Index)),
                new FreezableStepProperty.Step(indexer, location)
            },
        };

        var elementAtData = new FreezableStepData(elementAtIndexDict, location);

        var elementAtStep = new CompoundFreezableStep(
            "ArrayElementAtIndex",
            elementAtData,
            location
        );

        var result = new OptionFreezableStep(new[] { entityGetValueStep, elementAtStep }, location);
        return result;
    }

    /// <summary>
    /// Create a new Freezable Interpolated string
    /// </summary>
    public static IFreezableStep CreateFreezableInterpolatedString(
        IEnumerable<IFreezableStep> steps,
        TextLocation location)
    {
        var dict = new StepParameterDict
        {
            {
                new StepParameterReference.Named(nameof(StringInterpolate.Strings)),
                new FreezableStepProperty.StepList(steps.ToImmutableList(), location)
            },
        };

        var fpd = new FreezableStepData(dict, location);

        return new CompoundFreezableStep(
            nameof(StringInterpolate),
            fpd,
            location
        );
    }

    /// <summary>
    /// Create a new Freezable Sequence
    /// </summary>
    public static IFreezableStep CreateFreezableSequence(
        IEnumerable<IFreezableStep> steps,
        IFreezableStep finalStep,
        TextLocation location)
    {
        var dict = new StepParameterDict
        {
            {
                new StepParameterReference.Named(nameof(Sequence<ISCLObject>.InitialSteps)),
                new FreezableStepProperty.StepList(steps.ToImmutableList(), location)
            },
            {
                new StepParameterReference.Named(nameof(Sequence<ISCLObject>.FinalStep)),
                new FreezableStepProperty.Step(finalStep, location)
            },
        };

        var fpd = new FreezableStepData(dict, location);

        return new CompoundFreezableStep(
            "Sequence",
            fpd,
            location
        );
    }

    /// <summary>
    /// Create a freezable GetVariable step.
    /// </summary>
    public static IFreezableStep CreateFreezableGetVariable(
        VariableName variableName,
        TextLocation location)
    {
        var dict = new StepParameterDict
        {
            {
                new StepParameterReference.Named(nameof(GetVariable<ISCLObject>.Variable)),
                new FreezableStepProperty.Variable(variableName, location)
            }
        };

        var fpd = new FreezableStepData(dict, location);

        var step = new CompoundFreezableStep(
            "GetVariable",
            fpd,
            location
        );

        return step;
    }

    /// <summary>
    /// Create a freezable GetVariable step.
    /// </summary>
    public static IFreezableStep CreateFreezableSetVariable(
        FreezableStepProperty variableName,
        FreezableStepProperty value,
        TextLocation location)
    {
        var dict = new StepParameterDict
        {
            {
                new StepParameterReference.Named(nameof(SetVariable<ISCLObject>.Variable)),
                variableName
            },
            { new StepParameterReference.Named(nameof(SetVariable<ISCLObject>.Value)), value },
        };

        var fpd = new FreezableStepData(dict, location);

        var step = new CompoundFreezableStep(
            "SetVariable",
            fpd,
            location
        );

        return step;
    }

    /// <summary>
    /// Create a freezable Not step.
    /// </summary>
    public static IFreezableStep CreateFreezableNot(IFreezableStep boolean, TextLocation location)
    {
        var dict = new StepParameterDict
        {
            {
                new StepParameterReference.Named(nameof(Not.Boolean)),
                new FreezableStepProperty.Step(boolean, location)
            },
        };

        var fpd  = new FreezableStepData(dict, location);
        var step = new CompoundFreezableStep(nameof(Not), fpd, location);

        return step;
    }

    /// <summary>
    /// Create a new Freezable Array
    /// </summary>
    public static IFreezableStep CreateFreezableList(
        ImmutableList<IFreezableStep> elements,
        TextLocation location)
    {
        var dict = new StepParameterDict
        {
            {
                new StepParameterReference.Named(nameof(ArrayNew<ISCLObject>.Elements)),
                new FreezableStepProperty.StepList(elements, location)
            }
        };

        var fpd = new FreezableStepData(dict, location);

        return new CompoundFreezableStep(
            "ArrayNew",
            fpd,
            location
        );
    }
}

/// <summary>
/// Contains helper methods for creating infix steps
/// </summary>
public static class InfixHelper
{
    private record OperatorData1(string OperatorString, string StepName, string TermsName);

    /// <summary>
    /// Try to create an infix step
    /// </summary>
    public static Result<FreezableStepProperty, IError> TryCreateStep(
        TextLocation textLocation,
        string op,
        IReadOnlyList<Result<FreezableStepProperty, IError>> terms)
    {
        List<IError>                errors     = new();
        List<FreezableStepProperty> properties = new();

        foreach (var result in terms)
        {
            if (result.IsFailure)
                errors.Add(result.Error);
            else
                properties.Add(result.Value);
        }

        if (errors.Any())
            return Result.Failure<FreezableStepProperty, IError>(ErrorList.Combine(errors));

        var operatorData = OperatorLookup[op].ToList();

        if (!operatorData.Any())
            return new SingleError(
                textLocation,
                ErrorCode.CouldNotParse,
                op,
                "Operator"
            );

        List<IFreezableStep> freezableSteps = new();

        foreach (var (_, stepName, termsName) in operatorData)
        {
            var stepParameterDict = new StepParameterDict()
            {
                {
                    new StepParameterReference.Named(termsName), new FreezableStepProperty.StepList(
                        properties.Select(x => x.ConvertToStep()).ToImmutableList(),
                        textLocation
                    )
                }
            };

            var data = new FreezableStepData(
                stepParameterDict,
                textLocation
            );

            var step = new CompoundFreezableStep(stepName, data, textLocation);
            freezableSteps.Add(step);
        }

        if (freezableSteps.Count == 1)
            return new FreezableStepProperty.Step(freezableSteps.Single(), textLocation);

        var alt = new OptionFreezableStep(freezableSteps, textLocation);

        return new FreezableStepProperty.Step(alt, textLocation);
    }

    private static readonly ILookup<string, OperatorData1> OperatorLookup =
        new List<OperatorData1>()
            {
                new("+", "ArrayConcat", nameof(ArrayConcat<SCLInt>.Arrays)),
                new("+", nameof(Sum), nameof(Sum.Terms)),
                new("+", nameof(DoubleSum), nameof(DoubleSum.Terms)),
                new("-", nameof(Subtract), nameof(Subtract.Terms)),
                new("-", nameof(DoubleSubtract), nameof(Subtract.Terms)),
                new("*", nameof(Product), nameof(Product.Terms)),
                new("*", nameof(DoubleProduct), nameof(DoubleProduct.Terms)),
                new("/", nameof(Divide), nameof(Divide.Terms)),
                new("/", nameof(DoubleDivide), nameof(DoubleDivide.Terms)),
                new("%", nameof(Modulo), nameof(Modulo.Terms)),
                new("^", nameof(Power), nameof(Power.Terms)),
                new("^", nameof(DoublePower), nameof(DoublePower.Terms)),
                new("&&", nameof(And), nameof(And.Terms)),
                new("||", nameof(Or), nameof(Or.Terms)),
                new("==", "Equals", "Terms"),
                new("!=", "NotEquals", "Terms"),
                new("<", "LessThan", "Terms"),
                new("<=", "LessThanOrEqual", "Terms"),
                new(">", "GreaterThan", "Terms"),
                new(">=", "GreaterThanOrEqual", "Terms"),
                new("+", nameof(EntityCombine), nameof(EntityCombine.Terms)),
                new("+", nameof(StringJoin), nameof(StringJoin.Strings)),
            }
            .ToLookup(x => x.OperatorString, StringComparer.OrdinalIgnoreCase);
}
