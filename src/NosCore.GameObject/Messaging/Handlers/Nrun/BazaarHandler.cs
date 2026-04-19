//  __  _  __    __   ___ __  ___ ___
// |  \| |/__\ /' _/ / _//__\| _ \ __|
// | | ' | \/ |`._`.| \_| \/ | v / _|
// |_|\__|\__/ |___/ \__/\__/|_|_\___|
//

using System.Linq;
using System.Threading.Tasks;
using JetBrains.Annotations;
using NodaTime;
using NosCore.Data.Enumerations.Buff;
using NosCore.GameObject.Ecs;
using NosCore.GameObject.Messaging.Events;
using NosCore.Packets.Enumerations;
using NosCore.Packets.ServerPackets.UI;

namespace NosCore.GameObject.Messaging.Handlers.Nrun
{
    [UsedImplicitly]
    public sealed class BazaarHandler(IClock clock)
    {
        [UsedImplicitly]
        public Task Handle(NrunRequestedEvent evt)
        {
            if (evt.Packet.Runner != NrunRunnerType.OpenNosBazaar || evt.Target is not NpcComponentBundle)
            {
                return Task.CompletedTask;
            }

            var medalBonus = evt.ClientSession.Character.StaticBonusList.FirstOrDefault(s =>
                s.StaticBonusType == StaticBonusType.BazaarMedalGold ||
                s.StaticBonusType == StaticBonusType.BazaarMedalSilver);
            var medal = medalBonus != null
                ? medalBonus.StaticBonusType == StaticBonusType.BazaarMedalGold
                    ? (byte)MedalType.Gold
                    : (byte)MedalType.Silver
                : (byte)0;
            var time = medalBonus != null
                ? (int)(medalBonus.DateEnd == null
                    ? 720
                    : medalBonus.DateEnd.Value.Minus(clock.GetCurrentInstant()).ToTimeSpan().TotalHours)
                : 0;
            return evt.ClientSession.SendPacketAsync(new WopenPacket
            {
                Type = WindowType.NosBazaar,
                Unknown = medal,
                Unknown2 = (byte)time
            });
        }
    }
}
