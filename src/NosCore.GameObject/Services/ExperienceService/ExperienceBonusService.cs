//  __  _  __    __   ___ __  ___ ___
// |  \| |/__\ /' _/ / _//__\| _ \ __|
// | | ' | \/ |`._`.| \_| \/ | v / _|
// |_|\__|\__/ |___/ \__/\__/|_|_\___|
//

using System.Linq;
using NosCore.Data.Enumerations.Buff;
using NosCore.GameObject.Ecs;
using NosCore.GameObject.Ecs.Extensions;
using NosCore.GameObject.Services.BattleService;
using NosCore.GameObject.Services.EquipmentService;
using NosCore.GameObject.Services.ItemGenerationService;

namespace NosCore.GameObject.Services.ExperienceService
{
    public sealed class ExperienceBonusService(IBuffService buffService, IEquipmentStatsService equipmentStatsService,
        IItemCatalog itemCatalog, ICardCatalog cardCatalog) : IExperienceBonusService
    {
        public ExperienceBonus GetBonus(PlayerComponentBundle player)
        {
            int experience = 0, jobExperience = 0, heroExperience = 0, reputation = 0;
            var cards = buffService.GetActiveBuffs(player).SelectMany(b => b.BCards)
                .Concat(equipmentStatsService.Resolve(player).BCards ?? [])
                .Concat(player.MedalBCards(itemCatalog, cardCatalog));
            foreach (var bCard in cards)
            {
                var value = BattleStatsProvider.ScaleByLevel(bCard, player.Level);
                switch (bCard.Effect())
                {
                    case BCardEffect.ItemExpIncreased:
                    case BCardEffect.IncreaseXpGain:
                    case BCardEffect.NormalAndJobExpIncreased:
                        experience += value;
                        break;
                    case BCardEffect.ItemExpIncreasedNegated:
                    case BCardEffect.DecreaseXpGain:
                    case BCardEffect.NormalAndJobExpIncreasedNegated:
                        experience -= value;
                        break;
                    case BCardEffect.IncreaseJXpGain:
                        jobExperience += value;
                        break;
                    case BCardEffect.DecreaseJXpGain:
                        jobExperience -= value;
                        break;
                    case BCardEffect.IncreaseHeroXpReceived:
                        heroExperience += value;
                        break;
                    case BCardEffect.DecreaseHeroXpReceived:
                        heroExperience -= value;
                        break;
                    case BCardEffect.IncreaseFameReceived:
                        reputation += value;
                        break;
                    case BCardEffect.DecreaseFameReceived:
                        reputation -= value;
                        break;
                }
            }

            return new ExperienceBonus(experience, jobExperience, heroExperience, reputation);
        }
    }
}
