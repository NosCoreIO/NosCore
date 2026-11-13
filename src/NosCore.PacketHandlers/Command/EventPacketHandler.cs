//  __  _  __    __   ___ __  ___ ___
// |  \| |/__\ /' _/ / _//__\| _ \ __|
// | | ' | \/ |`._`.| \_| \/ | v / _|
// |_|\__|\__/ |___/ \__/\__/|_|_\___|
//

using NosCore.Core.I18N;
using NosCore.Data.CommandPackets;
using NosCore.Data.Enumerations.I18N;
using NosCore.GameObject.Ecs.Extensions;
using NosCore.GameObject.Infastructure;
using NosCore.GameObject.Networking.ClientSession;
using NosCore.GameObject.Services.EventService;
using NosCore.Packets.Enumerations;
using System.Threading.Tasks;

namespace NosCore.PacketHandlers.Command
{
    public class EventPacketHandler(IEventService eventService, IGameLanguageLocalizer gameLanguageLocalizer)
        : PacketHandler<EventPacket>, IWorldPacketHandler
    {
        public override Task ExecuteAsync(EventPacket packet, ClientSession session)
        {
            return eventService.TryStart(packet.EventType, false)
                ? Task.CompletedTask
                : session.SendPacketAsync(session.Character.GenerateSay(
                    gameLanguageLocalizer[LanguageKey.EVENT_ALREADY_RUNNING, session.Account.Language], SayColorType.Yellow));
        }
    }
}
