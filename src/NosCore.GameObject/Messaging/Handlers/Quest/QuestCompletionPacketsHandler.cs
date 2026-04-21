//  __  _  __    __   ___ __  ___ ___
// |  \| |/__\ /' _/ / _//__\| _ \ __|
// | | ' | \/ |`._`.| \_| \/ | v / _|
// |_|\__|\__/ |___/ \__/\__/|_|_\___|
//

using System.Threading.Tasks;
using JetBrains.Annotations;
using NosCore.Data.Enumerations.I18N;
using NosCore.GameObject.Ecs.Extensions;
using NosCore.GameObject.Messaging.Events;
using NosCore.Packets.Enumerations;
using NosCore.Packets.ServerPackets.UI;

namespace NosCore.GameObject.Messaging.Handlers.Quest
{
    [UsedImplicitly]
    public sealed class QuestCompletionPacketsHandler
    {
        [UsedImplicitly]
        public async Task Handle(QuestCompletedEvent evt)
        {
            await evt.Character.SendPacketAsync(new MsgiPacket
            {
                Type = MessageType.Default,
                Message = Game18NConstString.QuestComplete
            });
            await evt.Character.SendPacketAsync(evt.Quest.GenerateQstiPacket(false));
            await evt.Character.SendPacketAsync(evt.Character.GenerateQuestPacket());
        }
    }
}
