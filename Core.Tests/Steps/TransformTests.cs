﻿using System.Globalization;
using static Reductech.EDR.Core.TestHarness.SchemaHelpers;

namespace Reductech.EDR.Core.Tests.Steps;

public partial class TransformTests : StepTestBase<Transform, Array<Entity>>
{
    #pragma warning disable CA1822 // Mark members as static
    partial void OnInitialized()
        #pragma warning restore CA1822 // Mark members as static
    {
        CultureInfo.DefaultThreadCurrentCulture   = new CultureInfo("en-GB");
        CultureInfo.DefaultThreadCurrentUICulture = new CultureInfo("en-GB");
    }

    const string SchemaName = "My Schema";

    /// <inheritdoc />
    protected override IEnumerable<StepCase> StepCases
    {
        get
        {
            static StepCase CreateCase(
                string name,
                List<Entity> entities,
                JsonSchema schema,
                Action<Transform> setTransform,
                params string[] expectedLogValues)
            {
                var schemaEntity = schema.ConvertToEntity();

                var transform = new Transform
                {
                    EntityStream = Array(entities.ToArray()), Schema = Constant(schemaEntity),
                };

                setTransform(transform);

                return new StepCase(
                    name,
                    new ForEach<Entity>
                    {
                        Action = new LambdaFunction<Entity, Unit>(
                            null,
                            new Log { Value = GetEntityVariable }
                        ),
                        Array = transform
                    },
                    Unit.Default,
                    expectedLogValues
                );
            }

            yield return CreateCase(
                "Simple case",
                new List<Entity>
                {
                    Entity.Create(("Foo", "Hello"),   ("Bar", 1)),
                    Entity.Create(("Foo", "Hello 2"), ("Bar", 2))
                },
                new JsonSchemaBuilder()
                    .Title(SchemaName)
                    .AdditionalItems(false)
                    .Properties(
                        ("foo", AnyString),
                        ("Bar", AnyInt)
                    ),
                _ => { },
                "('Foo': \"Hello\" 'Bar': 1)",
                "('Foo': \"Hello 2\" 'Bar': 2)"
            );

            yield return CreateCase(
                "Transform Integers",
                new List<Entity> { Entity.Create(("Bar", 1)), Entity.Create(("Bar", "2")) },
                new JsonSchemaBuilder()
                    .Title(SchemaName)
                    .AdditionalItems(false)
                    .Properties(("Bar", AnyInt)),
                _ => { },
                "('Bar': 1)",
                "('Bar': 2)"
            );

            yield return CreateCase(
                "Transform List",
                new List<Entity> { Entity.Create(("Foo", new object[] { "1", "2", "3" })) },
                new JsonSchemaBuilder()
                    .Title(SchemaName)
                    .Type(SchemaValueType.Object)
                    .Properties(
                        ("Foo", new JsonSchemaBuilder().Type(SchemaValueType.Array).Items(AnyInt))
                    ),
                _ => { },
                "('Foo': [1, 2, 3])"
            );

            yield return CreateCase(
                "Transform Nested Property",
                new List<Entity> { Entity.Create(("Foo", Entity.Create(("Bar", "1")))) },
                new JsonSchemaBuilder()
                    .Title(SchemaName)
                    .Type(SchemaValueType.Object)
                    .AdditionalItems(false)
                    .Properties(
                        ("Foo",
                         new JsonSchemaBuilder().Type(SchemaValueType.Object)
                             .Properties(("Bar", AnyInt))
                             .Build())
                    ),
                _ => { },
                "('Foo': ('Bar': 1))"
            );

            yield return CreateCase(
                "Transform DateTime",
                new List<Entity> { Entity.Create(("Bar", new DateTime(1990, 1, 6))) },
                new JsonSchemaBuilder()
                    .Title(SchemaName)
                    .AdditionalItems(false)
                    .Properties(("Bar", AnyDateTime)),
                _ => { },
                "('Bar': 1990-01-06T00:00:00.0000000)"
            );

            yield return CreateCase(
                "Transform DateTime as string",
                new List<Entity> { Entity.Create(("Bar", new DateTime(1990, 1, 6).ToString("O"))) },
                new JsonSchemaBuilder()
                    .Title(SchemaName)
                    .AdditionalItems(false)
                    .Properties(("Bar", AnyDateTime)),
                _ => { },
                "('Bar': 1990-01-06T00:00:00.0000000)"
            );

            yield return CreateCase(
                "Transform DateTime as string with format",
                new List<Entity> { Entity.Create(("Bar", "1990/01/06")) },
                new JsonSchemaBuilder()
                    .Title(SchemaName)
                    .AdditionalItems(false)
                    .Properties(("Bar", AnyDateTime)),
                x =>
                {
                    x.DateInputFormats =
                        new OneOfStep<StringStream, Array<StringStream>, Entity>(
                            Constant("yyyy/MM/dd")
                        );
                },
                "('Bar': 1990-01-06T00:00:00.0000000)"
            );

            yield return CreateCase(
                "Transform DateTime as string with different formats with different props",
                new List<Entity>
                {
                    Entity.Create(
                        ("Foo", "21/10/1988"),
                        ("Bar", "1990/01/06")
                    )
                },
                new JsonSchemaBuilder()
                    .Title(SchemaName)
                    .AdditionalItems(false)
                    .Properties(
                        ("Foo", AnyDateTime),
                        ("Bar", AnyDateTime)
                    ),
                x =>
                {
                    x.DateInputFormats =
                        new OneOfStep<StringStream, Array<StringStream>, Entity>(
                            Constant(
                                Entity.Create(
                                    ("Foo", "dd/MM/yyyy"),
                                    ("Bar", "yyyy/MM/dd")
                                )
                            )
                        );
                },
                "('Foo': 1988-10-21T00:00:00.0000000 'Bar': 1990-01-06T00:00:00.0000000)"
            );
        }
    }

    /// <inheritdoc />
    protected override IEnumerable<ErrorCase> ErrorCases
    {
        get
        {
            static ErrorCase CreateCase(
                string name,
                List<Entity> entities,
                JsonSchema schema,
                ErrorCode expectedErrorCode,
                params object[] expectedErrorArgs)
            {
                var schemaEntity = schema.ConvertToEntity();

                var enforceSchema = new Transform
                {
                    EntityStream  = Array(entities.ToArray()),
                    Schema        = Constant(schemaEntity),
                    ErrorBehavior = Constant(ErrorBehavior.Fail)
                };

                return new ErrorCase(
                    name,
                    new Sequence<Unit>
                    {
                        InitialSteps = new List<IStep<Unit>>
                        {
                            new ForEach<Entity>
                            {
                                Action = new LambdaFunction<Entity, Unit>(
                                    null,
                                    new Log { Value = GetEntityVariable }
                                ),
                                Array = enforceSchema,
                            }
                        },
                        FinalStep = new DoNothing()
                    },
                    new ErrorBuilder(expectedErrorCode, expectedErrorArgs).WithLocation(
                        new ErrorLocation(enforceSchema)
                    )
                );
            }

            yield return CreateCase(
                "Cannot Transform DateTime",
                new List<Entity> { Entity.Create(("Bar", "1990--01--06")) },
                new JsonSchemaBuilder()
                    .Title(SchemaName)
                    .AdditionalItems(false)
                    .Properties(("Bar", AnyDateTime)),
                ErrorCode.SchemaViolation,
                "Should be DateTime",
                ".Bar"
            );

            foreach (var baseErrorCase in base.ErrorCases)
                yield return baseErrorCase;
        }
    }
}
