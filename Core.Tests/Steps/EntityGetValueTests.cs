﻿namespace Reductech.EDR.Core.Tests.Steps;

public partial class EntityGetValueTests : StepTestBase<EntityGetValue<StringStream>, StringStream>
{
    /// <inheritdoc />
    protected override IEnumerable<StepCase> StepCases
    {
        get
        {
            yield return new StepCase(
                "Get List Property",
                new EntityGetValue<StringStream>
                {
                    Entity   = Constant(Entity.Create(("Foo", new[] { "Hello", "World" }))),
                    Property = Constant("Foo")
                },
                "[\"Hello\", \"World\"]"
            );

            yield return new StepCase(
                "Get Enum Property",
                new EntityGetValue<StringStream>
                {
                    Entity   = Constant(Entity.Create(("Foo", TextCase.Lower))),
                    Property = Constant("Foo")
                },
                "Lower"
            );

            yield return new StepCase(
                "Entity is not a constant",
                new EntityGetValue<StringStream>()
                {
                    Entity = new CreateEntityStep(
                        new Dictionary<EntityPropertyKey, IStep>()
                        {
                            {
                                new EntityPropertyKey("Foo"),
                                new StringSubstring()
                                {
                                    String = Constant("Hello World"), Length = Constant(5)
                                }
                            }
                        }
                    ),
                    Property = Constant("Foo")
                },
                "Hello"
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
                "('Prop1': \"Val0\" 'Prop2': \"Val1\")[\"Bar2\"]"
            );
        }
    }
}

public partial class EntityGetValueTypeTests : StepTestBase<AssertTrue, Unit>
{
    private static Entity TheEntity => Entity.Create(
        ("String", "Hello"),
        ("Empty", null),
        ("BoolTrue", true),
        ("BoolFalse", false),
        ("Int", 123),
        ("Double", 12.3),
        ("Enum", Enums.TextCase.Lower),
        ("Date", new DateTime(2020, 1, 1))
    );

    /// <inheritdoc />
    protected override IEnumerable<StepCase> StepCases
    {
        get
        {
            yield return new StepCase(
                "Get Simple Property (String)",
                GetTestCase(Constant("Hello"), TheEntity, "String"),
                Unit.Default
            );

            yield return new StepCase(
                "Get Simple Property (Empty)",
                GetTestCase(Constant("null"), TheEntity, "Empty"),
                Unit.Default
            );

            yield return new StepCase(
                "Get Simple Property (Bool - True)",
                GetTestCase(Constant(true), TheEntity, "BoolTrue"),
                Unit.Default
            );

            yield return new StepCase(
                "Get Simple Property (Bool - False)",
                GetTestCase(Constant(false), TheEntity, "BoolFalse"),
                Unit.Default
            );

            yield return new StepCase(
                "Get Simple Property (Int)",
                GetTestCase(Constant(123), TheEntity, "Int"),
                Unit.Default
            );

            yield return new StepCase(
                "Get Simple Property (Double)",
                GetTestCase(Constant(12.3), TheEntity, "Double"),
                Unit.Default
            );

            yield return new StepCase(
                "Get Simple Property (Date)",
                GetTestCase(Constant(new DateTime(2020, 1, 1)), TheEntity, "Date"),
                Unit.Default
            );
        }
    }

    private static AssertTrue GetTestCase<T>(
        IStep<T> expected,
        Entity entity,
        string propertyName) where T : ISCLObject => new()
    {
        Boolean = new Equals<T>
        {
            Terms = new ArrayNew<T>
            {
                Elements = new List<IStep<T>>
                {
                    expected,
                    new EntityGetValue<T>
                    {
                        Entity   = Constant(entity),
                        Property = Constant(propertyName)
                    }
                }
            }
        }
    };
}
