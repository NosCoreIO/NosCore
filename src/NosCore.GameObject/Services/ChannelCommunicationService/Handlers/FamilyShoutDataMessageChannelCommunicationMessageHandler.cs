//  __  _  __    __   ___ __  ___ ___
// |  \| |/__\ /' _/ / _//__\| _ \ __|
// | | ' | \/ |`._`.| \_| \/ | v / _|
// |_|\__|\__/ |___/ \__/\__/|_|_\___|
//

using NosCore.Core.I18N;
using NosCore.Data.Enumerations.I18N;
using NosCore.GameObject.InterChannelCommunication.Messages;
using NosCore.GameObject.Services.BroadcastService;
using NosCore.Packets.Enumerations;
using NosCore.Packets.ServerPackets.UI;
using System.Linq;
using System.Threading.Tasks;

namespace NosCore.GameObject.Services.ChannelCommunicationService.Handlers
{
    public class FamilyShoutDataMessageChannelCommunicationMessageHandler(ISessionRegistry sessionRegistry,
        IGameLanguageLocalizer gameLanguageLocalizer) : ChannelCommunicationMessageHandler<FamilyShoutData>
    {
        public override Task Handle(FamilyShoutData data)
        {
            return Task.WhenAll(sessionRegistry
                .GetSessions(s => s.HasSelectedCharacter && s.Character.Family?.FamilyId == data.FamilyId)
                .Select(session =>
                {
                    // The client has no message that carries free text, so msgi cannot replace msg here.
#pragma warning disable CS0618
                    return session.SendPacketAsync(new MsgPacket
                    {
                        Type = MessageType.Default,
                        Message = string.Format(gameLanguageLocalizer[LanguageKey.FAMILY_SHOUT, session.Account.Language], data.Message)
                    });
#pragma warning restore CS0618
                }));
        }
    }
}
