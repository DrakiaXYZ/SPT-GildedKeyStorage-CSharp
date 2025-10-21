using BepInEx;
using DrakiaXYZ.GildedKeyStorage.ClientMod.VersionChecker;
using EFT;
using EFT.InventoryLogic;
using EFT.UI.DragAndDrop;
using HarmonyLib;
using SPT.Reflection.Patching;
using System;
using System.Linq;
using System.Reflection;

namespace DrakiaXYZ.GildedKeyStorage.ClientMod
{
    [BepInPlugin("xyz.drakia.gildedkeystorage", "DrakiaXYZ-GildedKeyStorage", "2.0.2")]
    [BepInDependency("com.SPT.core", "4.0.0")]
    public class GildedKeyStoragePlugin : BaseUnityPlugin
    {
        public void Awake()
        {
            if (!TarkovVersion.CheckEftVersion(Logger, Info, Config))
            {
                throw new Exception($"Invalid EFT Version");
            }

            new RemoveSlotItemsForMapEntryPatch().Enable();
            new HideSpecialSlotGrids().Enable();
            new LoggerBonePatch().Enable();
        }
    }

    class RemoveSlotItemsForMapEntryPatch : ModulePatch
    {
        protected override MethodBase GetTargetMethod()
        {
            var desiredType = typeof(LocalGame).BaseType;
            var desiredMethod = AccessTools.FirstMethod(typeof(LocalGame).BaseType, method =>
            {
                var parms = method.GetParameters();
                return parms.Length == 2
                && parms[0].ParameterType == typeof(Profile)
                && parms[0].Name == "profile"
                && parms[1].ParameterType == typeof(string)
                && parms[1].Name == "keyId";
            });

            Logger.LogDebug($"{this.GetType().Name} Type: {desiredType?.Name}");
            Logger.LogDebug($"{this.GetType().Name} Method: {desiredMethod?.Name}");

            return desiredMethod;
        }

        [PatchPrefix]
        public static bool PatchPrefix(Profile profile, string keyId)
        {
            if (string.IsNullOrEmpty(keyId))
            {
                return false;
            }

            // If we are transiting, the key has already been consumed, don't do anything
            if (TransitControllerAbstractClass.IsTransit(profile.Id, out string transitLocation))
            {
                return false;
            }

            // Find the entry item in the user's profile
            Item itemToRemove = null;
            Item keyItem = profile.Inventory.GetPlayerItems(EPlayerItems.Equipment).FirstOrDefault(item => item.Id == keyId);
            if (keyItem != null)
            {
                KeyComponent keyComponent = keyItem.GetItemComponent<KeyComponent>();
                if (keyComponent != null)
                {
                    if (keyComponent.Template.MaximumNumberOfUsage > 0)
                    {
                        keyComponent.NumberOfUsages = keyComponent.NumberOfUsages + 1;
                        if (keyComponent.NumberOfUsages >= keyComponent.Template.MaximumNumberOfUsage)
                        {
                            itemToRemove = keyItem;
                        }
                    }
                }
                else
                {
                    itemToRemove = keyItem;
                }
            }

            // If we need to remove the item (Either not a Key, or has hit its use limit), remove it
            if (itemToRemove != null)
            {
                StashGridClass stashGrid = itemToRemove.Parent.Container as StashGridClass;
                if (stashGrid != null)
                {
                    var result = stashGrid.Remove(itemToRemove, false);
                    if (result.Failed)
                    {
                        Logger.LogError(result.Error);
                    }
                }
                else
                {
                    Slot slot = itemToRemove.Parent.Container as Slot;
                    if (slot != null)
                    {
                        var result = slot.RemoveItemWithoutRestrictions();
                        if (result.Failed)
                        {
                            Logger.LogError(result.Error);
                        }
                    }
                }
            }

            // Skip original
            return false;
        }
    }

    public class HideSpecialSlotGrids : ModulePatch
    {
        protected override MethodBase GetTargetMethod()
        {
            return AccessTools.DeclaredMethod(typeof(GeneratedGridsView), nameof(GeneratedGridsView.Show));
        }

        [PatchPrefix]
        private static bool PatchPrefix(GeneratedGridsView __instance, CompoundItem compoundItem)
        {
            // The item is in the special slot, and we're drawing the inventory UI based on the parent, skip showing the grids
            if (compoundItem.CurrentAddress.IsSpecialSlotAddress() && __instance.transform.parent.name.StartsWith("SpecialSlot"))
            {
                return false;
            }

            return true;
        }
    }

    public class LoggerBonePatch : ModulePatch
    {
        protected override MethodBase GetTargetMethod()
        {
            return AccessTools.DeclaredMethod(typeof(LoggerClass), nameof(LoggerClass.LogError));
        }

        [PatchPrefix]
        private static bool PatchPrefix(string format)
        {
            // If the log starts with "bone mod_mount_" and contains "item_container", skip it, this is a Gilded "error"
            if (format.StartsWith("bone mod_mount_") && format.Contains("item_container"))
            {
                return false;
            }

            return true;
        }
    }
}
