//  __  _  __    __   ___ __  ___ ___
// |  \| |/__\ /' _/ / _//__\| _ \ __|
// | | ' | \/ |`._`.| \_| \/ | v / _|
// |_|\__|\__/ |___/ \__/\__/|_|_\___|
//

using NosCore.Data.Enumerations;
using NosCore.GameObject.Infastructure;
using NosCore.GameObject.Networking.ClientSession;
using NosCore.GameObject.Services.MinilandService;
using NosCore.Packets.ClientPackets.Miniland;
using NosCore.Packets.Enumerations;
using NosCore.Packets.ServerPackets.Miniland;
using NosCore.Packets.ServerPackets.UI;
using System.Threading.Tasks;

namespace NosCore.PacketHandlers.Miniland
{
    public class RmvobjPacketHandler(IMinilandService minilandProvider) : PacketHandler<RmvobjPacket>,
        IWorldPacketHandler
    {
        public override async Task ExecuteAsync(RmvobjPacket rmvobjPacket, ClientSession clientSession)
        {
            var minilandobject =
                clientSession.Character.InventoryService.LoadBySlotAndType(rmvobjPacket.Slot, NoscorePocketType.Miniland);
            if (minilandobject == null)
            {
                return;
            }

            if (minilandProvider.GetMiniland(clientSession.Character.CharacterId).State != MinilandState.Lock)
            {
                await clientSession.SendPacketAsync(new MsgiPacket
                {
                    Type = MessageType.Default,
                    Message = Game18NConstString.RemoveOnlyLockMode
                });
                return;
            }

            if (!clientSession.Character.MapInstance.MapDesignObjects.ContainsKey(minilandobject.Id))
            {
                return;
            }

            var minilandObject = clientSession.Character.MapInstance.MapDesignObjects[minilandobject.Id];
            clientSession.Character.MapInstance.MapDesignObjects.TryRemove(minilandobject.Id, out _);
            await clientSession.SendPacketAsync(minilandObject.GenerateEffect(true));
            await clientSession.SendPacketAsync(new MinilandPointPacket
            { MinilandPoint = minilandobject.ItemInstance.Item.MinilandObjectPoint, Unknown = 100 });
            await clientSession.SendPacketAsync(minilandObject.GenerateMapDesignObject(true));
        }
    }
}
