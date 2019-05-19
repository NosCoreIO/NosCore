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
    public class BlInsPackettHandler : PacketHandler<BlInsPacket>, IWorldPacketHandler
    {

        public override void Execute(BlInsPacket blinsPacket, ClientSession session)
        {
            var server = WebApiAccess.Instance.Get<List<ChannelInfo>>(WebApiRoute.Channel)
                ?.FirstOrDefault(c => c.Type == ServerType.FriendServer);
            if (server != null)
            {
                WebApiAccess.Instance.Post<BlacklistRequest>(WebApiRoute.Friend, new BlacklistRequest { CharacterId = session.Character.CharacterId, BlInsPacket = blinsPacket }, server.WebApi);
            }
        }
    }
}
