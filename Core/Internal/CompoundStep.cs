﻿using Reductech.EDR.Core.Internal.Logging;

namespace Reductech.EDR.Core.Internal;

/// <summary>
/// A runnable step that is not a constant.
/// </summary>
public abstract class CompoundStep<T> : ICompoundStep<T> where T : ISCLObject
{
    /// <summary>
    /// Run this step.
    /// Does not activate logging.
    /// </summary>
    protected abstract Task<Result<T, IError>> Run(
        IStateMonad stateMonad,
        CancellationToken cancellationToken);

    /// <inheritdoc />
    async Task<Result<T, IError>> IRunnableStep<T>.Run(
        IStateMonad stateMonad,
        CancellationToken cancellationToken)
    {
        using (stateMonad.Logger.BeginScope(Name))
        {
            object[] GetEnterStepArgs()
            {
                var properties = AllProperties
                    .ToDictionary(x => x.Name, x => x.GetLogName());

                return new object[] { Name, properties };
            }

            LogSituation.EnterStep.Log(stateMonad, this, GetEnterStepArgs());

            var result = await Run(stateMonad, cancellationToken);

            if (result.IsFailure)
            {
                LogSituation.ExitStepFailure.Log(stateMonad, this, Name, result.Error.AsString);
            }
            else
            {
                var resultValue = SerializeOutput(result.Value);

                LogSituation.ExitStepSuccess.Log(stateMonad, this, Name, resultValue);
            }

            return result;

            static string SerializeOutput(object? o)
            {
                return o switch
                {
                    null            => "Null",
                    StringStream ss => ss.NameInLogs(false),
                    IArray array    => array.Name,
                    Unit            => "Unit",
                    _               => o.ToString()!
                };
            }
        }
    }

    /// <inheritdoc />
    public virtual Task<Result<T1, IError>> Run<T1>(
        IStateMonad stateMonad,
        CancellationToken cancellationToken) where T1 : ISCLObject
    {
        return Run(stateMonad, cancellationToken)
            .BindCast<T, T1, IError>(
                ErrorCode.InvalidCast.ToErrorBuilder(typeof(T), typeof(T1)).WithLocation(this)
            );
    }

    /// <summary>
    /// The factory used to create steps of this type.
    /// </summary>
    public abstract IStepFactory StepFactory { get; }

    /// <inheritdoc />
    public string Serialize() => StepFactory.Serializer.Serialize(AllProperties);

    /// <inheritdoc />
    public virtual string Name => StepFactory.TypeName;

    /// <inheritdoc />
    public override string ToString() => Name;

    /// <summary>
    /// The text location for this step.
    /// </summary>
    public TextLocation? TextLocation { get; set; }

    /// <inheritdoc />
    public virtual IEnumerable<Requirement> RuntimeRequirements
    {
        get
        {
            var connectorName = GetType().Assembly.GetName().Name!;

            var requirements = AllProperties
                .SelectMany(x => x.RequiredVersions)
                .Select(x => x.ToRequirement(connectorName))
                .ToList();

            var nestedRequirements = AllProperties.SelectMany(GetNestedRequirements).ToList();

            requirements.AddRange(nestedRequirements);

            var groupedRequirements = Requirement.CompressRequirements(requirements).ToList();
            return groupedRequirements;

            static IEnumerable<Requirement> GetNestedRequirements(StepProperty stepProperty)
            {
                return stepProperty switch
                {
                    StepProperty.SingleStepProperty ssp => ssp.Step.GetAllRequirements(),
                    StepProperty.StepListProperty slp => slp.StepList.SelectMany(
                        x => x.GetAllRequirements()
                    ),
                    _ => new List<Requirement>()
                };
            }
        }
    }

    /// <inheritdoc />
    public virtual Maybe<EntityValue> TryConvertToEntityValue() => Maybe<EntityValue>.None;

    /// <inheritdoc />
    public virtual bool ShouldBracketWhenSerialized => true;

    /// <inheritdoc />
    public Type OutputType => typeof(T);

    /// <summary>
    /// All properties of this step
    /// </summary>
    public IEnumerable<StepProperty> AllProperties
    {
        get
        {
            var r = GetType()
                .GetProperties()
                .Select(
                    propertyInfo => (propertyInfo,
                                     attribute: propertyInfo
                                         .GetCustomAttribute<StepPropertyBaseAttribute>())
                )
                .Where(x => x.attribute != null)
                .OrderByDescending(x => x.attribute!.Order != null)
                .ThenBy(x => x.attribute!.Order)
                .SelectMany((x, i) => GetMember(x, i).ToEnumerable());

            return r;

            Maybe<StepProperty> GetMember(
                (PropertyInfo propertyInfo, StepPropertyBaseAttribute? attribute) arg1,
                int index)
            {
                var (propertyInfo, _) = arg1;
                var val = propertyInfo.GetValue(this);

                var logAttribute = propertyInfo.GetCustomAttribute<LogAttribute>();

                var requiredVersions = propertyInfo.GetCustomAttributes<RequirementAttribute>()
                    .ToImmutableList();

                if (val is IStep step)
                {
                    return new StepProperty.SingleStepProperty(
                        step,
                        propertyInfo.Name,
                        index,
                        logAttribute,
                        requiredVersions
                    );
                }

                if (val is LambdaFunction lf)
                {
                    return new StepProperty.LambdaFunctionProperty(
                        lf,
                        propertyInfo.Name,
                        index,
                        logAttribute,
                        requiredVersions
                    );
                }

                if (val is VariableName vn)
                {
                    return new StepProperty.VariableNameProperty(
                        vn,
                        propertyInfo.Name,
                        index,
                        logAttribute,
                        requiredVersions
                    );
                }

                if (val is IEnumerable<IStep> enumerable)
                {
                    var list = enumerable.ToList();

                    return new StepProperty.StepListProperty(
                        list,
                        propertyInfo.Name,
                        index,
                        logAttribute,
                        requiredVersions
                    );
                }

                return Maybe<StepProperty>.None;
            }
        }
    }

    /// <summary>
    /// Check that this step meets requirements
    /// </summary>
    public virtual Result<Unit, IError> VerifyThis(StepFactoryStore stepFactoryStore) =>
        Unit.Default;

    /// <inheritdoc />
    public Result<Unit, IError> Verify(StepFactoryStore stepFactoryStore)
    {
        var r0 = new[] { VerifyThis(stepFactoryStore) };

        var rRequirements = StepFactory.Requirements.Concat(RuntimeRequirements)
            .Select(req => req.Check(stepFactoryStore).MapError(x => x.WithLocation(this)));

        var r3 = AllProperties
            .Select(
                x => x switch
                {
                    StepProperty.SingleStepProperty singleStepProperty => singleStepProperty.Step
                        .Verify(stepFactoryStore),
                    StepProperty.StepListProperty stepListProperty => stepListProperty.StepList
                        .Select(s => s.Verify(stepFactoryStore))
                        .Combine(ErrorList.Combine)
                        .Map(_ => Unit.Default),
                    StepProperty.LambdaFunctionProperty lambda => lambda.LambdaFunction.Step.Verify(
                        stepFactoryStore
                    ),

                    StepProperty.VariableNameProperty _ => Unit.Default,
                    _ => throw new ArgumentOutOfRangeException(nameof(x), x, null)
                }
            );

        var finalResult = r0.Concat(rRequirements)
            .Concat(r3)
            .Combine(ErrorList.Combine)
            .Map(_ => Unit.Default);

        return finalResult;
    }
}
