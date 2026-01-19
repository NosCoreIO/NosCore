//  __  _  __    __   ___ __  ___ ___
// |  \| |/__\ /' _/ / _//__\| _ \ __|
// | | ' | \/ |`._`.| \_| \/ | v / _|
// |_|\__|\__/ |___/ \__/\__/|_|_\___|
//

using NodaTime;
using NosCore.Data.Enumerations.Buff;
using NosCore.GameObject.ComponentEntities.Entities;
using NosCore.GameObject.ComponentEntities.Interfaces;
using NosCore.GameObject.Networking.ClientSession;
using NosCore.Packets.ClientPackets.Npcs;
using NosCore.Packets.Enumerations;
using NosCore.Packets.ServerPackets.UI;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace NosCore.GameObject.Services.NRunService.Handlers
{
    public class BazaarHandler(IClock clock) : INrunEventHandler
    {
        public bool Condition(Tuple<IAliveEntity, NrunPacket> item)
        {
            return (item.Item2.Runner == NrunRunnerType.OpenNosBazaar)
                && item.Item1 is MapNpc;
        }

        public Task ExecuteAsync(RequestData<Tuple<IAliveEntity, NrunPacket>> requestData)
        {
            var medalBonus = requestData.ClientSession.Character.StaticBonusList
                .FirstOrDefault(s =>
                    (s.StaticBonusType == StaticBonusType.BazaarMedalGold) ||
                    (s.StaticBonusType == StaticBonusType.BazaarMedalSilver));
            var medal = medalBonus != null ? medalBonus.StaticBonusType == StaticBonusType.BazaarMedalGold
                ? (byte)MedalType.Gold : (byte)MedalType.Silver : (byte)0;
            var time = medalBonus != null ? (int)(medalBonus.DateEnd == null ? 720 : (medalBonus.DateEnd?.Minus(clock.GetCurrentInstant()))?.ToTimeSpan().TotalHours ?? 0) : 0;
            return requestData.ClientSession.SendPacketAsync(new WopenPacket
            {
                Type = WindowType.NosBazaar,
                Unknown = medal,
                Unknown2 = (byte)time
            });
        }
    }
}
