//  __  _  __    __   ___ __  ___ ___
// |  \| |/__\ /' _/ / _//__\| _ \ __|
// | | ' | \/ |`._`.| \_| \/ | v / _|
// |_|\__|\__/ |___/ \__/\__/|_|_\___|
//

using Microsoft.Extensions.Options;
using NosCore.Core;
using NosCore.Core.Configuration;
using NosCore.GameObject.InterChannelCommunication.Hubs.AuthHub;
using NosCore.GameObject.InterChannelCommunication.Hubs.ChannelHub;
using NosCore.Data.Enumerations.Map;
using NosCore.GameObject.Networking.ClientSession;
using NosCore.GameObject.Services.MapInstanceGenerationService;
using NosCore.GameObject.Services.SaveService;
using NosCore.Packets.Enumerations;
using NosCore.Packets.ServerPackets.Login;
using NosCore.Shared.Enumerations;
using System.Linq;
using System.Threading.Tasks;

namespace NosCore.GameObject.Services.ChannelService
{
    public class ChannelService(IAuthHub authHttpClient, IChannelHub channelHttpClient, ISaveService saveService,
            IOptions<WorldConfiguration> worldConfiguration)
        : IChannelService
    {
        public async Task MoveChannelAsync(ClientSession clientSession, int channelId)
        {
            var servers = await channelHttpClient.GetCommunicationChannels();
            var server = servers.FirstOrDefault(x => x.Id == channelId);
            if (server == null || server.Type != ServerType.WorldServer)
            {
                return;
            }

            await MoveAsync(clientSession, server, ItModeType.ShipToAct4);
        }

        public async Task<ChannelInfo?> GetChannelHostingAsync(bool act4Map)
        {
            if (act4Map == worldConfiguration.Value.Act4Channel)
            {
                return null;
            }

            var worlds = (await channelHttpClient.GetCommunicationChannels())
                .Where(c => c.Type == ServerType.WorldServer && !c.IsMaintenance && c.ServerId == worldConfiguration.Value.ServerId);
            return act4Map
                ? worlds.FirstOrDefault(c => c.IsAct4)
                : worlds.Where(c => !c.IsAct4).OrderBy(c => c.Id).FirstOrDefault();
        }

        public async Task<bool> TryMoveToHostAsync(ClientSession clientSession, MapInstance map, short mapX, short mapY)
        {
            if (map.MapInstanceType != MapInstanceType.BaseMapInstance
                || await GetChannelHostingAsync(map.IsAct4) is not { } channel)
            {
                return false;
            }

            return await MoveToMapAsync(clientSession, channel, map.IsAct4 ? ItModeType.ToAct4 : ItModeType.ToPortAlveus,
                map.Map.MapId, mapX, mapY);
        }

        public async Task<bool> MoveToMapAsync(ClientSession clientSession, ChannelInfo server, ItModeType mode,
            short mapId, short mapX, short mapY)
        {
            var character = clientSession.Character;
            var (previousMapId, previousMapX, previousMapY) = (character.MapId, character.MapX, character.MapY);
            character.MapId = mapId;
            character.MapX = mapX;
            character.MapY = mapY;
            if (await MoveAsync(clientSession, server, mode))
            {
                return true;
            }

            character.MapId = previousMapId;
            character.MapX = previousMapX;
            character.MapY = previousMapY;
            return false;
        }

        // The destination loads the player from the database, so without a stored save the player
        // would arrive as they were before and stays on this channel instead.
        private async Task<bool> MoveAsync(ClientSession clientSession, ChannelInfo server, ItModeType mode)
        {
            if (!await saveService.SaveAsync(clientSession))
            {
                return false;
            }

            await clientSession.SendPacketAsync(new MzPacket(server.DisplayHost ?? server.Host)
            {
                Port = server.DisplayPort ?? server.Port,
                CharacterSlot = clientSession.Character.Slot
            });

            await clientSession.SendPacketAsync(new ItPacket
            {
                Mode = mode
            });

            await authHttpClient.SetAwaitingChannelChangeAsync(clientSession.Account.Name, server.Id, clientSession.MfaValidated);
            await clientSession.DisconnectAsync();
            return true;
        }
    }
}
