//  __  _  __    __   ___ __  ___ ___
// |  \| |/__\ /' _/ / _//__\| _ \ __|
// | | ' | \/ |`._`.| \_| \/ | v / _|
// |_|\__|\__/ |___/ \__/\__/|_|_\___|
//

using System.Threading.Tasks;
using NosCore.GameObject.Networking.ClientSession;
using NosCore.Networking;
using NosCore.Networking.SessionGroup.ChannelMatcher;
using NosCore.Packets.Enumerations;
using NosCore.Packets.ServerPackets.Player;

namespace NosCore.GameObject.Ecs.Extensions;

public static class GenderExtensions
{
    public static async Task ChangeGenderAsync(this ClientSession session)
    {
        var character = session.Character;
        character.Gender = character.Gender == GenderType.Female ? GenderType.Male : GenderType.Female;

        await session.SendPacketAsync(character.GenerateEq());
        await session.SendPacketAsync(new PSexPacket { Gender = character.Gender });
        await character.MapInstance.SendPacketAsync(character.GenerateIn(string.Empty), new EveryoneBut(session.ConnectedChannel.Id));
        await character.MapInstance.SendPacketAsync(character.GenerateCMode());
        await character.MapInstance.SendPacketAsync(character.GenerateEff(196));
    }
}
