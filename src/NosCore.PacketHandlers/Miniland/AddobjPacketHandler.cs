//  __  _  __    __   ___ __  ___ ___
// |  \| |/__\ /' _/ / _//__\| _ \ __|
// | | ' | \/ |`._`.| \_| \/ | v / _|
// |_|\__|\__/ |___/ \__/\__/|_|_\___|
//

using NosCore.Data.Enumerations;
using NosCore.Data.Enumerations.Items;
using NosCore.GameObject.ComponentEntities.Entities;
using NosCore.GameObject.Infastructure;
using NosCore.GameObject.Networking.ClientSession;
using NosCore.GameObject.Services.MinilandService;
using NosCore.Packets.ClientPackets.Miniland;
using NosCore.Packets.Enumerations;
using NosCore.Packets.ServerPackets.Miniland;
using NosCore.Packets.ServerPackets.UI;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace NosCore.PacketHandlers.Miniland
{
    public class AddobjPacketHandler(IMinilandService minilandProvider) : PacketHandler<AddobjPacket>,
        IWorldPacketHandler
    {
        public override async Task ExecuteAsync(AddobjPacket addobjPacket, ClientSession clientSession)
        {
            var minilandobject =
                clientSession.Character.InventoryService.LoadBySlotAndType(addobjPacket.Slot, NoscorePocketType.Miniland);
            if (minilandobject == null)
            {
                return;
            }

            if (clientSession.Character.MapInstance.MapDesignObjects.ContainsKey(minilandobject.Id))
            {
                await clientSession.SendPacketAsync(new MsgiPacket
                {
                    Type = MessageType.Default,
                    Message = Game18NConstString.YouAlreadyHaveThisMinilandObject
                });
                return;
            }

            if (minilandProvider.GetMiniland(clientSession.Character.CharacterId).State != MinilandState.Lock)
            {
                await clientSession.SendPacketAsync(new MsgiPacket
                {
                    Type = MessageType.Default,
                    Message = Game18NConstString.InstallationOnlyLockMode
                });
                return;
            }

            var minilandobj = new MapDesignObject
            {
                MinilandObjectId = Guid.NewGuid(),
                MapX = addobjPacket.PositionX,
                MapY = addobjPacket.PositionY,
                Level1BoxAmount = 0,
                Level2BoxAmount = 0,
                Level3BoxAmount = 0,
                Level4BoxAmount = 0,
                Level5BoxAmount = 0
            };


            if (minilandobject.ItemInstance?.Item?.ItemType == ItemType.House)
            {
                var min = clientSession.Character.MapInstance.MapDesignObjects
                    .FirstOrDefault(s => (s.Value.InventoryItemInstance?.ItemInstance?.Item?.ItemType == ItemType.House) &&
                        (s.Value.InventoryItemInstance.ItemInstance.Item.ItemSubType ==
                            minilandobject.ItemInstance.Item.ItemSubType)).Value;
                if (min != null)
                {
                    await clientSession.HandlePacketsAsync(new[] { new RmvobjPacket { Slot = min.InventoryItemInstance?.Slot ?? 0 } });
                }
            }

            minilandProvider.AddMinilandObject(minilandobj, clientSession.Character.CharacterId, minilandobject);

            await clientSession.SendPacketAsync(minilandobj.GenerateEffect());
            await clientSession.SendPacketAsync(new MinilandPointPacket
            { MinilandPoint = minilandobject.ItemInstance?.Item?.MinilandObjectPoint ?? 0, Unknown = 100 });
            await clientSession.SendPacketAsync(minilandobj.GenerateMapDesignObject());
        }
    }
}
