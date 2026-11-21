//  __  _  __    __   ___ __  ___ ___
// |  \| |/__\ /' _/ / _//__\| _ \ __|
// | | ' | \/ |`._`.| \_| \/ | v / _|
// |_|\__|\__/ |___/ \__/\__/|_|_\___|
//

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using NodaTime;
using NosCore.Algorithm.PartnerAgilityService;
using NosCore.Algorithm.PartnerSkillRankService;
using NosCore.Data.Enumerations.Character;
using NosCore.Data.Enumerations.Items;
using NosCore.Data.StaticEntities;
using NosCore.GameObject.Ecs;
using NosCore.GameObject.Ecs.Extensions;
using NosCore.GameObject.Ecs.Interfaces;
using NosCore.GameObject.Infastructure;
using NosCore.GameObject.Services.BattleService;
using NosCore.GameObject.Services.InventoryService;
using NosCore.GameObject.Services.ItemGenerationService.Item;
using NosCore.GameObject.Services.SkillService;
using NosCore.GameObject.Services.UpgradeService;
using NosCore.Networking;
using NosCore.Packets.ClientPackets.Inventory;
using NosCore.Packets.ClientPackets.Mates;
using NosCore.Packets.Enumerations;
using NosCore.Packets.Interfaces;
using NosCore.Packets.ServerPackets.Chats;
using NosCore.Packets.ServerPackets.Mates;
using NosCore.Packets.ServerPackets.UI;
using NosCore.PathFinder.Interfaces;
using NosCore.Shared.Enumerations;

