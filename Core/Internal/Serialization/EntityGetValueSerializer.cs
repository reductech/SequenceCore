namespace Reductech.Sequence.Core.Internal.Serialization;

/// <summary>
/// Serializer for EntityGetValue
/// </summary>
public class EntityGetValueSerializer : IStepSerializer
{
    private EntityGetValueSerializer() { }

    /// <summary>
    /// The instance
    /// </summary>
    public static IStepSerializer Instance { get; } = new EntityGetValueSerializer();

    /// <inheritdoc />
    public string Serialize(SerializeOptions options, IEnumerable<StepProperty> stepProperties)
    {
        var (first, second) = stepProperties.GetFirstTwo().GetValueOrThrow();

        var entity = first.Serialize(options);

        var index = second.Serialize(options);

        return $"{entity}[{index}]";
    }
}
