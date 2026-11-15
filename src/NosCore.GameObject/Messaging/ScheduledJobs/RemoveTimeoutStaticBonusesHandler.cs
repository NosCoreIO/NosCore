//  __  _  __    __   ___ __  ___ ___
// |  \| |/__\ /' _/ / _//__\| _ \ __|
// | | ' | \/ |`._`.| \_| \/ | v / _|
// |_|\__|\__/ |___/ \__/\__/|_|_\___|
//

using System.Linq;
using System.Threading.Tasks;
using JetBrains.Annotations;
using Microsoft.Extensions.Options;
using NodaTime;
using NosCore.Core.Configuration;
using NosCore.Data.Enumerations.Buff;
using NosCore.GameObject.Ecs.Extensions;
using NosCore.GameObject.Services.BroadcastService;
using NosCore.Packets.Enumerations;
using NosCore.Packets.ServerPackets.UI;

namespace NosCore.GameObject.Messaging.ScheduledJobs
{
    [UsedImplicitly]
    public sealed class RemoveTimeoutStaticBonusesHandler(
        IClock clock,
        ISessionRegistry sessionRegistry,
        IOptions<WorldConfiguration> conf)
    {
        [UsedImplicitly]
        public async Task Handle(RemoveTimeoutStaticBonusesMessage _)
        {
            var now = clock.GetCurrentInstant();
            foreach (var session in sessionRegistry.GetSessions())
            {
                using (await session.AcquirePacketLockAsync())
                {
                    if (!session.HasPlayerEntity)
                    {
                        continue;
                    }

                    var character = session.Character;
                    var expired = character.StaticBonusList.Where(s => s.DateEnd != null && s.DateEnd < now).ToList();
                    if (expired.Count == 0)
                    {
                        continue;
                    }

                    character.StaticBonusList.RemoveAll(expired.Contains);
                    foreach (var bonus in expired)
                    {
                        await session.SendPacketAsync(new MsgiPacket
                        {
                            Type = MessageType.Default,
                            Message = bonus.StaticBonusType switch
                            {
                                StaticBonusType.MedalOfErenia => Game18NConstString.MedalEreniaExpired,
                                StaticBonusType.AdventurersMedal => Game18NConstString.AdventurerMedalExpired,
                                _ => Game18NConstString.MagicItemExpired,
                            }
                        });
                    }

                    character.LoadExpensions();
                    await session.SendPacketAsync(character.GenerateExts(conf));
                }
            }
        }
    }
}
