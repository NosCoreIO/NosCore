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
using NosCore.Data.Enumerations.Character;
using NosCore.Data.StaticEntities;
using NosCore.GameObject.Ecs;
using NosCore.GameObject.Ecs.Extensions;
using NosCore.GameObject.Ecs.Interfaces;
using NosCore.GameObject.Infastructure;
using NosCore.GameObject.Services.BattleService;
using NosCore.GameObject.Services.SkillService;
using NosCore.Networking;
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
    public sealed class PartnerSpecialistService(IClock clock, List<SkillDto> skills, IBattleService battleService,
            IHeuristic distanceCalculator) : IPartnerSpecialistService, ISingletonService
    {
        private const short CooldownSeconds = 30;
        private const short TransformDelayMilliseconds = 5000;
        private const int TransformEffect = 196;
        private const int MinimumSkillCooldownMs = 200;

        private readonly ILookup<short, SkillDto> _skillsBySet = skills
            .Where(s => s.IsPartnerSkill())
            .OrderBy(s => s.CastId)
            .ToLookup(s => s.UpgradeType);

        public IReadOnlyList<SkillDto> Skills(PlayerComponentBundle owner, Mate partner) =>
            partner.SpecialistMorph != null && partner.Specialist(owner.InventoryService) is { } specialist
                ? _skillsBySet[specialist.Item.PartnerSkillSet].ToList()
                : [];

        public async Task<bool> CastAsync(PlayerComponentBundle owner, long mateTransportId, int slot, IAliveEntity target)
        {
            if (!owner.Mates.TryGetValue(mateTransportId, out var partner)
                || !partner.IsTeamMember
                || partner.Entity is not { } entity
                || target.MapInstance != entity.MapInstance
                || Skills(owner, partner).FirstOrDefault(s => s.CastId == slot) is not { } skill
                || entity.Mp < skill.MpCost)
            {
                return false;
            }

            var now = clock.GetCurrentInstant();
            if (entity.NextUsableAt.TryGetValue(skill.SkillVNum, out var readyAt) && readyAt > now)
            {
                return false;
            }

            var distance = (int)distanceCalculator.GetDistance((entity.PositionX, entity.PositionY),
                (target.PositionX, target.PositionY));
            var targetArea = target is INonPlayableEntity { NpcMonster: { } npcMonster } ? npcMonster.BasicArea : (byte)0;
            if (!SkillReach.IsInReach(skill.TargetType, skill.Range, skill.TargetRange, distance, targetArea)
                || !await battleService.Hit(entity, target, new HitArguments { SkillId = skill.CastId, Skill = skill }))
            {
                return false;
            }

            entity.NextUsableAt[skill.SkillVNum] =
                now.Plus(Duration.FromMilliseconds(Math.Max(MinimumSkillCooldownMs, skill.Cooldown * 100)));
            entity.Mp -= skill.MpCost;
            await owner.SendPacketAsync(partner.GeneratePst());
            return true;
        }

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
            await SendAsync(owner, new GuriPacket
            {
                Type = GuriPacketType.Dance,
                Argument = (uint)VisualType.Npc,
                EntityId = partner.MateTransportId
            });
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

            await owner.SendPacketAsync(partner.GenerateScn(owner.AccountLanguage, owner.InventoryService, partnerSkills));
            await SendAsync(owner, entity.GenerateOut());
            await SendAsync(owner, partner.GenerateIn(owner.AccountLanguage, entity.PositionX, entity.PositionY, partnerSkills));
        }

        private static Mate? TeamPartner(PlayerComponentBundle owner) =>
            owner.Mates.Values.FirstOrDefault(s => s.MateType == MateType.Partner && s.IsTeamMember && s.Entity != null);

        // Broadcasting a hidden owner's mates would put the owner back on screen.
        private static Task SendAsync(PlayerComponentBundle owner, IPacket packet) =>
            owner.Invisible ? owner.SendPacketAsync(packet) : owner.MapInstance.SendPacketAsync(packet);
    }
}
