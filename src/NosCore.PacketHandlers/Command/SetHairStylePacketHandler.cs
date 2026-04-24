//  __  _  __    __   ___ __  ___ ___
// |  \| |/__\ /' _/ / _//__\| _ \ __|
// | | ' | \/ |`._`.| \_| \/ | v / _|
// |_|\__|\__/ |___/ \__/\__/|_|_\___|
//

using System;
using System.Threading.Tasks;
using NosCore.Data.CommandPackets;
using NosCore.GameObject.Ecs.Extensions;
using NosCore.GameObject.Infastructure;
using NosCore.GameObject.Networking;
using NosCore.GameObject.Networking.ClientSession;
using NosCore.Networking;
using NosCore.Packets.Enumerations;

namespace NosCore.PacketHandlers.Command
{
    public class SetHairStylePacketHandler : PacketHandler<SetHairStylePacket>, IWorldPacketHandler
    {
        public override async Task ExecuteAsync(SetHairStylePacket packet, ClientSession session)
        {
            if (!Enum.IsDefined(typeof(HairStyleType), packet.Style) ||
                packet.Style > (byte)HairStyleType.HairStyleB)
            {
                return;
            }

            var character = session.Character;
            character.HairStyle = (HairStyleType)packet.Style;

            await session.SendPacketAsync(character.GenerateEq());
            await character.MapInstance.SendPacketAsync(character.GenerateIn(string.Empty));
        }
    }
}
