using SPTarkov.Server.Core.Models.Common;

namespace DrakiaXYZ.GildedKeyStorage.Models;

public class CaseGrid
{
    public required int Width { get; set; }
    public required int Height { get; set; }
    public HashSet<MongoId>? IncludedFilter { get; set; }
    public HashSet<MongoId>? ExcludedFilter { get; set; }
}