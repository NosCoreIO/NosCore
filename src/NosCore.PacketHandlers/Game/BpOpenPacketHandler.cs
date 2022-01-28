using Microsoft.Extensions.Options;
using NodaTime;
using NosCore.Core.Configuration;
using NosCore.GameObject;
using NosCore.GameObject.ComponentEntities.Extensions;
using NosCore.GameObject.Networking.ClientSession;
using NosCore.Packets.ClientPackets.Player;
using System;
using System.Threading.Tasks;

namespace NosCore.PacketHandlers.Game
{
    public class BpOpenPacketHandler : PacketHandler<BpOpenPacket>, IWorldPacketHandler
    {
        private readonly IOptions<WorldConfiguration> _worldConfiguration;
        private readonly IClock _clock;

        public BpOpenPacketHandler(IOptions<WorldConfiguration> worldConfiguration, IClock clock)
        {
            _worldConfiguration = worldConfiguration;
            _clock = clock;
        }

        public override async Task ExecuteAsync(BpOpenPacket packet, ClientSession session)
        {
            await session.SendPacketAsync(session.Character.GenerateBpm(_clock, _worldConfiguration));
            await session.SendPacketAsync(session.Character.GenerateBpp());
            await session.SendPacketAsync(new BptPacket
            {
                MinutesUntilSeasonEnd = (long)Instant.Subtract(_worldConfiguration.Value.BattlepassConfiguration.EndSeason, _clock.GetCurrentInstant()).TotalMinutes
            });
            await session.SendPacketAsync(new BpoPacket());
        }
    }
}
