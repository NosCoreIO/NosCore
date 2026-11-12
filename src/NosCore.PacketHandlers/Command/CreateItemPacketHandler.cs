//  __  _  __    __   ___ __  ___ ___
// |  \| |/__\ /' _/ / _//__\| _ \ __|
// | | ' | \/ |`._`.| \_| \/ | v / _|
// |_|\__|\__/ |___/ \__/\__/|_|_\___|
//

using NosCore.Data.CommandPackets;
using NosCore.GameObject.Ecs.Extensions;
using NosCore.GameObject.Infastructure;
using NosCore.GameObject.Networking.ClientSession;
using NosCore.GameObject.Services.ItemGrantService;
using NosCore.Packets.Enumerations;
using NosCore.Packets.ServerPackets.Chats;
using NosCore.Packets.ServerPackets.UI;
using NosCore.Shared.Enumerations;
using System.Threading.Tasks;

namespace NosCore.PacketHandlers.Command
{
    public class CreateItemPackettHandler(IItemGrantService itemGrantService)
        : PacketHandler<CreateItemPacket>, IWorldPacketHandler
    {
        public override async Task ExecuteAsync(CreateItemPacket createItemPacket, ClientSession session)
        {
            switch (itemGrantService.Grant(session.Character, createItemPacket.VNum,
                createItemPacket.DesignOrAmount, createItemPacket.Upgrade))
            {
                case ItemGrantOutcome.UnknownItem:
                    await session.SendPacketAsync(new MsgiPacket
                    {
                        Type = MessageType.Default,
                        Message = Game18NConstString.ItemDoesNotExist
                    });
                    return;

                case ItemGrantOutcome.NotEnoughSpace:
                    await session.SendPacketAsync(new MsgiPacket
                    {
                        Type = MessageType.Default,
                        Message = Game18NConstString.NotEnoughSpace
                    });
                    return;

                case ItemGranted(var items, var amount):
                    await session.SendPacketAsync(items.GeneratePocketChange());
                    await session.SendPacketAsync(session.Character.GenerateSayi(SayColorType.Green, Game18NConstString.ReceivedThisItem,
                        2, createItemPacket.VNum.ToString(), amount));
                    return;
            }
        }
    }
}
