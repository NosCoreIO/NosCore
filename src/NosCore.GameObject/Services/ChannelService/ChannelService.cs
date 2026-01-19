//  __  _  __    __   ___ __  ___ ___
// |  \| |/__\ /' _/ / _//__\| _ \ __|
// | | ' | \/ |`._`.| \_| \/ | v / _|
// |_|\__|\__/ |___/ \__/\__/|_|_\___|
//

using NosCore.GameObject.InterChannelCommunication.Hubs.AuthHub;
using NosCore.GameObject.InterChannelCommunication.Hubs.ChannelHub;
using NosCore.GameObject.Services.SaveService;
using NosCore.Packets.ServerPackets.Login;
using NosCore.Shared.Enumerations;
using System.Linq;
using System.Threading.Tasks;

namespace NosCore.GameObject.Services.ChannelService
{
    public class ChannelService(IAuthHub authHttpClient,
            IChannelHub channelHttpClient, ISaveService saveService)
        : IChannelService
    {
        public async Task MoveChannelAsync(Networking.ClientSession.ClientSession clientSession, int channelId)
        {
            var servers = await channelHttpClient.GetCommunicationChannels();
            var server = servers.FirstOrDefault(x => x.Id == channelId);
            if (server == null || server.Type != ServerType.WorldServer)
            {
                return;
            }
            await clientSession.SendPacketAsync(new MzPacket(server.DisplayHost ?? server.Host)
            {
                Port = server.DisplayPort ?? server.Port,
                CharacterSlot = clientSession.Character.Slot
            });

            await clientSession.SendPacketAsync(new ItPacket
            {
                Mode = 1
            });

            await authHttpClient.SetAwaitingConnectionAsync(-1, clientSession.Account.Name);
            await saveService.SaveAsync(clientSession.Character);
            await clientSession.DisconnectAsync();
        }

    }
}
