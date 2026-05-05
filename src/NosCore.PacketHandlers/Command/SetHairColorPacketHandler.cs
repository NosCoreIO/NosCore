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
    public class SetHairColorPacketHandler : PacketHandler<SetHairColorPacket>, IWorldPacketHandler
    {
        public override async Task ExecuteAsync(SetHairColorPacket packet, ClientSession session)
        {
            if (!Enum.IsDefined(typeof(HairColorType), packet.Color))
            {
                return;
            }

            var character = session.Character;
            character.HairColor = (HairColorType)packet.Color;

            await session.SendPacketAsync(character.GenerateEq());
            await character.MapInstance.SendPacketAsync(character.GenerateIn(string.Empty));
        }
    }
}
