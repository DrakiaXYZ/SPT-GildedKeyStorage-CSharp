using DrakiaXYZ.GildedKeyStorage.Models;
using SPTarkov.DI.Annotations;
using SPTarkov.Server.Core.Utils;
using SPTarkov.Server.Core.Utils.Json;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.Json.Serialization;

namespace DrakiaXYZ.GildedKeyStorage.Helpers;

[Injectable(InjectionType.Singleton)]
public class CustomJsonUtil(IEnumerable<IJsonConverterRegistrator> registrators) : JsonUtil(registrators)
{
    private readonly JsonSerializerOptions options = new JsonSerializerOptions(JsonUtil.JsonSerializerOptionsNoIndent!)
    {
        UnmappedMemberHandling = JsonUnmappedMemberHandling.Skip,
        AllowTrailingCommas = true,
        PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower,
        PropertyNameCaseInsensitive = true,
    };

    public new T? Deserialize<T>(string? json)
    {
        return string.IsNullOrEmpty(json) ? default : JsonSerializer.Deserialize<T>(json, options);
    }

    public T? Deserialize<T>(JsonNode? node)
    {
        return node is null ? default : JsonSerializer.Deserialize<T>(node, options);
    }

    public JsonNode? SerializeToNode<T>(T? value)
    {
        return JsonSerializer.SerializeToNode(value, options);
    }
}