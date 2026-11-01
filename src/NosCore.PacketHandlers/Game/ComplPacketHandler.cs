//  __  _  __    __   ___ __  ___ ___
// |  \| |/__\ /' _/ / _//__\| _ \ __|
// | | ' | \/ |`._`.| \_| \/ | v / _|
// |_|\__|\__/ |___/ \__/\__/|_|_\___|
//

using NodaTime;
using NosCore.GameObject.Infastructure;
using NosCore.GameObject.Networking.ClientSession;
using NosCore.GameObject.Services.BroadcastService;
using NosCore.Packets.ClientPackets.Player;
using NosCore.Packets.Enumerations;
using NosCore.Packets.ServerPackets.Chats;
using NosCore.Shared.Enumerations;
using System;
using System.Threading.Tasks;

namespace NosCore.PacketHandlers.Game
{
    public class ComplPacketHandler(IClock clock, ISessionRegistry sessionRegistry)
        : PacketHandler<ComplPacket>, IWorldPacketHandler
    {
        private const byte MinimumLevel = 30;
        private static readonly Duration DelayAfterConnection = Duration.FromMinutes(60);

        public override async Task ExecuteAsync(ComplPacket packet, ClientSession session)
        {
            var character = session.Character;
            if (character.Level < MinimumLevel)
            {
                await session.SendPacketAsync(new SayiPacket
                {
                    VisualType = VisualType.Player,
                    VisualId = character.CharacterId,
                    Type = SayColorType.Red,
                    Message = Game18NConstString.OnlyPlayersAboveLevelThirtyLead
                });
                return;
            }

            var now = clock.GetCurrentInstant();
            var availableAt = character.ConnectedAt + DelayAfterConnection;
            if (now < availableAt)
            {
                await session.SendPacketAsync(new SayiPacket
                {
                    VisualType = VisualType.Player,
                    VisualId = character.CharacterId,
                    Type = SayColorType.Red,
                    Message = Game18NConstString.CantCommendNow,
                    ArgumentType = 4,
                    Game18NArguments = { (long)Math.Ceiling((availableAt - now).TotalMinutes) }
                });
                return;
            }

            var account = character.Account;
            if (account.LastCompliment is { } last && last.InUtc().Date == now.InUtc().Date)
            {
                await session.SendPacketAsync(new SayiPacket
                {
                    VisualType = VisualType.Player,
                    VisualId = character.CharacterId,
                    Type = SayColorType.Red,
                    Message = Game18NConstString.CanOnlyLeadOncePerDayAndAccount
                });
                return;
            }

            if (packet.CharacterId == character.CharacterId
                || !sessionRegistry.TryGetCharacter(s => s.CharacterId == packet.CharacterId, out var target))
            {
                return;
            }

            target.Compliment = (short)(target.Compliment + 1);
            account.LastCompliment = now;

            await session.SendPacketAsync(new Sayi2Packet
            {
                VisualType = VisualType.Player,
                VisualId = character.CharacterId,
                Type = SayColorType.Green,
                Message = Game18NConstString.HaveCommendedPercentZero,
                ArgumentType = 1,
                Game18NArguments = { target.Name }
            });
            await target.SendPacketAsync(new Sayi2Packet
            {
                VisualType = VisualType.Player,
                VisualId = target.CharacterId,
                Type = SayColorType.Green,
                Message = Game18NConstString.HaveBeenCommendedByPercentZero,
                ArgumentType = 1,
                Game18NArguments = { character.Name }
            });
        }
    }
}
