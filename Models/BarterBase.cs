using SPTarkov.Server.Core.Models.Eft.Common.Tables;

namespace DrakiaXYZ.GildedKeyStorage.Models;

public class BarterBase
{
    public required string Id { get; set; }
    public required string Name { get; set; }

    public required string Trader { get; set; }
    public required int TraderLoyaltyLevel { get; set; }
    public required bool UnlimitedStock { get; set; }
    public required int StockAmount { get; set; }
    public required List<BarterScheme> Barter { get; set; }
}