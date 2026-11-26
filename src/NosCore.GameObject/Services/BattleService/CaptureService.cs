//  __  _  __    __   ___ __  ___ ___
// |  \| |/__\ /' _/ / _//__\| _ \ __|
// | | ' | \/ |`._`.| \_| \/ | v / _|
// |_|\__|\__/ |___/ \__/\__/|_|_\___|
//

using System;
using System.Linq;
using System.Threading.Tasks;
using NodaTime;
using NosCore.Dao.Interfaces;
using NosCore.Data.Dto;
using NosCore.Data.Enumerations.Buff;
using NosCore.Data.Enumerations.Character;
using NosCore.GameObject.Ecs;
using NosCore.GameObject.Ecs.Extensions;
using NosCore.GameObject.Ecs.Interfaces;
using NosCore.GameObject.Messaging.Events;
using NosCore.GameObject.Messaging;
using NosCore.GameObject.Services.BattleService.Model;
using NosCore.GameObject.Services.GroupService;
using NosCore.GameObject.Services.MateService;
using NosCore.GameObject.Services.QuestService;
using NosCore.Networking;
using NosCore.Packets.Enumerations;
using NosCore.Packets.ServerPackets.Battle;
using NosCore.Packets.ServerPackets.Entities;
using NosCore.Packets.ServerPackets.Mates;
using NosCore.Packets.ServerPackets.UI;
using NosCore.PathFinder.Interfaces;
using NosCore.Shared.Enumerations;

