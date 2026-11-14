//  __  _  __    __   ___ __  ___ ___
// |  \| |/__\ /' _/ / _//__\| _ \ __|
// | | ' | \/ |`._`.| \_| \/ | v / _|
// |_|\__|\__/ |___/ \__/\__/|_|_\___|
//

using Microsoft.Extensions.Options;
using NosCore.Core.Configuration;
using NosCore.GameObject.Ecs;
using NosCore.GameObject.Ecs.Extensions;
using NosCore.GameObject.Networking.ClientSession;
using NosCore.GameObject.Services.FamilyService;
using NosCore.GameObject.Services.InventoryService;
using NosCore.GameObject.Services.ItemGenerationService;
using NosCore.Instances.Abstractions;
using NosCore.Packets.Enumerations;
using NosCore.Packets.ServerPackets.Event;
using NosCore.Shared.Enumerations;
using NosCore.Shared.Helpers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace NosCore.GameObject.Services.ScriptedInstanceService
{
    public class TimeSpaceRewardService(IScriptedInstanceService scriptedInstanceService,
            IItemGenerationService itemGenerationService, IItemDeliveryService itemDeliveryService,
            IFamilyContributionService familyContributionService,
            IOptions<WorldConfiguration> worldConfiguration)
        : ITimeSpaceRewardService
    {
        public async Task<bool> OpenChestAsync(ClientSession session)
        {
            var character = session.Character;
            if (character.MapInstance is not { } map
                || scriptedInstanceService.GetRun(map.MapInstanceId) is not { Host: TimeSpaceRunHost host } run
                || host.Entrance.Definition is not { } definition
                || !run.TryOpenChest(character.CharacterId))
            {
                return false;
            }

            await GiveGoldAsync(character, definition.Gold);
            if (definition.Reputation > 0)
            {
                await character.SetReputationAsync(character.Reputation + definition.Reputation);
            }

            if (definition.FamilyExperience > 0 && character.Family is { } family)
            {
                await familyContributionService.ExperienceEarnedAsync(family.FamilyId, character.CharacterId,
                    character.Name, definition.FamilyExperience);
            }

            var drawn = definition.DrawItems.Count == 0
                ? null
                : definition.DrawItems[RandomHelper.Instance.RandomNumber(0, definition.DrawItems.Count)];
            foreach (var gift in definition.GiftItems.Concat(definition.SpecialItems).Append(drawn).OfType<InstanceGift>())
            {
                await itemDeliveryService.DeliverAsync(character, itemGenerationService.Create(gift.VNum, gift.Amount, 0, 0, gift.Design));
            }

            await session.SendPacketAsync(new RepayPacket
            {
                BonusRewards = Slots(definition.GiftItems, 3),
                SpecialRewards = Slots(definition.SpecialItems, 2),
                DrawnReward = Slot(drawn)
            });
            return true;
        }

        private async Task GiveGoldAsync(PlayerComponentBundle character, long amount)
        {
            var gold = Math.Min(amount, Math.Max(0, worldConfiguration.Value.MaxGoldAmount - character.Gold));
            if (gold <= 0)
            {
                return;
            }

            character.Gold += gold;
            await character.SendPacketAsync(character.GenerateGold());
            await character.SendPacketAsync(character.GenerateSayi(SayColorType.Yellow,
                Game18NConstString.EarnedReputationAndReward, 4, gold));
        }

        private static List<RepaySubPacketItem> Slots(IReadOnlyList<InstanceGift> gifts, int count) =>
            [.. gifts.Take(count).Select(Slot)];

        private static RepaySubPacketItem Slot(InstanceGift? gift) =>
            gift == null ? new RepaySubPacketItem() : new RepaySubPacketItem { ItemId = gift.VNum, Amount = gift.Amount };
    }
}
