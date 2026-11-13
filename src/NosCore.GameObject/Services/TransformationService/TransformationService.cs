//  __  _  __    __   ___ __  ___ ___
// |  \| |/__\ /' _/ / _//__\| _ \ __|
// | | ' | \/ |`._`.| \_| \/ | v / _|
// |_|\__|\__/ |___/ \__/\__/|_|_\___|
//

using Microsoft.Extensions.Options;
using NodaTime;
using NosCore.Algorithm.ExperienceService;
using NosCore.Algorithm.HeroExperienceService;
using NosCore.Algorithm.SkillPointService;
using NosCore.Algorithm.JobExperienceService;
using NosCore.Core.Configuration;
using NosCore.Data.Enumerations;
using NosCore.Data.Enumerations.I18N;
using NosCore.GameObject.Ecs.Extensions;
using NosCore.Data.StaticEntities;
using NosCore.GameObject.Networking.ClientSession;
using NosCore.GameObject.Services.ItemGenerationService.Item;
using NosCore.Networking;
using NosCore.Packets.Enumerations;
using NosCore.Packets.ServerPackets.Chats;
using NosCore.Packets.ServerPackets.Specialists;
using NosCore.Packets.ServerPackets.UI;
using NosCore.Shared.Enumerations;
using NosCore.Shared.I18N;
using Microsoft.Extensions.Logging;
using System;
using NosCore.Data.Enumerations.Items;
using System.Linq;
using System.Reactive.Linq;
using System.Threading.Tasks;

