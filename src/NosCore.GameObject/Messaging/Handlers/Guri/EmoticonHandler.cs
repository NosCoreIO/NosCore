//  __  _  __    __   ___ __  ___ ___
// |  \| |/__\ /' _/ / _//__\| _ \ __|
// | | ' | \/ |`._`.| \_| \/ | v / _|
// |_|\__|\__/ |___/ \__/\__/|_|_\___|
//

using System;
using System.Threading.Tasks;
using JetBrains.Annotations;
using NosCore.GameObject.Ecs.Extensions;
using NosCore.GameObject.Messaging.Events;
using NosCore.GameObject.Networking;
using NosCore.Networking;
using NosCore.Packets.Enumerations;

namespace NosCore.GameObject.Messaging.Handlers.Guri
{
    [UsedImplicitly]
    public sealed class EmoticonHandler
    {
        [UsedImplicitly]
        public Task Handle(GuriPacketReceivedEvent evt)
        {
            var packet = evt.Packet;
            if (packet.Type != GuriPacketType.TextInput || packet.Data < 973 || packet.Data > 999)
            {
                return Task.CompletedTask;
            }

            var session = evt.ClientSession;
            if (session.Character.EmoticonsBlocked)
            {
                return Task.CompletedTask;
            }

            if (packet.VisualId.GetValueOrDefault() != session.Character.CharacterId)
            {
                return Task.CompletedTask;
            }

            return session.Character.MapInstance.SendPacketAsync(
                session.Character.GenerateEff(Convert.ToInt32(packet.Data) + 4099));
        }
    }
}
