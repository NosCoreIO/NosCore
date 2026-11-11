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
            var result = itemGrantService.Grant(session.Character, createItemPacket.VNum,
                createItemPacket.DesignOrAmount, createItemPacket.Upgrade);
            switch (result.Outcome)
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

                case ItemGrantOutcome.Granted:
                    await session.SendPacketAsync((result.Items ?? []).GeneratePocketChange());
                    await session.SendPacketAsync(new SayiPacket
                    {
                        VisualType = VisualType.Player,
                        VisualId = session.Character.CharacterId,
                        Type = SayColorType.Green,
                        Message = Game18NConstString.ReceivedThisItem,
                        ArgumentType = 2,
                        Game18NArguments = { createItemPacket.VNum.ToString(), result.Amount }
                    });
                    return;
            }
        }
    }
}