namespace NosCore.GameObject.Services.MateService
{
    public sealed class PartnerSpecialistService(IClock clock, IPartnerSkillCatalog partnerSkillCatalog, IBattleService battleService,
            IHeuristic distanceCalculator, IPartnerSkillRankService partnerSkillRankService,
            IPartnerAgilityService partnerAgilityService, IRandomProvider random)
        : IPartnerSpecialistService, ISingletonService
    {
        private const short CooldownSeconds = 30;
        private const short TransformDelayMilliseconds = 5000;
        private const int TransformEffect = 196;

        public IReadOnlyList<SkillDto> Skills(PlayerComponentBundle owner, Mate partner) =>
            partner.SpecialistMorph != null && partner.Specialist(owner.InventoryService) is { } specialist
                ? partnerSkillCatalog.CardSkills(specialist)
                : [];

        public Task<bool> CastAsync(PlayerComponentBundle owner, long mateTransportId, int slot, IAliveEntity target) =>
            owner.Mates.TryGetValue(mateTransportId, out var partner)
            && Skills(owner, partner).FirstOrDefault(s => s.CastId == slot) is { } skill
                ? MateSkillCast.CastAsync(owner, partner, skill, target, battleService, distanceCalculator,
                    clock.GetCurrentInstant())
                : Task.FromResult(false);

        public async Task ToggleAsync(PlayerComponentBundle owner)
        {
            if (owner.IsVehicled || TeamPartner(owner) is not { } partner)
            {
                return;
            }

            if (partner.SpecialistMorph != null)
            {
                await UntransformAsync(owner, partner);
                return;
            }

            if (partner.Specialist(owner.InventoryService) is not { } specialist)
            {
                await owner.SendPacketAsync(new MsgiPacket
                {
                    Type = MessageType.Default,
                    Message = Game18NConstString.NoSpecialistCardEquipped
                });
                return;
            }

            var now = clock.GetCurrentInstant();
            if (partner.SpecialistCooldownEndsAt > now)
            {
                await owner.SendPacketAsync(new MsgiPacket
                {
                    Type = MessageType.Default,
                    Message = Game18NConstString.CantTrasformWithSideEffect,
                    ArgumentType = 4,
                    Game18NArguments = { (short)Math.Ceiling((partner.SpecialistCooldownEndsAt - now).TotalSeconds) }
                });
                return;
            }

            await owner.StartDelayAsync(new PdelayPacket
            {
                Type = DelayPacketType.Locomotion,
                Delay = TransformDelayMilliseconds,
                Packet = new PslPacket { Type = PslPacketType.Wear }
            }, specialist.Id, now);
        }

        public async Task TransformAsync(PlayerComponentBundle owner)
        {
            if (TeamPartner(owner) is not { Entity: { } entity } partner
                || partner.SpecialistMorph != null
                || partner.Specialist(owner.InventoryService) is not { } specialist
                || !owner.TryCompleteDelay(DelayPacketType.Locomotion, specialist.Id, clock.GetCurrentInstant()))
            {
                return;
            }

            partner.SpecialistMorph = specialist.Item.Morph;
            partner.SpecialistName = specialist.Item.PartnerSpecialistName;
            entity.Morph = partner.Morph();
            await RefreshAsync(owner, partner, entity);
            await SendAsync(owner, entity.GenerateEff(TransformEffect));
        }

        public async Task UntransformAsync(PlayerComponentBundle owner, Mate partner)
        {
            if (partner.SpecialistMorph == null)
            {
                return;
            }

            partner.SpecialistMorph = null;
            partner.SpecialistName = 0;
            partner.SpecialistCooldownEndsAt = clock.GetCurrentInstant() + Duration.FromSeconds(CooldownSeconds);
            await owner.SendPacketAsync(new DpskiPacket());
            if (partner.Entity is { } entity)
            {
                entity.Morph = partner.Morph();
                await RefreshAsync(owner, partner, entity);
            }

            await owner.SendPacketAsync(owner.GenerateSayi(SayColorType.Red, Game18NConstString.DurationOfSideEffect,
                4, CooldownSeconds));
            await owner.SendPacketAsync(new PsdPacket { SpCooldown = CooldownSeconds });
        }

        private async Task RefreshAsync(PlayerComponentBundle owner, Mate partner, MateComponentBundle entity)
        {
            var partnerSkills = Skills(owner, partner);
            await owner.SendPacketAsync(partner.GenerateCond());
            await SendAsync(owner, entity.GenerateCMode());
            if (partner.SpecialistMorph != null)
            {
                await owner.SendPacketAsync(partnerSkills.GeneratePski());
            }

            await owner.SendPacketAsync(partner.GenerateScn(owner.AccountLanguage, owner.InventoryService, partnerSkillCatalog));
            await SendAsync(owner, entity.GenerateOut());
            await SendAsync(owner, partner.GenerateIn(owner.AccountLanguage, entity.PositionX, entity.PositionY, partnerSkills));
        }

        public async Task LearnSkillAsync(PlayerComponentBundle owner, byte petSlot, byte skillSlot)
        {
            if (owner.Mates.Values.FirstOrDefault(s => s.MateType == MateType.Partner && s.PetSlot == petSlot) is not { } partner)
            {
                return;
            }

            if (!partner.IsTeamMember)
            {
                await SayAsync(owner, Game18NConstString.PartnerMustAccompanying);
                return;
            }

            if (partner.Specialist(owner.InventoryService) is not { } card)
            {
                await SayAsync(owner, Game18NConstString.EquipPartnerWithSpecialist);
                return;
            }

            if (partner.SpecialistMorph != null)
            {
                await SayAsync(owner, Game18NConstString.CancelPartnerSpecialist);
                return;
            }

            if (skillSlot >= partnerSkillCatalog.CardSkills(card).Count)
            {
                return;
            }

            if (card.PartnerSkillRank(skillSlot) != SpPartnerRank.NotIdentifiedYet)
            {
                await SayAsync(owner, Game18NConstString.PartnerHasSkill);
                return;
            }

            if (card.Agility < partnerAgilityService.MaxAgility)
            {
                await SayAsync(owner, Game18NConstString.PartnerNeedAgility);
                return;
            }

            card.SetPartnerSkillRank(skillSlot, RollRank());
            card.Agility = 0;
            await owner.SendPacketAsync(partner.GenerateScn(owner.AccountLanguage, owner.InventoryService, partnerSkillCatalog));
            await SayAsync(owner, Game18NConstString.YourPartnerNowHasSkill);
        }

        public async Task UseSkillTicketAsync(PlayerComponentBundle owner, InventoryItemInstance ticket, UseItemPacket packet)
        {
            var resetsAll = ticket.ItemInstance.Item.Effect == ItemEffectType.PartnerSkillTicketAll;
            if (packet.TargetType is not { } petSlot || packet.TargetSlot is not { } skillSlot
                || owner.Mates.Values.FirstOrDefault(s => s.MateType == MateType.Partner && s.PetSlot == (byte)petSlot) is not { } partner)
            {
                return;
            }

            if (!partner.IsTeamMember)
            {
                await SayAsync(owner, Game18NConstString.PartnerMustAccompanying);
                return;
            }

            if (partner.Specialist(owner.InventoryService) is not { } card)
            {
                await SayAsync(owner, Game18NConstString.EquipPartnerWithSpecialist);
                return;
            }

            if (partner.SpecialistMorph != null)
            {
                await SayAsync(owner, Game18NConstString.CancelPartnerSpecialist);
                return;
            }

            var slots = Enumerable.Range(0, partnerSkillCatalog.CardSkills(card).Count).ToList();
            if (skillSlot < 0 || skillSlot >= slots.Count)
            {
                return;
            }

            var learned = slots.Where(slot => card.PartnerSkillRank(slot) != SpPartnerRank.NotIdentifiedYet).ToList();
            if (resetsAll && learned.Count == 0)
            {
                await SayAsync(owner, Game18NConstString.SpecialistMustHasOneSkill);
                return;
            }

            if (!resetsAll && !learned.Contains(skillSlot))
            {
                await SayAsync(owner, Game18NConstString.SkillLevelInsufficient);
                return;
            }

            var hasRankS = (resetsAll ? learned : [skillSlot]).Any(slot => card.PartnerSkillRank(slot) == SpPartnerRank.S);
            if (!resetsAll && hasRankS)
            {
                await SayAsync(owner, Game18NConstString.CannotUseIfSkillLevelReached);
                return;
            }

            if (packet.Mode == 0)
            {
                await owner.SendPacketAsync(new QnaiPacket
                {
                    YesPacket = new UseItemPacket
                    {
                        VisualType = owner.VisualType,
                        VisualId = owner.VisualId,
                        Type = ticket.Type.ToPocketType(),
                        Slot = ticket.Slot,
                        Mode = 1,
                        Parameter = packet.Parameter,
                        TargetType = petSlot,
                        TargetSlot = skillSlot
                    },
                    Question = !resetsAll ? Game18NConstString.AskChangeSkillLevelChosen
                        : hasRankS ? Game18NConstString.PartnersSResetItQuestion
                        : Game18NConstString.WantToChangeAllSkillLevels
                });
                return;
            }

            foreach (var slot in resetsAll ? learned : [skillSlot])
            {
                card.SetPartnerSkillRank(slot, SpPartnerRank.NotIdentifiedYet);
            }

            card.Agility = partnerAgilityService.MaxAgility;
            await owner.UseUpOneAsync(ticket);
            await owner.SendPacketAsync(partner.GenerateScn(owner.AccountLanguage, owner.InventoryService, partnerSkillCatalog));
            await SayAsync(owner, resetsAll ? Game18NConstString.AllSkillReset : Game18NConstString.SkillLevelChanged);
        }

        private SpPartnerRank RollRank()
        {
            var ranks = Enumerable.Range(partnerSkillRankService.MinRank,
                    partnerSkillRankService.MaxRank - partnerSkillRankService.MinRank + 1)
                .Select(rank => (byte)rank)
                .ToList();
            var roll = random.NextDouble() * ranks.Sum(partnerSkillRankService.GetWeight);
            foreach (var rank in ranks)
            {
                roll -= partnerSkillRankService.GetWeight(rank);
                if (roll < 0)
                {
                    return (SpPartnerRank)rank;
                }
            }

            return (SpPartnerRank)ranks[^1];
        }

        private static Task SayAsync(PlayerComponentBundle owner, Game18NConstString message) =>
            owner.SendPacketAsync(new MsgiPacket { Type = MessageType.Default, Message = message });

        private static Mate? TeamPartner(PlayerComponentBundle owner) =>
            owner.Mates.Values.FirstOrDefault(s => s.MateType == MateType.Partner && s.IsTeamMember && s.Entity != null);

        // Broadcasting a hidden owner's mates would put the owner back on screen.
        private static Task SendAsync(PlayerComponentBundle owner, IPacket packet) =>
            owner.Invisible ? owner.SendPacketAsync(packet) : owner.MapInstance.SendPacketAsync(packet);
    }
}
