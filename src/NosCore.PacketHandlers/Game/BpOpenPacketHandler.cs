using Microsoft.Extensions.Options;
using NodaTime;
using NosCore.Core.Configuration;
using NosCore.Data.StaticEntities;
using NosCore.GameObject;
using NosCore.GameObject.ComponentEntities.Extensions;
using NosCore.GameObject.Networking.ClientSession;
using NosCore.Packets.ClientPackets.Player;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace NosCore.PacketHandlers.Game
{
    public class BpOpenPacketHandler : PacketHandler<BpOpenPacket>, IWorldPacketHandler
    {
        private readonly IOptions<WorldConfiguration> _worldConfiguration;
        private readonly IClock _clock;
        private readonly List<QuestDto> _quests;

        public BpOpenPacketHandler(IOptions<WorldConfiguration> worldConfiguration, IClock clock, List<QuestDto> quests)
        {
            _worldConfiguration = worldConfiguration;
            _clock = clock;
            _quests = quests;
        }

        public override async Task ExecuteAsync(BpOpenPacket packet, ClientSession session)
        {
            await session.SendPacketAsync(session.Character.GenerateBpm(_clock, _worldConfiguration, _quests));
            await session.SendPacketAsync(session.Character.GenerateBpp());
            await session.SendPacketAsync(new BptPacket
            {
                MinutesUntilSeasonEnd = (long)Instant.Subtract(_worldConfiguration.Value.BattlepassConfiguration.EndSeason, _clock.GetCurrentInstant()).TotalMinutes
            });
            await session.SendPacketAsync(new BpoPacket());
        }
    }
}
