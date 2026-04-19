//  __  _  __    __   ___ __  ___ ___
// |  \| |/__\ /' _/ / _//__\| _ \ __|
// | | ' | \/ |`._`.| \_| \/ | v / _|
// |_|\__|\__/ |___/ \__/\__/|_|_\___|
//

<<<<<<< HEAD
using System.Linq;
=======
>>>>>>> 400adfdd (Swap recurring-jobs infrastructure from Rx-based Clock to Wolverine)
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
<<<<<<< HEAD
            foreach (var session in sessionRegistry.GetSessions().ToList())
            {
                var character = session.Character;
                var staticBonusList = character.StaticBonusList;
                if (staticBonusList.RemoveAll(s => s.DateEnd != null && s.DateEnd < now) > 0)
                {
                    await session.SendPacketAsync(new MsgiPacket
=======
            foreach (var character in sessionRegistry.GetCharacters())
            {
                if (character.StaticBonusList.RemoveAll(s => s.DateEnd != null && s.DateEnd < now) > 0)
                {
                    await character.SendPacketAsync(new MsgiPacket
>>>>>>> 400adfdd (Swap recurring-jobs infrastructure from Rx-based Clock to Wolverine)
                    {
                        Type = MessageType.Default,
                        Message = Game18NConstString.MagicItemExpired
                    });
                }
            }
        }
    }
}
