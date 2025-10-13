using DrakiaXYZ.GildedKeyStorage.Helpers;
using DrakiaXYZ.GildedKeyStorage.Models;
using SPTarkov.DI.Annotations;
using SPTarkov.Server.Core.DI;
using SPTarkov.Server.Core.Helpers;
using SPTarkov.Server.Core.Models.Common;
using SPTarkov.Server.Core.Models.Eft.Common.Tables;
using SPTarkov.Server.Core.Models.Enums;
using SPTarkov.Server.Core.Models.Spt.Mod;
using SPTarkov.Server.Core.Models.Utils;
using SPTarkov.Server.Core.Services;
using SPTarkov.Server.Core.Services.Mod;
using SPTarkov.Server.Core.Utils;
using System.Reflection;
using System.Text.Json;
using Path = System.IO.Path;

namespace DrakiaXYZ.GildedKeyStorage;

[Injectable(TypePriority = OnLoadOrder.PostDBModLoader + 1)]
public class GildedKeyStorageMod(
    CustomJsonUtil jsonUtil,
    FileUtil fileUtil,
    ItemHelper itemHelper,
    CustomItemService customItemService,
    DatabaseService databaseService,
    DebugHelper debugHelper,
    Config config) : IOnLoad
{
    private readonly string modPath = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location)!;
    private readonly string HANDBOOK_GEARCASES = "5b5f6fa186f77409407a7eb7";

    public async Task OnLoad()
    {
        var casesConfigPath = Path.Join(modPath, "config", "cases.json");
        var cases = jsonUtil.Deserialize<List<Case>>(await fileUtil.ReadFileAsync(casesConfigPath))!;

        foreach (var newCase in cases)
        {
            CreateCase(newCase);
        }

        CreateBarters();
        AdjustItemProperties();
        SetLabsCardInRaidLimit(9);

        if (config.GetConfig().Debug!.LogMissingKeys)
        {
            debugHelper.LogMissingKeys(cases);
        }

        if (config.GetConfig().Debug!.LogRareKeys)
        {
            debugHelper.LogRareKeys();
        }
    }

    private void CreateCase(Case newCase)
    {
        string itemToCloneTpl;
        string parentItemId;
        if (newCase.CaseType == "container")
        {
            itemToCloneTpl = ItemTpl.CONTAINER_SICC;
            parentItemId = BaseClasses.SIMPLE_CONTAINER;
        }
        else if (newCase.CaseType == "slots")
        {
            itemToCloneTpl = ItemTpl.MOUNT_STRIKE_INDUSTRIES_KEYMOD_4_INCH_RAIL;
            parentItemId = BaseClasses.MOUNT;
        }
        else
        {
            throw new Exception($"Unknown case type {newCase.CaseType}");
        }

        var cloneDetails = new NewItemFromCloneDetails
        {
            ItemTplToClone = itemToCloneTpl,
            ParentId = parentItemId,
            NewId = newCase.Id,
            HandbookParentId = HANDBOOK_GEARCASES,
            FleaPriceRoubles = newCase.FleaPrice,
            HandbookPriceRoubles = newCase.FleaPrice,
            Locales = new Dictionary<string, LocaleDetails>
            {
                {
                    "en", new LocaleDetails
                    {
                        Name = newCase.ItemName,
                        ShortName = newCase.ItemShortName,
                        Description = newCase.ItemDescription,
                    }
                }
            },
            OverrideProperties = new TemplateItemProperties
            {
                Name = newCase.Name,
                IsAlwaysAvailableForInsurance = true,
                InsuranceDisabled = !config.GetConfig().CasesInsuranceEnabled,
                CanSellOnRagfair = !config.GetConfig().CasesFleaBanned,
                DiscardLimit = -1,
                Prefab = new Prefab
                {
                    Path = newCase.Bundle,
                    Rcid = "",
                },
                Width = newCase.ExternalSize.Width,
                Height = newCase.ExternalSize.Height,
            }
        };

        if (newCase.Sound is not null)
        {
            cloneDetails.OverrideProperties.ItemSound = newCase.Sound;
        }

        if (newCase.CaseType == "container")
        {
            cloneDetails.OverrideProperties.Grids = CreateGrids(newCase);
        }
        else if (newCase.CaseType == "slots")
        {
            cloneDetails.OverrideProperties.Slots = CreateSlots(newCase);
        }

        customItemService.CreateItemFromClone(cloneDetails);
        AllowCaseInContainers(newCase.Id);
        AddBarters(newCase);
    }

    private IEnumerable<Grid> CreateGrids(Case newCase)
    {
        List<Grid> grids = new List<Grid>();

        for (int i = 0; i < newCase.Grids!.Count; i++)
        {
            var gridConfig = newCase.Grids[i];
            gridConfig.IncludedFilter ??= [];
            gridConfig.ExcludedFilter ??= [];
            if (gridConfig.IncludedFilter.Count == 0)
            {
                gridConfig.IncludedFilter.Add(BaseClasses.ITEM);
            }

            grids.Add(new Grid
            {
                Id = new MongoId().ToString(),
                Name = $"column{i}",
                Parent = newCase.Id,
                Properties = new GridProperties
                {
                    CellsH = gridConfig.Width,
                    CellsV = gridConfig.Height,
                    MinCount = 0,
                    MaxCount = 0,
                    MaxWeight = 0,
                    IsSortingTable = false,
                    Filters =
                    [
                        new GridFilter()
                        {
                            Filter = gridConfig.IncludedFilter,
                            ExcludedFilter = gridConfig.ExcludedFilter,
                        }
                    ]
                }
            });

        }

        return grids;
    }

    private IEnumerable<Slot> CreateSlots(Case newCase)
    {
        List<Slot> slots = new List<Slot>();

        for (int i = 0; i < newCase.SlotIds!.Count; i++)
        {
            slots.Add(new Slot
            {
                Id = new MongoId().ToString(),
                Name = $"mod_mount_{i}",
                Parent = newCase.Id,
                MergeSlotWithChildren = false,
                Required = false,
                Prototype = "55d30c4c4bdc2db4468b457e",
                Properties = new SlotProperties
                {
                    Filters =
                    [
                        new SlotFilter
                        {
                            Filter = [newCase.SlotIds[i]]
                        }
                    ]
                }
            });
        }

        return slots;
    }

    private void AllowCaseInContainers(string itemId)
    {
        var itemTable = databaseService.GetTables().Templates.Items;
        foreach (var (_, item) in itemTable)
        {
            // Skip non-items (Root nodes, etc)
            if (item.Type != "Item") continue;

            // Backpacks are handled via a blacklist, so add to blacklist if backpacks are disbaled
            if (!config.GetConfig().AllowCasesInBackpacks && item.Parent == BaseClasses.BACKPACK)
            {
                DisallowInContainer(itemId, item);
            }

            // Secure containers are handled by whitelist, so add to whitelist if secure is enabled
            if (config.GetConfig().AllowCasesInSecure && item.Parent == BaseClasses.MOB_CONTAINER && item.Id != ItemTpl.SECURE_CONTAINER_BOSS)
            {
                AllowInContainer(itemId, item);
            }

            // Disallow in specific containers as defined
            if (config.GetConfig().CasesDisallowedIn.Contains(item.Id))
            {
                DisallowInContainer(itemId, item);
            }

            // Allow in specific containers as defined
            if (config.GetConfig().CasesAllowedIn.Contains(item.Id))
            {
                AllowInContainer(itemId, item);
            }
        }

        if (config.GetConfig().AllowCasesInSpecial)
        {
            AllowInSpecialSlots(itemId, itemTable[ItemTpl.POCKETS_1X4_SPECIAL]);
            AllowInSpecialSlots(itemId, itemTable[ItemTpl.POCKETS_1X4_TUE]);
        }
    }

    private void AllowInContainer(string itemId, TemplateItem container)
    {
        foreach (var grid in container.Properties!.Grids!)
        {
            if (grid.Properties!.Filters == null || grid.Properties.Filters.Count() == 0)
            {
                grid.Properties!.Filters = [new GridFilter() {
                    Filter = [],
                    ExcludedFilter = []
                }];
            }
            grid.Properties.Filters.First().Filter ??= [];
            grid.Properties.Filters.First().Filter!.Add(itemId);
        }
    }

    private void DisallowInContainer(string itemId, TemplateItem container)
    {
        foreach (var grid in container.Properties!.Grids!)
        {
            if (grid.Properties!.Filters == null || grid.Properties.Filters.Count() == 0)
            {
                grid.Properties!.Filters = [new GridFilter() {
                    Filter = [],
                    ExcludedFilter = []
                }];
            }
            grid.Properties.Filters.First().ExcludedFilter ??= [];
            grid.Properties.Filters.First().ExcludedFilter!.Add(itemId);
        }
    }

    private void AllowInSpecialSlots(string itemId, TemplateItem pocketItem)
    {
        foreach (var slot in pocketItem.Properties!.Slots!)
        {
            slot.Properties!.Filters!.First().Filter!.Add(itemId);
        }
    }

    private void AddBarters(BarterBase newCase)
    {
        // We use reflection to get the trader ID cause I'm lazy and like plaintext trader names
        FieldInfo? traderField = typeof(Traders).GetField(newCase.Trader.ToUpper());
        if (traderField == null) return;

        var traderId = (MongoId)traderField.GetValue(null)!;
        var trader = databaseService.GetTrader(traderId)!;
        trader.Assort.Items.Add(new Item
        {
            Id = newCase.Id,
            Template = newCase.Id,
            ParentId = "hideout",
            SlotId = "hideout",
            Upd = new Upd
            {
                UnlimitedCount = newCase.UnlimitedStock,
                StackObjectsCount = newCase.StockAmount,
            }
        });

        trader.Assort.BarterScheme.Add(newCase.Id, [newCase.Barter]);
        trader.Assort.LoyalLevelItems.Add(newCase.Id, newCase.TraderLoyaltyLevel);
    }

    private void CreateBarters()
    {
        var barterConfigPath = Path.Join(modPath, "config", "barters.json");
        var barters = jsonUtil.Deserialize<List<BarterBase>>(fileUtil.ReadFile(barterConfigPath))!;

        foreach (var barter in barters)
        {
            AddBarters(barter);
        }
    }

    private void AdjustItemProperties()
    {
        var itemTable = databaseService.GetTables().Templates.Items;
        foreach (var (_, item) in itemTable)
        {
            if (item.Type != "Item") continue;

            var itemProperties = item.Properties!;

            // Adjust key specific properties
            if (itemHelper.IsOfBaseclass(item.Id, BaseClasses.KEY))
            {
                if (config.GetConfig().WeightlessKeys)
                {
                    itemProperties.Weight = 0;
                }

                itemProperties.InsuranceDisabled = !config.GetConfig().KeyInsuranceEnabled;

                // If keys are to be set to no limit, and we're either not using the finite keys list, or this key doesn't exist
                // in it, set the key max usage to 0 (infinite)
                if (config.GetConfig().NoKeyUseLimit && 
                    (!config.GetConfig().UseFiniteKeysList || !config.GetConfig().FiniteKeysList.Contains(item.Id)))
                {
                    itemProperties.MaximumNumberOfUsage = 0;
                }

                if (config.GetConfig().KeysAreDiscardable)
                {
                    // BSG uses DiscordLimit == 0 to flag as not insurable, so we need to swap to the flag
                    if (itemProperties.DiscardLimit == 0)
                    {
                        itemProperties.InsuranceDisabled = true;
                    }
                    itemProperties.DiscardLimit = -1;
                }
            }

            // Adjust secure container properties
            if (config.GetConfig().AllKeysInSecure && itemHelper.IsOfBaseclass(item.Id, BaseClasses.MOB_CONTAINER) && itemProperties.Grids != null)
            {
                // Theta container has multiple grids, so we need to loop through all grids
                foreach (var grid in itemProperties.Grids)
                {
                    var filters = grid.Properties?.Filters;
                    if (filters?.Count() > 0)
                    {
                        filters.First().ExcludedFilter?.RemoveWhere(x => databaseService.GetItems()[x].Type == "Item" && itemHelper.IsOfBaseclass(x, BaseClasses.KEY));
                    }
                }
            }
        }
    }

    private void SetLabsCardInRaidLimit(int limitAmount)
    {
        var restrictionInRaid = databaseService.GetTables().Globals.Configuration.RestrictionsInRaid;
        var labsAccessRestriction = restrictionInRaid.FirstOrDefault(x => x.TemplateId == ItemTpl.KEYCARD_TERRAGROUP_LABS_ACCESS);
        if (labsAccessRestriction != null)
        {
            labsAccessRestriction.MaxInLobby = limitAmount;
            labsAccessRestriction.MaxInRaid = limitAmount;
        }
    }
}
