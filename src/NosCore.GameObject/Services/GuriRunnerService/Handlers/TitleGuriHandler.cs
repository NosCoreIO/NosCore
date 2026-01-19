//  __  _  __    __   ___ __  ___ ___
// |  \| |/__\ /' _/ / _//__\| _ \ __|
// | | ' | \/ |`._`.| \_| \/ | v / _|
// |_|\__|\__/ |___/ \__/\__/|_|_\___|
//

using NosCore.Data.Dto;
using NosCore.Data.Enumerations;
using NosCore.Data.Enumerations.Items;
using NosCore.GameObject.ComponentEntities.Extensions;
using NosCore.GameObject.Networking.ClientSession;
using NosCore.Networking;
using NosCore.Packets.Enumerations;
using NosCore.Packets.ServerPackets.UI;
using System;
using System.Linq;
using System.Threading.Tasks;
using GuriPacket = NosCore.Packets.ClientPackets.UI.GuriPacket;

namespace NosCore.GameObject.Services.GuriRunnerService.Handlers
{
    public class TitleGuriHandler : IGuriEventHandler
    {
        public bool Condition(GuriPacket packet)
        {
            return (packet.Type == GuriPacketType.Title);
        }

        public async Task ExecuteAsync(RequestData<GuriPacket> requestData)
        {
            var inv = requestData.ClientSession.Character.InventoryService.LoadBySlotAndType((short)(requestData.Data.VisualId ?? 0),
                NoscorePocketType.Main);
            if (inv?.ItemInstance?.Item?.ItemType != ItemType.Title ||
                requestData.ClientSession.Character.Titles.Any(s => s.TitleType == inv.ItemInstance?.ItemVNum))
            {
                return;
            }

            requestData.ClientSession.Character.Titles.Add(new TitleDto
            {
                Id = Guid.NewGuid(),
                TitleType = inv.ItemInstance.ItemVNum,
                Visible = false,
                Active = false,
                CharacterId = requestData.ClientSession.Character.VisualId
            });
            await requestData.ClientSession.Character.MapInstance.SendPacketAsync(requestData.ClientSession.Character.GenerateTitle());
            await requestData.ClientSession.SendPacketAsync(new InfoiPacket { Message = Game18NConstString.TitleChangedOrHidden });
            requestData.ClientSession.Character.InventoryService.RemoveItemAmountFromInventory(1, inv.ItemInstanceId);
            await requestData.ClientSession.SendPacketAsync(inv.GeneratePocketChange((PocketType)inv.Type, inv.Slot));
        }
    }
}