namespace NosCore.GameObject.Services.TransformationService
{
    public class TransformationService(IClock clock, IExperienceService experienceService,
            IJobExperienceService jobExperienceService, IHeroExperienceService heroExperienceService, ISkillPointService skillPointService, ILogger<TransformationService> logger,
            ILogLanguageLocalizer<LogLanguageKey> logLanguage, IOptions<WorldConfiguration> worldConfiguration,
            SpeedCalculationService.ISpeedCalculationService speedCalculationService,
            SkillService.ISkillService skillService,
            BattleService.IBattleStatsProvider battleStatsProvider,
            BattleService.IBuffService buffService,
            BattleService.ICardCatalog cardCatalog)
        : ITransformationService
    {
        public async Task RemoveSpAsync(ClientSession session)
        {
            var character = session.Character;
            character.UseSp = false;
            character.Morph = 0;
            character.MorphUpgrade = 0;
            character.MorphDesign = 0;

            await skillService.UnloadSpecialistSkillsAsync(character);
            character.SpCooldown = 30;

            var characterId = character.CharacterId;
            var spCooldown = character.SpCooldown;
            var mapInstance = character.MapInstance;
            var condPacket = session.Character.GenerateCond();
            var levPacket = session.Character.GenerateLev(experienceService, jobExperienceService, heroExperienceService, skillPointService);
            var cModePacket = session.Character.GenerateCMode();
            var statPacket = session.Character.GenerateStat();
            var scPacket = session.Character.GenerateSc(battleStatsProvider);

            await session.SendPacketAsync(condPacket);
            await session.SendPacketAsync(levPacket);
            await session.SendPacketAsync(scPacket);
            await session.SendPacketAsync(character.GenerateSayi(SayColorType.Red, Game18NConstString.DurationOfSideEffect, 4, spCooldown));
            await session.SendPacketAsync(new SdPacket { Cooldown = spCooldown });
            await mapInstance.SendPacketAsync(cModePacket);
            await mapInstance.SendPacketAsync(new GuriPacket
            {
                Type = GuriPacketType.Dance,
                Value = 1,
                EntityId = characterId
            });
            await session.SendPacketAsync(statPacket);

            var sideEffectGone = character.GenerateSayi(SayColorType.Red, Game18NConstString.TransformationSideEffectGone);

            async Task CoolDown()
            {
                await session.SendPacketAsync(sideEffectGone);
                await session.SendPacketAsync(new SdPacket { Cooldown = 0 });
            }

            Observable.Timer(TimeSpan.FromMilliseconds(spCooldown * 1000)).Select(_ => CoolDown()).Subscribe();
        }

        public async Task ChangeSpAsync(ClientSession session)
        {
            var character = session.Character;
            if (character.InventoryService.LoadBySlotAndType((byte)EquipmentType.Sp, NoscorePocketType.Wear)?.ItemInstance is
                not SpecialistInstance sp)
            {
                logger.LogError(logLanguage[LogLanguageKey.USE_SP_WITHOUT_SP_ERROR]);
                return;
            }

            if (character.ReputIconValue < sp.Item.ReputationMinimum)
            {
                await session.SendPacketAsync(character.GenerateSayi(SayColorType.Yellow, Game18NConstString.CanNotBeWornReputationLow));
                return;
            }

            var inventoryService = character.InventoryService;
            if (inventoryService.LoadBySlotAndType((byte)EquipmentType.Fairy, NoscorePocketType.Wear)?.ItemInstance is
                    WearableInstance fairy
                && (sp.Item.Element != 0) && (fairy.Item.Element != sp.Item.Element)
                && (fairy.Item.Element != sp.Item.SecondaryElement))
            {
                await session.SendPacketAsync(new MsgiPacket
                {
                    Type = MessageType.Default,
                    Message = Game18NConstString.SpecialistAndFairyDifferentElement
                });
                return;
            }

            character = session.Character;
            character.LastSp = clock.GetCurrentInstant();
            character.UseSp = true;
            character.Morph = sp.Item.Morph;
            character.MorphUpgrade = (byte)sp.Upgrade;
            character.MorphDesign = (byte)sp.Design;

            await skillService.LoadSpecialistSkillsAsync(character, sp.Item.Morph, sp.SpLevel);

            var characterId2 = character.CharacterId;
            var mapInstance = character.MapInstance;
            var cModePacket = session.Character.GenerateCMode();
            var levPacket = session.Character.GenerateLev(experienceService, jobExperienceService, heroExperienceService, skillPointService);
            var effPacket = session.Character.GenerateEff(196);
            var spPointPacket = session.Character.GenerateSpPoint(worldConfiguration);
            var condPacket = session.Character.GenerateCond();
            var statPacket = session.Character.GenerateStat();

            await mapInstance.SendPacketAsync(cModePacket);
            await session.SendPacketAsync(levPacket);
            await mapInstance.SendPacketAsync(effPacket);
            await mapInstance.SendPacketAsync(new GuriPacket
            {
                Type = GuriPacketType.Dance,
                Value = 1,
                EntityId = characterId2
            });
            await session.SendPacketAsync(spPointPacket);
            await session.SendPacketAsync(condPacket);
            await session.SendPacketAsync(statPacket);
            await session.SendPacketAsync(session.Character.GenerateSc(battleStatsProvider));
        }

        public async Task ChangeVehicleAsync(ClientSession session, ItemDto vehicle, ItemDto look)
        {
            var character = session.Character;
            character.IsVehicled = true;
            character.VehicleSpeed = vehicle.Speed;
            character.VehicleVNum = vehicle.VNum;

            // cond carries Speed, not VehicleSpeed, and Speed is written once at login from
            // the class table. Without this line the mount changes the sprite and nothing
            // else: the player rides at walking pace, and no packet or log says otherwise.
            character.Speed = speedCalculationService.CalculateSpeed(character);
            character.MorphUpgrade = 0;
            character.MorphDesign = 0;
            character.Morph = look.SecondMorph == 0
                ? (short)((short)character.Gender + look.Morph)
                : character.Gender == GenderType.Male
                    ? look.Morph
                    : look.SecondMorph;

            var mapInstance = character.MapInstance;
            var effPacket = session.Character.GenerateEff(196);
            var cModePacket = session.Character.GenerateCMode();
            var condPacket = session.Character.GenerateCond();

            await mapInstance.SendPacketAsync(effPacket);
            await mapInstance.SendPacketAsync(cModePacket);
            await session.SendPacketAsync(condPacket);
        }

        public async Task RemoveVehicleAsync(ClientSession session)
        {
            var character = session.Character;
            if (character.UseSp)
            {
                var sp = character.InventoryService.LoadBySlotAndType((byte)EquipmentType.Sp, NoscorePocketType.Wear);
                if (sp != null)
                {
                    character.Morph = sp.ItemInstance.Item.Morph;
                    character.MorphDesign = (byte)sp.ItemInstance.Design;
                    character.MorphUpgrade = (byte)sp.ItemInstance.Upgrade;
                }
                else
                {
                    logger.LogError(logLanguage[LogLanguageKey.USE_SP_WITHOUT_SP_ERROR]);
                }
            }
            else
            {
                character.Morph = 0;
            }

            character.IsVehicled = false;
            character.VehicleSpeed = 0;
            character.VehicleVNum = 0;
            foreach (var boost in buffService.GetActiveBuffs(character)
                         .Where(b => cardCatalog.IsGrantedBy(b.CardId, ItemEffectType.SpeedBooster)).ToList())
            {
                await buffService.RemoveAsync(character, boost.CardId);
            }
            character.Speed = speedCalculationService.CalculateSpeed(character);

            var mapInstance = character.MapInstance;
            var condPacket = session.Character.GenerateCond();
            var cModePacket = session.Character.GenerateCMode();

            await session.SendPacketAsync(condPacket);
            await mapInstance.SendPacketAsync(cModePacket);
        }
    }
}
