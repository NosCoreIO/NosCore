//  __  _  __    __   ___ __  ___ ___
// |  \| |/__\ /' _/ / _//__\| _ \ __|
// | | ' | \/ |`._`.| \_| \/ | v / _|
// |_|\__|\__/ |___/ \__/\__/|_|_\___|
//
using NosCore.GameObject.InterChannelCommunication.Messages;
using NosCore.GameObject.Services.BroadcastService;
using NosCore.Packets.ServerPackets.Families;
using System.Linq;
using System.Threading.Tasks;

namespace NosCore.GameObject.Services.ChannelCommunicationService.Handlers
{
    public class FamilyHistoryChangedDataMessageChannelCommunicationMessageHandler(ISessionRegistry sessionRegistry)
        : ChannelCommunicationMessageHandler<FamilyHistoryChangedData>
    {
        public override Task Handle(FamilyHistoryChangedData data) =>
            Task.WhenAll(sessionRegistry
                .GetSessions(s => s.HasSelectedCharacter && s.Character.Family?.FamilyId == data.FamilyId)
                .Select(s => s.SendPacketAsync(new FhisStcPacket())));
    }
}
