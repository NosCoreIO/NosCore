using ChickenAPI.Packets.ClientPackets.UI;
using NosCore.GameObject;
using NosCore.GameObject.Networking.ClientSession;
using NosCore.GameObject.Providers.GuriProvider;

namespace NosCore.PacketHandlers.Game
{
    public class GuriPacketHandler : PacketHandler<GuriPacket>, IWorldPacketHandler
    {
        private readonly IGuriProvider _guriProvider;

        public GuriPacketHandler(IGuriProvider guriProvider)
        {
            _guriProvider = guriProvider;
        }

        public override void Execute(GuriPacket guriPacket, ClientSession session)
        {
            _guriProvider.GuriLaunch(session, guriPacket);
        }
    }
}