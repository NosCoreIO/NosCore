//  __  _  __    __   ___ __  ___ ___
// |  \| |/__\ /' _/ / _//__\| _ \ __|
// | | ' | \/ |`._`.| \_| \/ | v / _|
// |_|\__|\__/ |___/ \__/\__/|_|_\___|
//

using NosCore.GameObject.Messaging;
using NosCore.GameObject.Services.InventoryService;
using NosCore.Data.Enumerations;
using NosCore.Data.Enumerations.Buff;
using NosCore.GameObject.Infastructure;
using NosCore.GameObject.Messaging.Events;
using NosCore.GameObject.Networking.ClientSession;
using NosCore.GameObject.Services.BattleService;
using NosCore.Packets.ClientPackets.Inventory;
using System.Threading.Tasks;

namespace NosCore.PacketHandlers.Inventory
{
    public class UseItemPacketHandler(IGameEventDispatcher gameEvents, IBuffService buffService) : PacketHandler<UseItemPacket>, IWorldPacketHandler
    {
        public override Task ExecuteAsync(UseItemPacket useItemPacket, ClientSession clientSession)
        {
            var inv = clientSession.Character.InventoryService.LoadBySlotAndType(
                useItemPacket.Slot, useItemPacket.Type.ToNoscorePocketType());
            if (inv?.ItemInstance?.Item == null
                || buffService.HasEffect(clientSession.Character, BCardEffect.TimeCircleSkillsItemCannotBeUsed))
            {
                return Task.CompletedTask;
            }

            return gameEvents.RaiseAsync(new ItemUsedEvent(clientSession, inv, useItemPacket));
        }
    }
}
