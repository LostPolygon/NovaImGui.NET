using System.Text.Json;
using System.Text.Json.Serialization;

namespace NovaImGuiNet.Generator.Metadata;

public static class MetadataDtoDeserializer {
    private static readonly JsonSerializerOptions JsonSerializerOptions;

    static MetadataDtoDeserializer() {
        JsonSerializerOptions = new JsonSerializerOptions(JsonSerializerDefaults.General) {
            PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower,
            UnmappedMemberHandling = JsonUnmappedMemberHandling.Disallow
        };
        JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter(JsonNamingPolicy.SnakeCaseLower));
    }

    public static MetadataDto Deserialize(string json) =>
        JsonSerializer.Deserialize<MetadataDto>(json, JsonSerializerOptions) ??
        throw new JsonException($"Failed to deserialize {nameof(MetadataDto)}");
}
