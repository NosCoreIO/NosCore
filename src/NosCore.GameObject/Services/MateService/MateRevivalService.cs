//  __  _  __    __   ___ __  ___ ___
// |  \| |/__\ /' _/ / _//__\| _ \ __|
// | | ' | \/ |`._`.| \_| \/ | v / _|
// |_|\__|\__/ |___/ \__/\__/|_|_\___|
//

using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using NodaTime;
using NosCore.Data.Enumerations.Items;
using NosCore.GameObject.Ecs;
using NosCore.GameObject.Ecs.Extensions;
using NosCore.GameObject.Infastructure;
using NosCore.GameObject.Services.BroadcastService;
using NosCore.GameObject.Services.InventoryService;
using NosCore.GameObject.Services.ItemGenerationService;
using NosCore.GameObject.Services.MapInstanceGenerationService;
using NosCore.Packets.Enumerations;
using NosCore.Packets.ServerPackets.UI;

namespace NosCore.GameObject.Services.MateService
{
    public sealed class MateRevivalService(
        IItemCatalog itemCatalog,
        ISessionRegistry sessionRegistry,
        IClock clock,
        IPartnerSpecialistService partnerSpecialistService,
        IPetSkillBarService petSkillBarService,
        ILogger<MateRevivalService> logger) : IMateRevivalService, ISingletonService
    {
        public const short SeedsRequired = 5;
        public static readonly Duration ReviveDelay = Duration.FromMinutes(3);

        public async Task KnockOutAsync(PlayerComponentBundle owner, Mate mate)
        {
            if (mate.Entity is { MapInstance: { } mapInstance })
            {
                await mate.DespawnAsync(mapInstance);
            }

            await partnerSpecialistService.UntransformAsync(owner, mate);
            await petSkillBarService.RefreshAsync(owner);

            var seedOfPower = itemCatalog.GetMaterialVNum(MaterialType.SeedOfPower);
            if (owner.InventoryService.CountItem(seedOfPower) < SeedsRequired)
            {
                mate.IsTeamMember = false;
                mate.Hp = mate.MaxHp;
                mate.Mp = mate.MaxMp;
                await owner.SendPacketAsync(Message(Game18NConstString.NosMateDeadNoMorePowerSeed));
                await owner.SendPacketsAsync(owner.GenerateMateRoster());
                await owner.SendPacketAsync(owner.GeneratePinit());
                return;
            }

            await owner.SendPacketsAsync(owner.InventoryService.RemoveItemAmountByVNum(seedOfPower, SeedsRequired));
            mate.Hp = 0;
            mate.RevivesAt = clock.GetCurrentInstant().Plus(ReviveDelay);
            await owner.SendPacketAsync(Message(Game18NConstString.FivePowerSeedUsed));
            await owner.SendPacketAsync(Message(Game18NConstString.NosmateDeadThreeMinutes));
            await owner.SendPacketAsync(mate.GeneratePst());
        }

        public async Task TickAsync(MapInstance mapInstance)
        {
            var now = clock.GetCurrentInstant();
            foreach (var session in sessionRegistry.GetClientSessionsByMapInstance(mapInstance.MapInstanceId))
            {
                if (!session.HasPlayerEntity)
                {
                    continue;
                }

                var owner = session.Character;
                foreach (var mate in owner.Mates.Values.Where(s => s.RevivesAt <= now))
                {
                    try
                    {
                        await ReviveAsync(owner, mate, mapInstance);
                    }
                    catch (Exception ex)
                    {
                        logger.LogWarning(ex, "Reviving mate {MateId} failed", mate.MateId);
                    }
                }
            }
        }

        private async Task ReviveAsync(PlayerComponentBundle owner, Mate mate, MapInstance mapInstance)
        {
            mate.RevivesAt = null;
            mate.Hp = mate.MaxHp;
            mate.Mp = mate.MaxMp;
            if (!mate.IsTeamMember || mate.Entity != null)
            {
                return;
            }

            var (_, x, y) = MatePlacement.Arrange(owner.PositionX, owner.PositionY, mapInstance.Map, new[] { mate })[0];
            await mate.SpawnAsync(owner, mapInstance, x, y);
            await owner.SendPacketAsync(mate.GenerateCond());
            await owner.SendPacketAsync(mate.GeneratePst());
            await petSkillBarService.RefreshAsync(owner);
        }

        private static MsgiPacket Message(Game18NConstString message) => new()
        {
            Type = MessageType.Default,
            Message = message
        };
    }
}
