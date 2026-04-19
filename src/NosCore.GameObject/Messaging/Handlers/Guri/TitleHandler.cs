//  __  _  __    __   ___ __  ___ ___
// |  \| |/__\ /' _/ / _//__\| _ \ __|
// | | ' | \/ |`._`.| \_| \/ | v / _|
// |_|\__|\__/ |___/ \__/\__/|_|_\___|
//

using System;
using System.Linq;
using System.Threading.Tasks;
using JetBrains.Annotations;
using NosCore.Data.Dto;
using NosCore.Data.Enumerations;
using NosCore.Data.Enumerations.Items;
using NosCore.GameObject.Ecs.Extensions;
using NosCore.GameObject.Messaging.Events;
using NosCore.Networking;
using NosCore.Packets.Enumerations;
using NosCore.Packets.ServerPackets.UI;

namespace NosCore.GameObject.Messaging.Handlers.Guri
{
    [UsedImplicitly]
    public sealed class TitleHandler
    {
        [UsedImplicitly]
        public async Task Handle(GuriPacketReceivedEvent evt)
        {
            if (evt.Packet.Type != GuriPacketType.Title)
            {
                return;
            }

            var session = evt.ClientSession;
            var inv = session.Character.InventoryService.LoadBySlotAndType(
                (short)(evt.Packet.VisualId ?? 0), NoscorePocketType.Main);
            if (inv?.ItemInstance?.Item?.ItemType != ItemType.Title ||
                session.Character.Titles.Any(s => s.TitleType == inv.ItemInstance?.ItemVNum))
            {
                return;
            }

            session.Character.Titles.Add(new TitleDto
            {
                Id = Guid.NewGuid(),
                TitleType = inv.ItemInstance.ItemVNum,
                Visible = false,
                Active = false,
                CharacterId = session.Character.VisualId
            });
            await session.Character.MapInstance.SendPacketAsync(session.Character.GenerateTitle());
            await session.SendPacketAsync(new InfoiPacket { Message = Game18NConstString.TitleChangedOrHidden });
            session.Character.InventoryService.RemoveItemAmountFromInventory(1, inv.ItemInstanceId);
            await session.SendPacketAsync(inv.GeneratePocketChange((PocketType)inv.Type, inv.Slot));
        }
    }
}
