//  __  _  __    __   ___ __  ___ ___
// |  \| |/__\ /' _/ / _//__\| _ \ __|
// | | ' | \/ |`._`.| \_| \/ | v / _|
// |_|\__|\__/ |___/ \__/\__/|_|_\___|
//

using System.Threading.Tasks;
using JetBrains.Annotations;
using NodaTime;
using NosCore.GameObject.Services.BroadcastService;
using NosCore.Packets.Enumerations;
using NosCore.Packets.ServerPackets.UI;

namespace NosCore.GameObject.Messaging.ScheduledJobs
{
    [UsedImplicitly]
    public sealed class RemoveTimeoutStaticBonusesHandler(
        IClock clock,
        ISessionRegistry sessionRegistry)
    {
        [UsedImplicitly]
        public async Task Handle(RemoveTimeoutStaticBonusesMessage _)
        {
            var now = clock.GetCurrentInstant();
            foreach (var character in sessionRegistry.GetCharacters())
            {
                if (character.StaticBonusList.RemoveAll(s => s.DateEnd != null && s.DateEnd < now) > 0)
                {
                    await character.SendPacketAsync(new MsgiPacket
                    {
                        Type = MessageType.Default,
                        Message = Game18NConstString.MagicItemExpired
                    });
                }
            }
        }
    }
}
