//  __  _  __    __   ___ __  ___ ___
// |  \| |/__\ /' _/ / _//__\| _ \ __|
// | | ' | \/ |`._`.| \_| \/ | v / _|
// |_|\__|\__/ |___/ \__/\__/|_|_\___|
//

using System;
using System.Linq;
using System.Threading.Tasks;
using NosCore.Algorithm.PartnerAgilityService;
using NosCore.Data.Enumerations.Character;
using NosCore.GameObject.Ecs;
using NosCore.GameObject.Ecs.Extensions;
using NosCore.GameObject.Services.BattleService;
using NosCore.Networking;
using NosCore.Packets.Enumerations;
using NosCore.Packets.ServerPackets.UI;

namespace NosCore.GameObject.Services.MateService
{
    public sealed class MateProgressionService(IMateStatsService mateStatsService,
        IPartnerSkillCatalog partnerSkillCatalog, IPartnerAgilityService partnerAgilityService,
        IBuffService buffService, ICardCatalog cardCatalog) : IMateProgressionService
    {
        public async Task AddAgilityAsync(PlayerComponentBundle owner, byte monsterLevel)
        {
            if (owner.Mates.Values.FirstOrDefault(s => s.MateType == MateType.Partner && s.IsTeamMember) is not { } partner
                || partner.Specialist(owner.InventoryService) is not { } card
                || card.Agility >= partnerAgilityService.MaxAgility)
            {
                return;
            }

            var gain = partnerAgilityService.GetAgilityGain(owner.Level, monsterLevel);
            if (gain == 0)
            {
                return;
            }

            card.Agility = (byte)Math.Min(card.Agility + gain, partnerAgilityService.MaxAgility);
            await owner.SendPacketAsync(partner.GenerateScn(owner.AccountLanguage, owner.InventoryService, partnerSkillCatalog));
            if (card.Agility == partnerAgilityService.MaxAgility)
            {
                await owner.SendPacketAsync(new MsgiPacket { Type = MessageType.Default, Message = Game18NConstString.PartnersAgilityMax });
            }
        }

        public async Task AddExperienceAsync(PlayerComponentBundle owner, long experience)
        {
            if (experience <= 0)
            {
                return;
            }

            var bonus = buffService.GetActiveBuffs(owner).Sum(b => cardCatalog.GetCard(b.CardId)?.MateExperienceBonus ?? 0);
            experience = experience * (100 + bonus) / 100;
            foreach (var mate in owner.Mates.Values.Where(s => s.IsTeamMember))
            {
                await AddExperienceAsync(owner, mate, experience);
            }
        }

        public async Task AddExperienceAsync(PlayerComponentBundle owner, Mate mate, long experience)
        {
            if (experience <= 0)
            {
                return;
            }

            var leveledUp = false;
            mateStatsService.ApplyLevel(mate);
            mate.Experience += experience;
            // A mate stays below its owner: the client refuses a partner of the same level.
            while (mate.XpLoad > 0 && mate.Experience >= mate.XpLoad && mate.Level + 1 < owner.Level)
            {
                mate.Experience -= mate.XpLoad;
                mate.Level++;
                mateStatsService.ApplyLevel(mate);
                leveledUp = true;
            }

            mate.Experience = Math.Min(mate.Experience, mate.XpLoad);

            if (leveledUp)
            {
                if (mate.RefillAfterLevelUp() is { } entity)
                {
                    await owner.MapInstance.SendPacketAsync(entity.GenerateEff(6));
                    await owner.MapInstance.SendPacketAsync(entity.GenerateEff(198));
                }

                await owner.SendPacketAsync(mate.GeneratePst());
            }

            await owner.SendPacketAsync(mate.MateType == MateType.Pet
                ? mate.GenerateScp(owner.AccountLanguage)
                : mate.GenerateScn(owner.AccountLanguage, owner.InventoryService, partnerSkillCatalog));
        }
    }
}
