
using SPTarkov.Server.Core.Models.Common;
using SPTarkov.Server.Core.Models.Eft.Common.Tables;

namespace DrakiaXYZ.GildedKeyStorage.Models;

public class Case : BarterBase
{
    public required string CaseType { get; set; }

    public required string ItemName { get; set; }
    public required string ItemShortName { get; set; }
    public required string ItemDescription { get; set; }
    public required string Bundle { get; set; }
    public required int FleaPrice { get; set; }
    public string? Sound { get; set; }

    public required Size ExternalSize { get; set; }

    public List<CaseGrid>? Grids { get; set; }
    public List<MongoId>? SlotIds { get; set; }

    public class Size
    {
        public required int Width { get; set; }
        public required int Height { get; set; }
    }
}
