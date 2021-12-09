﻿using Reductech.EDR.Core.TestHarness;

namespace Reductech.EDR.Core.Tests.Steps;

public partial class EntityCombineTests : StepTestBase<EntityCombine, Entity>
{
    /// <inheritdoc />
    protected override IEnumerable<StepCase> StepCases
    {
        get
        {
            yield return new StepCase(
                "Combine two simple entities",
                new EntityCombine
                {
                    Terms = Array(Entity.Create(("Foo", 1)), Entity.Create(("Bar", 2)))
                },
                Entity.Create(("Foo", 1), ("Bar", 2))
            );

            yield return new StepCase(
                "Combine two simple entities with property override",
                new EntityCombine
                {
                    Terms = Array(
                        Entity.Create(("Foo", 1), ("Bar", 1)),
                        Entity.Create(("Bar", 2))
                    )
                },
                Entity.Create(("Foo", 1), ("Bar", 2))
            );

            yield return new StepCase(
                "Combine nested entities",
                new EntityCombine
                {
                    Terms = Array(
                        Entity.Create(("Foo", Entity.Create(("Bar", 2)))),
                        Entity.Create(("Foo", Entity.Create(("Baz", 3))))
                    )
                },
                Entity.Create(("Foo", Entity.Create(("Bar", 2), ("Baz", 3))))
            );
        }
    }

    /// <inheritdoc />
    protected override IEnumerable<SerializeCase> SerializeCases
    {
        get
        {
            var (step, _) = CreateStepWithDefaultOrArbitraryValues();

            yield return new SerializeCase(
                "Default",
                step,
                @"('Prop1': ""Val0"" 'Prop2': ""Val1"") + ('Prop1': ""Val2"" 'Prop2': ""Val3"") + ('Prop1': ""Val4"" 'Prop2': ""Val5"")"
            );
        }
    }
}
