using DrakiaXYZ.GildedKeyStorage.Helpers;
using DrakiaXYZ.GildedKeyStorage.Models;
using SPTarkov.DI.Annotations;
using SPTarkov.Server.Core.Helpers;
using SPTarkov.Server.Core.Helpers.Dialog.Commando.SptCommands;
using SPTarkov.Server.Core.Models.Common;
using SPTarkov.Server.Core.Models.Eft.Common.Tables;
using SPTarkov.Server.Core.Models.Eft.Dialog;
using SPTarkov.Server.Core.Models.Eft.Profile;
using SPTarkov.Server.Core.Services;
using SPTarkov.Server.Core.Utils;
using System.Reflection;

using Path = System.IO.Path;

namespace DrakiaXYZ.GildedKeyStorage;

[Injectable]
class GildedKeyStorageChatCommand(
    FileUtil fileUtil,
    CustomJsonUtil jsonUtil,
    MailSendService mailSendService,
    ItemHelper itemHelper) : ISptCommand
{

    public ValueTask<string> PerformAction(UserDialogInfo commandHandler, MongoId sessionId, SendMessageRequest request)
    {
        var modPath = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location)!;
        var casesConfigPath = Path.Join(modPath, "config", "cases.json");
        var cases = jsonUtil.Deserialize<List<Case>>(fileUtil.ReadFile(casesConfigPath))!;

        // Sort the items to add with the containers at the start
        List<MongoId> itemIdList = new List<MongoId>();
        foreach (var newCase in cases)
        {
            itemIdList.Insert(0, newCase.Id);
            if (newCase.SlotIds != null)
            {
                itemIdList.AddRange(newCase.SlotIds);
            }
        }
        
        // Generate the list of Item type items to send
        List<Item> itemsToSend = new List<Item>();
        foreach (var itemId in itemIdList)
        {
            var item = itemHelper.GetItem(itemId);
            itemsToSend.Add(
                new Item
                {
                    Id = new MongoId(),
                    Template = item.Value!.Id,
                    Upd = itemHelper.GenerateUpdForItem(item.Value),
                }
            );
        }

        itemHelper.SetFoundInRaid(itemsToSend);
        mailSendService.SendSystemMessageToPlayer(sessionId, $"Find attached all Gilded Key Storage and keys", itemsToSend);

        return ValueTask.FromResult(request.DialogId);
    }

    public string Command => "giveKeys";
    public string CommandHelp => "Usage: spt giveKeys";
}