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
using NosCore.GameObject.Services.BattleService.Model;
using NosCore.GameObject.Services.MateService;
using NosCore.Networking;
using NosCore.Packets.Enumerations;
using NosCore.Packets.ServerPackets.Entities;
using NosCore.Packets.ServerPackets.Mates;
using NosCore.Packets.ServerPackets.UI;
using NosCore.Shared.Enumerations;
using Wolverine;

namespace NosCore.GameObject.Services.BattleService
{
    // A monster can be caught below half health and at or under the player's level. The new pet
    // starts fifteen levels below the monster and comes out at once when no other pet is out.
    public sealed class CaptureService(IDao<MateDto, long> mateDao, IMateService mateService,
        IMessageBus messageBus, IClock clock, IRandomProvider random) : ICaptureService
    {
        private const int CaptureHpThresholdPercent = 50;
        private const int LowLevelCaptureRate = 90;
        private const int DefaultCaptureRate = 50;
        private const int LowLevelCutoff = 20;
        private const byte MateStartLevelOffset = 15;
        private const short NewPetLoyalty = 3000;
        private const int CaughtEffect = 197;

        public bool IsCaptureSkill(SkillInfo skill) =>
            HasCaptureBCard(skill);

        public async Task TryCaptureAsync(IAliveEntity caster, IAliveEntity target, SkillInfo skill)
        {
            if (!HasCaptureBCard(skill) || caster is not PlayerComponentBundle owner
                || target is not MonsterComponentBundle monster
                || monster.NpcMonster == null || !monster.IsAlive || monster.MapInstance == null)
            {
                return;
            }

            if (monster.NpcMonster.Level > owner.Level)
            {
                await owner.SendPacketAsync(Message(Game18NConstString.CaptureToHighLevel));
                return;
            }

            var maxHp = monster.NpcMonster.MaxHp;
            var hpPercent = maxHp > 0 ? monster.Hp * 100 / maxHp : 100;
            if (hpPercent >= CaptureHpThresholdPercent)
            {
                await owner.SendPacketAsync(Message(Game18NConstString.CaptureTooMuchLife));
                return;
            }

            var rate = owner.Level < LowLevelCutoff ? LowLevelCaptureRate : DefaultCaptureRate;
            if (random.Next(0, 100) >= rate)
            {
                await owner.SendPacketAsync(Message(Game18NConstString.CaptureFailed));
                return;
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

            // Action 3 is the value the capture shows; the same ctl goes out when a pet leaves a map.
            await owner.SendPacketAsync(new CtlPacket
            {
                Type = (byte)VisualType.Npc, PetId = mate.MateTransportId, Action = 3, Unknown = 0
            });
            await owner.SendPacketAsync(Message(Game18NConstString.CaptureSuccessful));
            await owner.SendPacketsAsync(owner.GenerateMateRoster());
            if (mate.IsTeamMember)
            {
                await mate.SpawnAsync(owner, monster.MapInstance, monster.PositionX, monster.PositionY);
                await owner.SendPacketAsync(owner.GeneratePinit());
                await owner.SendPacketsAsync(owner.GenerateMateRoster());
            }

            await monster.MapInstance.SendPacketAsync(owner.GenerateEff(CaughtEffect));

            monster.Hp = 0;
            monster.IsAlive = false;
            monster.HitList.Clear();

            await monster.MapInstance.SendPacketAsync(new OutPacket
            {
                VisualType = VisualType.Monster,
                VisualId = monster.VisualId
            });

            // Same delayed respawn as a kill, without the kill's rewards and death effects.
            monster.MapInstance.ScheduleRespawn(monster,
                clock.GetCurrentInstant().Plus(RespawnTiming.For(monster.NpcMonster)));

            await messageBus.PublishAsync(new EntityCapturedEvent(monster, owner));
        }

        private static MsgiPacket Message(Game18NConstString message) =>
            new() { Type = MessageType.Default, Message = message };

        private static bool HasCaptureBCard(SkillInfo skill) =>
            skill.BCards.Any(b => b.Effect() == BCardEffect.CaptureCaptureAnimal);
    }
}
