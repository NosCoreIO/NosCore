//  __  _  __    __   ___ __  ___ ___
// |  \| |/__\ /' _/ / _//__\| _ \ __|
// | | ' | \/ |`._`.| \_| \/ | v / _|
// |_|\__|\__/ |___/ \__/\__/|_|_\___|
//

using NosCore.GameObject.InterChannelCommunication.Messages;
using NosCore.GameObject.Services.BroadcastService;
using NosCore.Packets.Enumerations;
using NosCore.Packets.Interfaces;
using NosCore.Packets.ServerPackets.Chats;
using NosCore.Shared.Enumerations;
using System.Linq;
using System.Threading.Tasks;

namespace NosCore.GameObject.Services.ChannelCommunicationService.Handlers
{
    public class FamilyChatDataMessageChannelCommunicationMessageHandler(ISessionRegistry sessionRegistry)
        : ChannelCommunicationMessageHandler<FamilyChatData>
    {
        public override Task Handle(FamilyChatData data)
        {
            return Task.WhenAll(sessionRegistry
                .GetSessions(s => s.HasSelectedCharacter && s.Character.Family?.FamilyId == data.FamilyId)
                .Select(session => session.SendPacketsAsync(new IPacket[]
                {
                    new SayPacket
                    {
                        VisualType = VisualType.Player,
                        VisualId = data.SenderId,
                        Type = SayColorType.Blue,
                        // The client names a sender it can see; anyone else needs the name in the line.
                        Message = session.Character.MapInstanceId == data.SenderMapInstanceId
                            ? data.Message
                            : $"[{data.SenderName}]:{data.Message}"
                    },
                    new SpeakPacket
                    {
                        VisualType = VisualType.Player,
                        VisualId = data.SenderId,
                        SpeakType = SpeakType.Family,
                        EntityName = data.SenderName,
                        Message = data.Message
                    }
                })));
        }
    }
}
