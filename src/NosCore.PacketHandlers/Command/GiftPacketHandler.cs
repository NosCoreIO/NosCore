//  __  _  __    __   ___ __  ___ ___
// |  \| |/__\ /' _/ / _//__\| _ \ __|
// | | ' | \/ |`._`.| \_| \/ | v / _|
// |_|\__|\__/ |___/ \__/\__/|_|_\___|
//

using NosCore.GameObject.Ecs.Extensions;
using NosCore.Data.CommandPackets;
using NosCore.GameObject.Infastructure;
using NosCore.GameObject.Networking.ClientSession;
using NosCore.GameObject.Services.RemoteCharacterService;
using NosCore.Packets.Enumerations;
using NosCore.Packets.ServerPackets.Chats;
using NosCore.Packets.ServerPackets.UI;
using NosCore.Shared.Enumerations;
using System.Threading.Tasks;

namespace NosCore.PacketHandlers.Command
{
    public class GiftPacketHandler(IRemoteCharacterService remoteCharacterService)
        : PacketHandler<GiftPacket>, IWorldPacketHandler
    {
        public override async Task ExecuteAsync(GiftPacket giftPacket, ClientSession session)
        {
            if (!await remoteCharacterService.SendGiftAsync(session.Character, giftPacket.CharacterName, giftPacket.VNum,
                    giftPacket.Amount, giftPacket.Rare, giftPacket.Upgrade))
            {
                await session.SendPacketAsync(new InfoiPacket
                {
                    Message = Game18NConstString.UnknownCharacter
                });
                return;
            }

            await session.SendPacketAsync(session.Character.GenerateSayi(SayColorType.Red, Game18NConstString.GiftDelivered));
        }
    }
}
