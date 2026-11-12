//  __  _  __    __   ___ __  ___ ___
// |  \| |/__\ /' _/ / _//__\| _ \ __|
// | | ' | \/ |`._`.| \_| \/ | v / _|
// |_|\__|\__/ |___/ \__/\__/|_|_\___|
//

using System.Threading.Tasks;
using JetBrains.Annotations;
using NosCore.Algorithm.ShellService;
using NosCore.Data.Enumerations.Items;
using NosCore.GameObject.Ecs.Extensions;
using NosCore.GameObject.Messaging.Events;
using NosCore.GameObject.Services.InventoryService;
using NosCore.GameObject.Services.ItemGenerationService;
using NosCore.GameObject.Services.ItemGenerationService.Item;
using NosCore.Packets.Enumerations;
using NosCore.Packets.ServerPackets.UI;

namespace NosCore.GameObject.Messaging.Handlers.Guri
{
    // The item put in the perfume window becomes the player's own, for the perfume and gold the window showed.
    [UsedImplicitly]
    public sealed class PerfumeHandler(IShellService shellService, IItemCatalog itemCatalog)
    {
        [UsedImplicitly]
        public async Task Handle(GuriPacketReceivedEvent evt)
        {
            var packet = evt.Packet;
            if (packet.Type != GuriPacketType.Perfume || packet.Argument is not { } pocket
                || packet.VisualId is not { } slot || slot is < short.MinValue or > short.MaxValue
                || itemCatalog.GetItemWithEffect(ItemEffectType.Perfume) is not { } perfume)
            {
                return;
            }

            var session = evt.ClientSession;
            var character = session.Character;
            var inventory = character.InventoryService;
            if (inventory.LoadBySlotAndType((short)slot, ((PocketType)pocket).ToNoscorePocketType())?.ItemInstance
                    is not WearableInstance equipment
                || equipment.BoundCharacterId is not { } owner || owner == character.CharacterId)
            {
                return;
            }

            var level = equipment.Item.LevelMinimum;
            var cost = shellService.GetPerfumeCost(level, unchecked((byte)equipment.Rare), equipment.Item.IsHeroic);
            var gold = shellService.GetPerfumeGoldCost(level);
            if (inventory.CountItem(perfume.VNum) < cost)
            {
                await SendAsync(evt, Game18NConstString.NotEnoughPerfume);
                return;
            }

            if (character.Gold < gold)
            {
                await SendAsync(evt, Game18NConstString.NotEnoughGold);
                return;
            }

            var packets = inventory.RemoveItemAmountByVNum(perfume.VNum, cost);
            character.RemoveGold(gold);
            equipment.BoundCharacterId = character.CharacterId;
            await session.SendPacketsAsync(packets);
            await session.SendPacketAsync(character.GenerateGold());
            await SendAsync(evt, Game18NConstString.EquipmentPerfumed);
        }

        private static Task SendAsync(GuriPacketReceivedEvent evt, Game18NConstString message) =>
            evt.ClientSession.SendPacketAsync(new MsgiPacket { Type = MessageType.Default, Message = message });
    }
}
