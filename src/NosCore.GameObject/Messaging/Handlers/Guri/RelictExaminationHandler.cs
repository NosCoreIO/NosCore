//  __  _  __    __   ___ __  ___ ___
// |  \| |/__\ /' _/ / _//__\| _ \ __|
// | | ' | \/ |`._`.| \_| \/ | v / _|
// |_|\__|\__/ |___/ \__/\__/|_|_\___|
//

using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using JetBrains.Annotations;
using NosCore.Dao.Interfaces;
using NosCore.Data.Dto;
using NosCore.Data.Enumerations;
using NosCore.Data.Enumerations.Interaction;
using NosCore.Data.Enumerations.Items;
using NosCore.GameObject.Ecs.Extensions;
using NosCore.GameObject.Messaging.Events;
using NosCore.GameObject.Services.BattleService;
using NosCore.GameObject.Services.InventoryService;
using NosCore.GameObject.Services.ItemGenerationService;
using NosCore.GameObject.Services.ItemGenerationService.Item;
using NosCore.GameObject.Services.NpcDialogService;
using NosCore.GameObject.Services.ShopService;
using NosCore.Packets.Enumerations;
using NosCore.Packets.ServerPackets.Chats;
using NosCore.Packets.ServerPackets.UI;
using NosCore.Shared.Enumerations;

namespace NosCore.GameObject.Messaging.Handlers.Guri
{
    // The window sends the fee it showed, which is how the relict is told apart: the client only
    // takes the relicts whose fee it knows.
    [UsedImplicitly]
    public sealed class RelictExaminationHandler(INpcDialogService npcDialogService, INpcServiceCatalog npcServices,
        IItemGenerationService itemProvider, IDao<RollGeneratedItemDto, short> rollGeneratedItemDao, IRandomProvider random)
    {
        [UsedImplicitly]
        public async Task Handle(GuriPacketReceivedEvent evt)
        {
            if (evt.Packet.Type != GuriPacketType.ActVRelic || evt.Packet.Argument is not { } fee)
            {
                return;
            }

            var session = evt.ClientSession;
            var character = session.Character;
            if (npcDialogService.FindOpenNpc(character, NpcDialogWindow.RelictExamination) is not { } npc
                || !npcServices.Offers(npc, NpcService.RelictExamination))
            {
                return;
            }

            var inventory = character.InventoryService;
            var relict = inventory.Values
                .Where(i => i.Type == NoscorePocketType.Main && i.ItemInstance.Item is { Effect: ItemEffectType.Relict } item
                    && item.EffectValue == fee)
                .OrderBy(i => i.Slot)
                .FirstOrDefault();
            if (relict == null)
            {
                return;
            }

            if (character.Level < relict.ItemInstance.Item.LevelMinimum)
            {
                await SayAsync(evt, Game18NConstString.TooLowLevel);
                return;
            }

            if (character.Gold < fee)
            {
                await session.SendPacketAsync(new InfoiPacket { Message = Game18NConstString.NotEnoughGold });
                return;
            }

            var rows = (rollGeneratedItemDao.Where(r => r.OriginalItemVNum == relict.ItemInstance.ItemVNum && r.Probability > 0)
                ?? Enumerable.Empty<RollGeneratedItemDto>()).OrderBy(r => r.RollGeneratedItemId).ToList();
            var sum = rows.Sum(r => (int)r.Probability);
            if (sum <= 0)
            {
                return;
            }

            var roll = random.Next(0, sum);
            var chosen = rows.First(r => (roll -= r.Probability) < 0);
            if (itemProvider.Create(chosen.ItemGeneratedVNum, chosen.ItemGeneratedAmount, 0, chosen.ItemGeneratedUpgrade) is not { } reward)
            {
                return;
            }

            if (!inventory.HasRoomFor(reward))
            {
                await SayAsync(evt, Game18NConstString.NotEnoughSpace);
                return;
            }

            var added = inventory.ConsumeAndAdd(relict, 1, [InventoryItemInstance.Create(reward, character.CharacterId)]);
            if (added == null)
            {
                return;
            }

            if (added.Count == 0)
            {
                await SayAsync(evt, Game18NConstString.NotEnoughSpace);
                return;
            }

            character.RemoveGold(fee);
            await session.SendPacketAsync(character.GenerateGold());
            await session.SendPacketAsync(inventory.LoadBySlotAndType(relict.Slot, relict.Type)
                .GeneratePocketChange(relict.Type.ToPocketType(), relict.Slot));
            foreach (var inv in added)
            {
                await session.SendPacketAsync(inv.GeneratePocketChange(inv.Type.ToPocketType(), inv.Slot));
            }

            await session.SendPacketAsync(new RdiPacket
            {
                ItemVNum = chosen.ItemGeneratedVNum,
                Amount = chosen.ItemGeneratedAmount,
            });
            await session.SendPacketAsync(character.GenerateSayi(SayColorType.Green, Game18NConstString.ReceivedThisItem,
                2, chosen.ItemGeneratedVNum.ToString(), chosen.ItemGeneratedAmount));
        }

        private static Task SayAsync(GuriPacketReceivedEvent evt, Game18NConstString message) =>
            evt.ClientSession.SendPacketAsync(new SayiPacket
            {
                VisualType = VisualType.Player,
                VisualId = evt.ClientSession.Character.CharacterId,
                Type = SayColorType.Yellow,
                Message = message,
            });
    }
}