namespace NosCore.GameObject.Services.BattleService
{
    public sealed class CaptureService(IDao<MateDto, long> mateDao, IMateService mateService,
        IGameEventDispatcher gameEvents, IClock clock, IRandomProvider random, IHeuristic distanceCalculator,
        IPartnerSkillCatalog partnerSkillCatalog) : ICaptureService
    {
        private const int CaptureHpThresholdPercent = 50;
        private const int LowLevelCaptureRate = 90;
        private const int DefaultCaptureRate = 50;
        private const int LowLevelCutoff = 20;
        private const byte MateStartLevelOffset = 15;
        private const short NewPetLoyalty = 3000;
        private const int CaughtEffect = 197;
        private const short CaughtAnimation = 15;
        private const short EscapedAnimation = 16;

        public bool IsCaptureSkill(SkillInfo skill) =>
            HasCaptureBCard(skill);

        public async Task<bool> TryCaptureAsync(IAliveEntity caster, IAliveEntity target, SkillInfo skill)
        {
            if (!HasCaptureBCard(skill) || caster is not PlayerComponentBundle owner)
            {
                return false;
            }

            if (target is not MonsterComponentBundle monster
                || monster.NpcMonster == null || !monster.IsAlive || monster.MapInstance == null)
            {
                await owner.SendPacketAsync(Cancel(skill.CastId));
                return false;
            }

            var distance = (int)distanceCalculator.GetDistance((owner.PositionX, owner.PositionY),
                (monster.PositionX, monster.PositionY));
            if (monster.MapInstance != owner.MapInstance
                || !SkillReach.IsInReach(skill.TargetType, skill.Range, skill.TargetRange, distance, monster.NpcMonster.BasicArea))
            {
                await owner.SendPacketAsync(Cancel(skill.CastId));
                return false;
            }

            await monster.MapInstance.SendPacketAsync(new CtPacket
            {
                VisualType = owner.VisualType,
                VisualId = owner.VisualId,
                TargetVisualType = monster.VisualType,
                TargetId = monster.VisualId,
                CastAnimationId = skill.CastAnimation,
                CastEffect = skill.CastEffect,
                SkillId = skill.SkillVnum
            });

            // A refused attempt still costs its mana and cooldown; the cancel naming the skill
            // rather than the cast ends the client's cast.
            if (monster.NpcMonster.Level > owner.Level)
            {
                await owner.SendPacketAsync(Message(Game18NConstString.CaptureToHighLevel));
                await owner.SendPacketAsync(Cancel(skill.SkillVnum));
                return true;
            }

            var maxHp = monster.NpcMonster.MaxHp;
            var hpPercent = maxHp > 0 ? monster.Hp * 100 / maxHp : 100;
            if (maxHp <= 0 || monster.Hp * 100L > maxHp * (long)CaptureHpThresholdPercent)
            {
                await owner.SendPacketAsync(Message(Game18NConstString.CaptureTooMuchLife));
                await owner.SendPacketAsync(Cancel(skill.SkillVnum));
                return true;
            }

            var rate = owner.Level < LowLevelCutoff ? LowLevelCaptureRate : DefaultCaptureRate;
            if (random.Next(0, 100) >= rate)
            {
                await owner.SendPacketAsync(Message(Game18NConstString.CaptureFailed));
                await monster.MapInstance.SendPacketAsync(Animation(owner, monster, hpPercent, EscapedAnimation));
                return true;
            }

            // Claimed before anything is awaited, so a second capture or a kill cannot take it as well.
            var caughtAt = Animation(owner, monster, hpPercent, CaughtAnimation);
            monster.Hp = 0;
            monster.IsAlive = false;
            monster.HitList.Clear();
            monster.RespawnAt = clock.GetCurrentInstant().Plus(RespawnTiming.For(monster.NpcMonster));

            // No catch animation here: the quest's progress, the message and the cancel end the cast.
            if (owner.CapturedMonsterGoesToQuest(monster.NpcMonster.NpcMonsterVNum))
            {
                await gameEvents.RaiseAsync(new EntityCapturedEvent(monster, owner, BecamePet: false));
                await owner.SendPacketAsync(Message(Game18NConstString.CaptureSuccessfulQuest));
                await owner.SendPacketAsync(Cancel(skill.SkillVnum));
                await monster.MapInstance.SendPacketAsync(monster.GenerateOut());
                return true;
            }

            var row = await mateDao.TryInsertOrUpdateAsync(new MateDto
            {
                CharacterId = owner.CharacterId,
                VNum = (short)monster.NpcMonster.NpcMonsterVNum,
                MateType = MateType.Pet,
                Level = (byte)Math.Max(1, monster.NpcMonster.Level - MateStartLevelOffset),
                Loyalty = NewPetLoyalty,
                Experience = 0,
                Hp = monster.NpcMonster.MaxHp,
                Mp = monster.NpcMonster.MaxMp,
                IsSummonable = true,
                IsTeamMember = !owner.Mates.Values.Any(s => s.MateType == MateType.Pet && s.IsTeamMember)
            });
            var mate = mateService.Adopt(row, monster.NpcMonster, owner.Mates.Values);
            owner.Mates[mate.MateTransportId] = mate;

            await owner.SendPacketAsync(new CtlPacket
            {
                Type = VisualType.Npc, PetId = mate.MateTransportId, Action = 3, Unknown = 0
            });
            await owner.SendPacketAsync(Message(Game18NConstString.CaptureSuccessful));
            await gameEvents.RaiseAsync(new EntityCapturedEvent(monster, owner, BecamePet: true));
            await owner.SendPacketsAsync(owner.GenerateMateRoster(partnerSkillCatalog));
            if (mate.IsTeamMember)
            {
                await mate.SpawnAsync(owner, monster.MapInstance, monster.PositionX, monster.PositionY);
                await owner.SendPacketAsync(owner.GeneratePinit());
                await owner.SendPacketsAsync(owner.GenerateMateRoster(partnerSkillCatalog));
            }

            await ShowCaughtAsync(owner, monster, caughtAt);
            if (mate.IsTeamMember)
            {
                await owner.SendPacketAsync(mate.GenerateCond());
                await owner.SendPacketAsync(mate.GeneratePst());
            }

            return true;
        }

        private static async Task ShowCaughtAsync(PlayerComponentBundle owner, MonsterComponentBundle monster, SuPacket caughtAt)
        {
            await monster.MapInstance.SendPacketAsync(caughtAt);
            await monster.MapInstance.SendPacketAsync(owner.GenerateEff(CaughtEffect));
            await monster.MapInstance.SendPacketAsync(monster.GenerateOut());
        }

        private static SuPacket Animation(PlayerComponentBundle owner, MonsterComponentBundle monster, int hpPercent,
            short animation) => new()
        {
            VisualType = VisualType.Player,
            VisualId = owner.VisualId,
            TargetVisualType = VisualType.Monster,
            TargetId = monster.VisualId,
            SkillVnum = -1,
            SkillCooldown = 0,
            AttackAnimation = animation,
            SkillEffect = -1,
            PositionX = -1,
            PositionY = -1,
            TargetIsAlive = true,
            HpPercentage = (byte)hpPercent,
            Damage = 0,
            HitMode = SuPacketHitMode.NoDamageSuccess,
            SkillTypeMinusOne = 0,
            TargetCurrentHp = monster.Hp,
            TargetMaxHp = monster.NpcMonster.MaxHp
        };

        private static CancelPacket Cancel(long castOrSkill) =>
            new() { Type = CancelPacketType.CancelAutoAttack, TargetId = castOrSkill, Unknow = -1 };

        private static MsgiPacket Message(Game18NConstString message) =>
            new() { Type = MessageType.Default, Message = message };

        private static bool HasCaptureBCard(SkillInfo skill) =>
            skill.BCards.Any(b => b.Effect() == BCardEffect.CaptureCaptureAnimal);
    }
}
