//  __  _  __    __   ___ __  ___ ___
// |  \| |/__\ /' _/ / _//__\| _ \ __|
// | | ' | \/ |`._`.| \_| \/ | v / _|
// |_|\__|\__/ |___/ \__/\__/|_|_\___|
//

using NosCore.Data.CommandPackets;
using NosCore.Data.Enumerations;
using NosCore.GameObject.Ecs.Extensions;
using NosCore.GameObject.Infastructure;
using NosCore.GameObject.Networking.ClientSession;
using NosCore.GameObject.Services.RemoteCharacterService;
using NosCore.Packets.Enumerations;
using NosCore.Packets.ServerPackets.UI;
using System.Threading.Tasks;

namespace NosCore.PacketHandlers.Command
{
    public class SetReputationPacketHandler(IRemoteCharacterService remoteCharacterService)
        : PacketHandler<SetReputationPacket>, IWorldPacketHandler
    {
        public override async Task ExecuteAsync(SetReputationPacket setReputationPacket, ClientSession session)
        {
            if ((setReputationPacket.Name == session.Character.Name) || string.IsNullOrEmpty(setReputationPacket.Name))
            {
                await session.Character.SetReputationAsync(setReputationPacket.Reputation);
                return;
            }

            if (!await remoteCharacterService.UpdateStatAsync(setReputationPacket.Name, UpdateStatActionType.UpdateReputation, setReputationPacket.Reputation))
            {
                await session.SendPacketAsync(new InfoiPacket
                {
                    Message = Game18NConstString.UnknownCharacter
                });
            }
        }
    }
}
