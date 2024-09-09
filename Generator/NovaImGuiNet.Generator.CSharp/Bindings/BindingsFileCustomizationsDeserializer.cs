using System.Text.Json;
using System.Text.Json.Serialization;

namespace NovaImGuiNet.Generator.CSharp;

public static class BindingsFileCustomizationsDeserializer {
    private static readonly JsonSerializerOptions JsonSerializerOptions;

    static BindingsFileCustomizationsDeserializer() {
        JsonSerializerOptions = new JsonSerializerOptions(JsonSerializerDefaults.General) {
            PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower,
            UnmappedMemberHandling = JsonUnmappedMemberHandling.Disallow
        };
        JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter(JsonNamingPolicy.SnakeCaseLower));
    }

    public static BindingsFileCustomizations Deserialize(string json) =>
        JsonSerializer.Deserialize<BindingsFileCustomizations>(json, JsonSerializerOptions) ??
        throw new JsonException($"Failed to deserialize {nameof(BindingsFileCustomizations)}");
}
