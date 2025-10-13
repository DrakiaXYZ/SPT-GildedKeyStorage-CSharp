using DrakiaXYZ.GildedKeyStorage.Models;
using SPTarkov.DI.Annotations;
using SPTarkov.Server.Core.Helpers;
using SPTarkov.Server.Core.Models.Common;
using SPTarkov.Server.Core.Models.Eft.Common.Tables;
using SPTarkov.Server.Core.Models.Logging;
using SPTarkov.Server.Core.Models.Utils;
using SPTarkov.Server.Core.Services;

namespace DrakiaXYZ.GildedKeyStorage.Helpers;

[Injectable]
public class DebugHelper(
    ItemHelper itemHelper,
    DatabaseService databaseService,
    LocaleService localeService,
    ISptLogger<DebugHelper> logger)
{

    // These are keys that BSG added with no actual use, or drop chance. Ignore them for now
    // These should be confirmed every client update to still be unused
    private List<string> ignoredKeyList = [
        "5671446a4bdc2d97058b4569",
        "57518f7724597720a31c09ab",
        "57518fd424597720c85dbaaa",
        "5751916f24597720a27126df",
        "5751961824597720a31c09ac",
        "590de4a286f77423d9312a32",
        "590de52486f774226a0c24c2",
        "61a6446f4b5f8b70f451b166",
        "63a39ddda3a2b32b5f6e007a",
        "63a39e0f64283b5e9c56b282",
        "63a39e5b234195315d4020bf",
        "63a39e6acd6db0635c1975fe",
        "63a71f1a0aa9fb29da61c537",
        "63a71f3b0aa9fb29da61c539",
        "658199a0490414548c0fa83b",
        "6582dc63cafcd9485374dbc5"
    ];

    public void LogRareKeys()
    {
        logger.LogWithColor("[Gilded Key Storage]: Rare key list: ", LogTextColor.Cyan);
        logger.LogWithColor("-------------------------------------------", LogTextColor.Yellow);

        foreach (var (_, item) in databaseService.GetItems())
        {
            // Skip if key is in ignore list
            if (ignoredKeyList.Contains(item.Id)) continue;

            // Skip items that aren't items (Otherwise isOfBaseClass fails)
            if (item.Type != "Item") continue;

            // Skip non-keys
            if (!itemHelper.IsOfBaseclass(item.Id, BaseClasses.KEY)) continue;

            // Skip quest keys
            if (item.Properties!.QuestItem.GetValueOrDefault(false)) continue;

            // Skip secret exfil items
            if (item.Properties!.IsSecretExitRequirement.GetValueOrDefault(false)) continue;

            // Find keys with 10 or less uses
            if (item.Properties.MaximumNumberOfUsage <= 10)
            {
                logger.LogWithColor($"      \"{item.Id}\", // {localeService.GetLocaleDb()[$"{item.Id} Name"]}, Uses: {item.Properties.MaximumNumberOfUsage}", LogTextColor.Cyan);
            }
        }
    }

    public void LogMissingKeys(List<Case> cases)
    {
        HashSet<MongoId> keysInConfig = new HashSet<MongoId>();
        foreach (var newCase in cases)
        {
            if (newCase.SlotIds != null)
            {
                keysInConfig.UnionWith(newCase.SlotIds);
            }
        }

        logger.LogWithColor("[Gilded Key Storage]: Keys missing from config: ", LogTextColor.Magenta);
        logger.LogWithColor("-------------------------------------------", LogTextColor.Yellow);

        foreach (var (_, item) in databaseService.GetItems())
        {
            // Skip if key is in ignore list
            if (ignoredKeyList.Contains(item.Id)) continue;

            // Skip items that aren't items (Otherwise isOfBaseClass fails)
            if (item.Type != "Item") continue;

            // Skip non-keys
            if (!itemHelper.IsOfBaseclass(item.Id, BaseClasses.KEY)) continue;

            // Skip quest keys
            if (item.Properties!.QuestItem.GetValueOrDefault(false)) continue;

            // Skip secret exfil items
            if (item.Properties!.IsSecretExitRequirement.GetValueOrDefault(false))
            {
                if (keysInConfig.Contains(item.Id))
                {
                    logger.LogWithColor("Extract key in container", LogTextColor.Red);
                    logger.LogWithColor(localeService.GetLocaleDb()[$"{item.Id} Name"], LogTextColor.Red);
                    logger.LogWithColor(item.Id, LogTextColor.Red);
                    logger.LogWithColor("-------------------------------------------", LogTextColor.Yellow);
                }
                continue;
            }

            if (!keysInConfig.Contains(item.Id))
            {
                logger.LogWithColor(localeService.GetLocaleDb()[$"{item.Id} Name"], LogTextColor.Magenta);
                logger.LogWithColor(item.Id, LogTextColor.Magenta);
                logger.LogWithColor("-------------------------------------------", LogTextColor.Yellow);
            }
        }
    }
}