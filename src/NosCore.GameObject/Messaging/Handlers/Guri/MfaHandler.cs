//  __  _  __    __   ___ __  ___ ___
// |  \| |/__\ /' _/ / _//__\| _ \ __|
// | | ' | \/ |`._`.| \_| \/ | v / _|
// |_|\__|\__/ |___/ \__/\__/|_|_\___|
//

using System.Threading.Tasks;
using JetBrains.Annotations;
using NosCore.Data.CommandPackets;
using NosCore.GameObject.Messaging.Events;
using NosCore.Packets.Enumerations;
using NosCore.Packets.ServerPackets.UI;
using TwoFactorAuthNet;

namespace NosCore.GameObject.Messaging.Handlers.Guri
{
    [UsedImplicitly]
    public sealed class MfaHandler
    {
        [UsedImplicitly]
        public async Task Handle(GuriPacketReceivedEvent evt)
        {
            var packet = evt.Packet;
            if (packet.Type != GuriPacketType.TextInput || packet.Argument != 3 || packet.VisualId != 0)
            {
                return;
            }

            var session = evt.ClientSession;
            if (session.MfaValidated || session.Account.MfaSecret == null)
            {
                return;
            }

            var tfa = new TwoFactorAuth();
            if (tfa.VerifyCode(session.Account.MfaSecret, packet.Value))
            {
                session.MfaValidated = true;
                await session.HandlePacketsAsync(new[]
                {
                    new EntryPointPacket { Name = session.Account.Name }
                });
                return;
            }

            await session.SendPacketAsync(new NosCore.Packets.ServerPackets.UI.GuriPacket
            {
                Type = GuriPacketType.TextInput,
                Argument = 3,
                EntityId = 0
            });
            await session.SendPacketAsync(new InfoiPacket { Message = Game18NConstString.IncorrectPassword });
        }
    }
}
