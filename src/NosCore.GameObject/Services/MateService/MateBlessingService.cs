//  __  _  __    __   ___ __  ___ ___
// |  \| |/__\ /' _/ / _//__\| _ \ __|
// | | ' | \/ |`._`.| \_| \/ | v / _|
// |_|\__|\__/ |___/ \__/\__/|_|_\___|
//

using System.Linq;
using System.Threading.Tasks;
using NosCore.GameObject.Ecs;
using NosCore.GameObject.Infastructure;
using NosCore.GameObject.Services.BattleService;
using NosCore.Networking;
using NosCore.Packets.ServerPackets.Map;
using NosCore.Shared.Enumerations;

namespace NosCore.GameObject.Services.MateService
{
    public sealed class MateBlessingService(IBuffService buffService, ICardCatalog cardCatalog)
        : IMateBlessingService, ISingletonService
    {
        private const int BlessingEffect = 4766;

        public async Task BlessAsync(PlayerComponentBundle owner)
        {
            var active = buffService.GetActiveBuffs(owner).Select(b => b.CardId).ToHashSet();
            foreach (var mate in owner.Mates.Values.Where(m => m.IsTeamMember && m.Entity != null
                && m.NpcMonster.OwnerBlessingCardId != 0 && !active.Contains(m.NpcMonster.OwnerBlessingCardId)))
            {
                await GrantAsync(owner, mate);
            }
        }

        public async Task RenewAsync(PlayerComponentBundle owner, short cardId)
        {
            if (owner.Mates.Values.FirstOrDefault(m => m.IsTeamMember && m.Entity != null
                && m.NpcMonster.OwnerBlessingCardId == cardId) is { } mate)
            {
                await GrantAsync(owner, mate);
            }
        }

        private async Task GrantAsync(PlayerComponentBundle owner, Mate mate)
        {
            var cardId = mate.NpcMonster.OwnerBlessingCardId;
            if (cardCatalog.GetCard(cardId) is not { } card || mate.Entity is not { } entity)
            {
                return;
            }

            await buffService.ApplyAsync(owner, card, cardCatalog.GetCardBCards(cardId), entity);
            await owner.MapInstance.SendPacketAsync(new EffTPacket
            {
                VisualType = VisualType.Npc,
                VisualId = entity.VisualId,
                TargetVisualType = VisualType.Player,
                TargetVisualId = owner.VisualId,
                EffectId = BlessingEffect
            });
        }
    }
}
