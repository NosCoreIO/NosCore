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
using NosCore.GameObject.Ecs.Interfaces;
using NosCore.GameObject.Messaging.Events;
using NosCore.GameObject.Services.BattleService.Model;
using NosCore.Networking;
using NosCore.Packets.Enumerations;
using NosCore.Packets.ServerPackets.Entities;
using NosCore.Shared.Enumerations;
using NosCore.Shared.Helpers;
using Wolverine;

namespace NosCore.GameObject.Services.BattleService
{
    // Capture rules mirror vanosilla BCardCaptureHandler + MonsterCaptureEventHandler:
    //   - target must be a monster at <50 % HP
    //   - monster.Level <= player.Level
    //   - guards for raid instance / player dignity skipped here (kept for a follow-up;
    //     dignity/MapInstanceType don't affect the mechanic enough to block an MVP)
    //   - rate: 90 % if player < level 20, else 50 %
    //   - mate level = max(monster.Level - 15, 1)
    public sealed class CaptureService(IDao<MateDto, long> mateDao, IMessageBus messageBus, IClock clock) : ICaptureService
    {
        private const int CaptureHpThresholdPercent = 50;
        private const int LowLevelCaptureRate = 90;
        private const int DefaultCaptureRate = 50;
        private const int LowLevelCutoff = 20;
        private const byte MateStartLevelOffset = 15;

        public bool IsCaptureSkill(SkillInfo skill) =>
            HasCaptureBCard(skill);

        public async Task TryCaptureAsync(IAliveEntity caster, IAliveEntity target, SkillInfo skill)
        {
            if (!HasCaptureBCard(skill))
            {
                return;
            }

            if (caster is not ICharacterEntity character)
            {
                return;
            }

            if (target is not MonsterComponentBundle monster)
            {
                return;
            }

            if (monster.NpcMonster == null || !monster.IsAlive || monster.MapInstance == null)
            {
                return;
            }

            if (monster.NpcMonster.Level > character.Level)
            {
                return;
            }

            var maxHp = monster.NpcMonster.MaxHp;
            var hpPercent = maxHp > 0 ? (monster.Hp * 100 / maxHp) : 100;
            if (hpPercent >= CaptureHpThresholdPercent)
            {
                return;
            }

            var rate = character.Level < LowLevelCutoff ? LowLevelCaptureRate : DefaultCaptureRate;
            if (RandomHelper.Instance.RandomNumber(0, 100) >= rate)
            {
                return;
            }

            var mateLevel = (byte)System.Math.Max(1, monster.NpcMonster.Level - MateStartLevelOffset);

            await mateDao.TryInsertOrUpdateAsync(new MateDto
            {
                CharacterId = character.CharacterId,
                VNum = (short)monster.NpcMonster.NpcMonsterVNum,
                MateType = MateType.Pet,
                Level = mateLevel,
                Loyalty = 1000,
                Experience = 0,
                Hp = monster.NpcMonster.MaxHp,
                Mp = monster.NpcMonster.MaxMp,
                IsSummonable = true,
            }).ConfigureAwait(false);

            monster.Hp = 0;
            monster.IsAlive = false;
            monster.HitList.Clear();

            await monster.MapInstance.SendPacketAsync(new OutPacket
            {
                VisualType = VisualType.Monster,
                VisualId = monster.VisualId
            }).ConfigureAwait(false);

            // Capture still vacates the spawn point; re-use the same map-tick respawn
            // path MonsterRespawnHandler uses for kills so a captured monster eventually
            // reappears at its spawn. Going through ScheduleRespawn directly (rather
            // than via EntityDiedEvent) avoids pulling in the kill pipeline's xp/gold
            // rewards, death bcards and hunt-quest credit.
            var respawnMs = Math.Max(1000, monster.NpcMonster.RespawnTime);
            monster.MapInstance.ScheduleRespawn(monster, clock.GetCurrentInstant().Plus(Duration.FromMilliseconds(respawnMs)));

            await messageBus.PublishAsync(new EntityCapturedEvent(monster, character)).ConfigureAwait(false);
        }

        private static bool HasCaptureBCard(SkillInfo skill) =>
            skill.BCards.Any(b =>
                (BCardType.CardType)b.Type == BCardType.CardType.Capture
                && (AdditionalTypes.Capture)b.SubType == AdditionalTypes.Capture.CaptureAnimal);
    }
}
