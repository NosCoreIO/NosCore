//  __  _  __    __   ___ __  ___ ___
// |  \| |/__\ /' _/ / _//__\| _ \ __|
// | | ' | \/ |`._`.| \_| \/ | v / _|
// |_|\__|\__/ |___/ \__/\__/|_|_\___|
//

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
    public class ChangeGenderPacketHandler : PacketHandler<ChangeGenderPacket>, IWorldPacketHandler
    {
        public override async Task ExecuteAsync(ChangeGenderPacket _, ClientSession session)
        {
            var character = session.Character;
            character.Gender = character.Gender == GenderType.Female ? GenderType.Male : GenderType.Female;

            await session.SendPacketAsync(character.GenerateEq());
            await character.MapInstance.SendPacketAsync(character.GenerateIn(string.Empty));
            await character.MapInstance.SendPacketAsync(character.GenerateCMode());
            await character.MapInstance.SendPacketAsync(character.GenerateEff(196));
        }
    }
}
