//  __  _  __    __   ___ __  ___ ___
// |  \| |/__\ /' _/ / _//__\| _ \ __|
// | | ' | \/ |`._`.| \_| \/ | v / _|
// |_|\__|\__/ |___/ \__/\__/|_|_\___|
//

using NosCore.Core;
using NosCore.GameObject.Networking.ClientSession;
using NosCore.GameObject.Services.MapInstanceGenerationService;
using NosCore.Packets.Enumerations;
using System.Threading.Tasks;

namespace NosCore.GameObject.Services.ChannelService
{
    public interface IChannelService
    {
        Task MoveChannelAsync(ClientSession clientSession, int channelId);

        // The channel to send a player to for a map this channel does not host: Glacernon belongs to the
        // Act 4 channel and every other map to the ordinary ones. Null when this channel keeps the player,
        // including when the other kind of channel is not running.
        Task<ChannelInfo?> GetChannelHostingAsync(bool act4Map);

        // Sends the player to the channel hosting that base map; false when this channel hosts it.
        Task<bool> TryMoveToHostAsync(ClientSession clientSession, MapInstance map, short mapX, short mapY);

        // The player reconnects to that channel already standing on the map.
        Task MoveToMapAsync(ClientSession clientSession, ChannelInfo server, ItModeType mode, short mapId, short mapX, short mapY);
    }
}
