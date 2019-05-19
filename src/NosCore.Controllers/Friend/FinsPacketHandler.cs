using System.Collections.Generic;
using System.Linq;
using ChickenAPI.Packets.ClientPackets.Relations;
using ChickenAPI.Packets.Enumerations;
using ChickenAPI.Packets.ServerPackets.UI;
using NosCore.Configuration;
using NosCore.Core;
using NosCore.Core.I18N;
using NosCore.Core.Networking;
using NosCore.Data.Enumerations;
using NosCore.Data.Enumerations.I18N;
using NosCore.Data.WebApi;
using NosCore.GameObject;
using NosCore.GameObject.Networking;
using NosCore.GameObject.Networking.ClientSession;
using Serilog;

namespace NosCore.PacketHandlers.Friend
{
    public class FinsPacketHandler : PacketHandler<FinsPacket>, IWorldPacketHandler
    {
        private readonly ILogger _logger;
        private readonly WorldConfiguration _worldConfiguration;

        public FinsPacketHandler(WorldConfiguration worldConfiguration, ILogger logger)
        {
            _worldConfiguration = worldConfiguration;
            _logger = logger;
        }
        public override void Execute(FinsPacket finsPacket, ClientSession session)
        {
            var server = WebApiAccess.Instance.Get<List<ChannelInfo>>(WebApiRoute.Channel)
                ?.FirstOrDefault(c => c.Type == ServerType.FriendServer);
            if (server != null)
            {
                WebApiAccess.Instance.Post<FriendShipRequest>(WebApiRoute.Friend, new FriendShipRequest { CharacterId = session.Character.CharacterId, FinsPacket = finsPacket }, server.WebApi);
            }
        }
    }
}
