//  __  _  __    __   ___ __  ___ ___
// |  \| |/__\ /' _/ / _//__\| _ \ __|
// | | ' | \/ |`._`.| \_| \/ | v / _|
// |_|\__|\__/ |___/ \__/\__/|_|_\___|
//

using NodaTime;
using NosCore.GameObject.Infastructure;
using NosCore.GameObject.Networking.ClientSession;
using NosCore.GameObject.Services.MapInstanceGenerationService;
using NosCore.Packets.Enumerations;
using NosCore.Packets.ServerPackets.Chats;
using NosCore.Packets.ServerPackets.Specialists;
using NosCore.Shared.Enumerations;
using System.Collections.Concurrent;
using System.Threading.Tasks;

namespace NosCore.GameObject.Services.TransformationService
{
    public interface ISpCooldownNotificationService
    {
        void Schedule(ClientSession session, Instant end);

        Task TickAsync(MapInstance mapInstance);
    }

    // The cooldown itself is gated by LastSp arithmetic; this only delivers the
    // "side effect gone" packets when the window elapses. Swept by the owning map's
    // life loop instead of a detached timer so a disconnected session is dropped
    // rather than raced.
    public sealed class SpCooldownNotificationService(IClock clock) : ISpCooldownNotificationService, ISingletonService
    {
        private readonly ConcurrentDictionary<long, (ClientSession Session, Instant End)> _pending = new();

        public void Schedule(ClientSession session, Instant end)
        {
            _pending[session.Character.CharacterId] = (session, end);
        }

        public async Task TickAsync(MapInstance mapInstance)
        {
            if (_pending.IsEmpty)
            {
                return;
            }

            var now = clock.GetCurrentInstant();
            foreach (var (characterId, entry) in _pending)
            {
                if (!entry.Session.HasPlayerEntity)
                {
                    _pending.TryRemove(characterId, out _);
                    continue;
                }

                if (entry.Session.Character.MapInstance.MapInstanceId != mapInstance.MapInstanceId)
                {
                    continue;
                }

                if (entry.End > now)
                {
                    continue;
                }

                if (!_pending.TryRemove(characterId, out _))
                {
                    continue;
                }

                await entry.Session.SendPacketAsync(new SayiPacket
                {
                    VisualType = VisualType.Player,
                    VisualId = characterId,
                    Type = SayColorType.Red,
                    Message = Game18NConstString.TransformationSideEffectGone
                });
                await entry.Session.SendPacketAsync(new SdPacket { Cooldown = 0 });
            }
        }
    }
}
