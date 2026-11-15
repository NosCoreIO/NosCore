//  __  _  __    __   ___ __  ___ ___
// |  \| |/__\ /' _/ / _//__\| _ \ __|
// | | ' | \/ |`._`.| \_| \/ | v / _|
// |_|\__|\__/ |___/ \__/\__/|_|_\___|
//

using System.Linq;
using System.Threading.Tasks;
using JetBrains.Annotations;
using NosCore.Data.Enumerations.Group;
using NosCore.Data.Enumerations.Items;
using NosCore.GameObject.Ecs.Extensions;
using NosCore.GameObject.Messaging.Events;
using NosCore.GameObject.Services.BattleService;
using NosCore.GameObject.Services.BroadcastService;
using NosCore.GameObject.Services.InventoryService;
using NosCore.GameObject.Services.MapChangeService;
using NosCore.GameObject.Services.ScriptedInstanceService;
using NosCore.Packets.Enumerations;
using NosCore.Shared.Enumerations;

namespace NosCore.GameObject.Messaging.Handlers.UseItem
{
    [UsedImplicitly]
    public sealed class TeamStoneHandler(ISessionRegistry sessionRegistry, IMapChangeService mapChangeService,
        IScriptedInstanceService scriptedInstanceService, IRandomProvider random)
    {
        private const int CellAttempts = 100;

        [UsedImplicitly]
        public async Task Handle(ItemUsedEvent evt)
        {
            var character = evt.ClientSession.Character;
            var team = character.Group;
            var mapInstance = character.MapInstance;
            if (evt.InventoryItem.ItemInstance.Item.Effect != ItemEffectType.TeamStone
                || team is not { Type: not GroupType.Group, RaidRun: null }
                || team.Leader?.VisualId != character.CharacterId
                || !scriptedInstanceService.GenerateRaidGates(mapInstance.Map.MapId).Any())
            {
                return;
            }

            await character.UseUpOneAsync(evt.InventoryItem);
            var members = team.MembersInJoiningOrder
                .Where(m => m.VisualType == VisualType.Player)
                .Select(m => sessionRegistry.GetSessionByCharacterId(m.VisualId))
                .OfType<Networking.ClientSession.ClientSession>()
                .Where(s => s.HasPlayerEntity)
                .ToList();
            foreach (var member in members)
            {
                if (mapInstance.Map.RandomWalkableCell(random, CellAttempts) is not { } cell)
                {
                    continue;
                }

                await mapChangeService.ChangeMapInstanceAsync(member, mapInstance.MapInstanceId, cell.X, cell.Y);
            }
        }
    }
}
