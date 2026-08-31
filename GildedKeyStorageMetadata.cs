

using SPTarkov.Server.Core.Models.Spt.Mod;

public record GildedKeyStorageMetadata : IModMetadata
{
    public string ModGuid { get; init; } = "xyz.drakia.gildedkeystorage";
    public string Name { get; init; } = "Gilded Key Storage";
    public string Author { get; init; } = "DrakiaXYZ";
    public List<string>? Contributors { get; init; } = new List<string>() { "Jehree" };
    public SemanticVersioning.Version Version { get; init; } = new("2.1.1");
    public SemanticVersioning.Range SptVersion { get; init; } = new("~4.1.0");
    public Dictionary<string, SemanticVersioning.Range>? ModDependencies { get; init; }
    public bool? IsBundleMod { get; init; } = true;
    public string License { get; init; } = "CC-BY-NC-ND";
    public bool HasPrepatcher { get; init; } = false;
    public List<string>? Incompatibilities { get; init; }
    public string? Url { get; init; }
}