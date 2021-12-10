﻿using Reductech.EDR.Core.TestHarness;

namespace Reductech.EDR.Core.Tests.Steps;

public partial class ArrayFilterTests : StepTestBase<ArrayFilter<Entity>, Array<Entity>>
{
    /// <inheritdoc />
    protected override IEnumerable<StepCase> StepCases
    {
        get
        {
            yield return new StepCase(
                "Filter stuff",
                new ForEach<Entity>
                {
                    Action = new LambdaFunction<Entity, Unit>(
                        null,
                        new Log<Entity> { Value = GetEntityVariable }
                    ),
                    Array = new ArrayFilter<Entity>
                    {
                        Array = Array(
                            Entity.Create(("Foo", "Alpha")),
                            Entity.Create(("Bar", "Alpha")),
                            Entity.Create(("Foo", "ALPHA")),
                            Entity.Create(("Foo", "Beta")),
                            Entity.Create(("Bar", "Beta"))
                        ),
                        Predicate = new LambdaFunction<Entity, bool>(
                            null,
                            new EntityHasProperty()
                            {
                                Property = Constant("Foo"), Entity = GetEntityVariable
                            }
                        )
                    }
                },
                Unit.Default,
                Entity.Create(("Foo", "Alpha")).Serialize(),
                Entity.Create(("Foo", "ALPHA")).Serialize(),
                Entity.Create(("Foo", "Beta")).Serialize()
            );

            yield return new StepCase(
                "Filter stuff with custom variable name",
                new ForEach<Entity>
                {
                    Action = new LambdaFunction<Entity, Unit>(
                        new VariableName("ForeachVar"),
                        new Log<Entity> { Value = GetVariable<Entity>("ForeachVar") }
                    ),
                    Array = new ArrayFilter<Entity>
                    {
                        Array = Array(
                            Entity.Create(("Foo", "Alpha")),
                            Entity.Create(("Bar", "Alpha")),
                            Entity.Create(("Foo", "ALPHA")),
                            Entity.Create(("Foo", "Beta")),
                            Entity.Create(("Bar", "Beta"))
                        ),
                        Predicate = new LambdaFunction<Entity, bool>(
                            new VariableName("FilterVar"),
                            new EntityHasProperty()
                            {
                                Property = Constant("Foo"),
                                Entity   = GetVariable<Entity>("FilterVar")
                            }
                        )
                    }
                },
                Unit.Default,
                Entity.Create(("Foo", "Alpha")).Serialize(),
                Entity.Create(("Foo", "ALPHA")).Serialize(),
                Entity.Create(("Foo", "Beta")).Serialize()
            );
        }
    }

    /// <inheritdoc />
    protected override IEnumerable<ErrorCase> ErrorCases
    {
        get
        {
            //Do not do default cases as some errors are not propagated due to lazy evaluation

            yield return new ErrorCase(
                "EntityStream is error",
                new ArrayFilter<Entity>()
                {
                    Array     = new FailStep<Array<Entity>>() { ErrorMessage = "Stream Fail" },
                    Predicate = new LambdaFunction<Entity, bool>(null, Constant(true))
                },
                new SingleError(
                    ErrorLocation.EmptyLocation,
                    ErrorCode.Test,
                    "Stream Fail"
                )
            );
        }
    }
}
