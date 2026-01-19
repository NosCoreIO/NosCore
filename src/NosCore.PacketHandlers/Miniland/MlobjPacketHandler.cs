//  __  _  __    __   ___ __  ___ ___
// |  \| |/__\ /' _/ / _//__\| _ \ __|
// | | ' | \/ |`._`.| \_| \/ | v / _|
// |_|\__|\__/ |___/ \__/\__/|_|_\___|
//

using NosCore.GameObject.Infastructure;
using NosCore.GameObject.Networking.ClientSession;
using NosCore.GameObject.Services.MinilandService;
using NosCore.Packets.ClientPackets.Miniland;
using NosCore.Packets.Enumerations;
using NosCore.Packets.ServerPackets.Miniland;
using NosCore.Packets.ServerPackets.UI;
using System.Threading.Tasks;

namespace NosCore.PacketHandlers.Miniland
{
    public class MlEditPacketHandler(IMinilandService minilandProvider) : PacketHandler<MLEditPacket>,
        IWorldPacketHandler
    {
        public override async Task ExecuteAsync(MLEditPacket mlEditPacket, ClientSession clientSession)
        {
            var miniland = minilandProvider.GetMiniland(clientSession.Character.CharacterId);
            switch (mlEditPacket.Type)
            {
                case 1:
                    await clientSession.SendPacketAsync(new MlintroPacket { Intro = mlEditPacket.MinilandInfo!.Replace(' ', '^') });
                    miniland.MinilandMessage = mlEditPacket.MinilandInfo;
                    await clientSession.SendPacketAsync(new InfoiPacket
                    {
                        Message = Game18NConstString.MinilandChanged
                    });
                    break;

                case 2:
                    switch (mlEditPacket.Parameter)
                    {
                        case MinilandState.Private:
                            await clientSession.SendPacketAsync(new MsgiPacket
                            {
                                Type = MessageType.Default,
                                Message = Game18NConstString.MinilandPrivate
                            });
                            await minilandProvider.SetStateAsync(clientSession.Character.CharacterId, MinilandState.Private);
                            break;

                        case MinilandState.Lock:
                            await clientSession.SendPacketAsync(new MsgiPacket
                            {
                                Type = MessageType.Default,
                                Message = Game18NConstString.MinilandLocked
                            });
                            await minilandProvider.SetStateAsync(clientSession.Character.CharacterId, MinilandState.Lock);
                            break;

                        case MinilandState.Open:
                            await clientSession.SendPacketAsync(new MsgiPacket
                            {
                                Type = MessageType.Default,
                                Message = Game18NConstString.MinilandPublic
                            });
                            await minilandProvider.SetStateAsync(clientSession.Character.CharacterId, MinilandState.Open);
                            break;

                        default:
                            return;
                    }

                    break;
            }
        }
    }
}
