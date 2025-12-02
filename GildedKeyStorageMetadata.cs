

using SPTarkov.Server.Core.Models.Spt.Mod;

public record GildedKeyStorageMetadata : AbstractModMetadata
{
    public override string ModGuid { get; init; } = "xyz.drakia.gildedkeystorage";
    public override string Name { get; init; } = "Gilded Key Storage";
    public override string Author { get; init; } = "DrakiaXYZ";
    public override List<string>? Contributors { get; init; } = new List<string>() { "Jehree" };
    public override SemanticVersioning.Version Version { get; init; } = new("2.0.4");
    public override SemanticVersioning.Range SptVersion { get; init; } = new("~4.0.0");
    public override List<string>? Incompatibilities { get; init; }
    public override Dictionary<string, SemanticVersioning.Range>? ModDependencies { get; init; }
    public override string? Url { get; init; }
    public override bool? IsBundleMod { get; init; } = true;
    public override string License { get; init; } = "CC-BY-NC-ND";
}