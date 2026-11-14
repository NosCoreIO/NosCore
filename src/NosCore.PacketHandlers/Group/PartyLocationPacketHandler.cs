//  __  _  __    __   ___ __  ___ ___
// |  \| |/__\ /' _/ / _//__\| _ \ __|
// | | ' | \/ |`._`.| \_| \/ | v / _|
// |_|\__|\__/ |___/ \__/\__/|_|_\___|
//

using NosCore.Data.Enumerations.Map;
using NosCore.GameObject.Ecs.Extensions;
using NosCore.GameObject.Infastructure;
using NosCore.GameObject.Networking.ClientSession;
using NosCore.GameObject.Services.BroadcastService;
using NosCore.Packets.ClientPackets.Commands;
using NosCore.Packets.Enumerations;
using NosCore.Shared.Enumerations;
using System.Linq;
using System.Threading.Tasks;

namespace NosCore.PacketHandlers.Group
{
    public class PartyLocationPacketHandler(ISessionRegistry sessionRegistry)
        : PacketHandler<PartyLocationPacket>, IWorldPacketHandler
    {
        private const byte OneString = 1;
        private const byte StringThenNumber = 3;

        public override async Task ExecuteAsync(PartyLocationPacket packet, ClientSession session)
        {
            var character = session.Character;
            var group = character.Group;
            if (group == null || group.Count <= 1)
            {
                await session.SendPacketAsync(character.GenerateSayi(SayColorType.Yellow, Game18NConstString.NotInGroup));
                return;
            }

            var members = group.MembersInJoiningOrder
                .Where(s => s.VisualType == VisualType.Player && s.VisualId != character.VisualId)
                .Where(s => string.IsNullOrEmpty(packet.CharacterName) || s.Name == packet.CharacterName);
            foreach (var member in members)
            {
                var name = member.Name ?? string.Empty;
                if (sessionRegistry.TryGetCharacter(s => s.VisualId == member.VisualId, out var target)
                    && target.MapInstance.MapInstanceType == MapInstanceType.BaseMapInstance)
                {
                    await session.SendPacketAsync(character.GenerateSayi2(SayColorType.Yellow, Game18NConstString.MapNo,
                        StringThenNumber, name, target.MapInstance.Map.MapId));
                    continue;
                }

                await session.SendPacketAsync(character.GenerateSayi2(SayColorType.Yellow,
                    Game18NConstString.GroupMemberInUnknownPlace, OneString, name));
            }
        }
    }
}
