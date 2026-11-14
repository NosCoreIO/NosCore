//  __  _  __    __   ___ __  ___ ___
// |  \| |/__\ /' _/ / _//__\| _ \ __|
// | | ' | \/ |`._`.| \_| \/ | v / _|
// |_|\__|\__/ |___/ \__/\__/|_|_\___|
//

using System.Threading.Tasks;
using NodaTime;
using NosCore.GameObject.Ecs;
using NosCore.GameObject.Ecs.Extensions;
using NosCore.GameObject.Map;
using NosCore.Packets.ClientPackets.Instance;
using NosCore.Packets.Enumerations;
using NosCore.Packets.ServerPackets.UI;
using NosCore.PathFinder.Interfaces;

namespace NosCore.GameObject.Services.ScriptedInstanceService
{
    public sealed class MapButtonService(IScriptedInstanceService scriptedInstanceService, IHeuristic distanceCalculator,
        IClock clock) : IMapButtonService
    {
        public const int PullDelay = 2000;

        public async Task<bool> PressAsync(PlayerComponentBundle character, long visualId)
        {
            if (visualId is < 1 or > MapButton.LastId
                || !character.MapInstance.Buttons.TryGetValue((int)visualId, out var button))
            {
                return false;
            }

            if ((button.IsOn && !button.Toggles) || !InReach(character, button))
            {
                return true;
            }

            await character.StartDelayAsync(new DelayPacket
            {
                Delay = PullDelay,
                Type = DelayPacketType.Button,
                Packet = new GitPacket { ButtonId = button.MapButtonId }
            }, button, clock.GetCurrentInstant());
            return true;
        }

        public Task UseAsync(PlayerComponentBundle character, int buttonId)
        {
            var mapInstance = character.MapInstance;
            if (!mapInstance.Buttons.TryGetValue(buttonId, out var button)
                || !InReach(character, button)
                || !character.TryCompleteDelay(DelayPacketType.Button, button, clock.GetCurrentInstant()))
            {
                return Task.CompletedTask;
            }

            return scriptedInstanceService.GetRun(mapInstance.MapInstanceId)?.UseButtonAsync(mapInstance.MapInstanceId, buttonId)
                ?? Task.CompletedTask;
        }

        private bool InReach(PlayerComponentBundle character, MapButton button)
        {
            return distanceCalculator.GetDistance((character.PositionX, character.PositionY),
                (button.PositionX, button.PositionY)) < 8;
        }
    }
}
