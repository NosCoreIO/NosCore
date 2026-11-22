//  __  _  __    __   ___ __  ___ ___
// |  \| |/__\ /' _/ / _//__\| _ \ __|
// | | ' | \/ |`._`.| \_| \/ | v / _|
// |_|\__|\__/ |___/ \__/\__/|_|_\___|
//

using NosCore.GameObject.Services.GroupService;
using NosCore.GameObject.Services.InventoryService;
using NosCore.GameObject.Services.EquipmentService;
using Microsoft.Extensions.Options;
using NodaTime;
using NosCore.Algorithm.ExperienceService;
using NosCore.Algorithm.HeroExperienceService;
using NosCore.Algorithm.SpExperienceService;
using NosCore.Algorithm.SkillPointService;
using NosCore.Algorithm.JobExperienceService;
using NosCore.Core.Configuration;
using NosCore.Data.Enumerations;
using NosCore.Data.Enumerations.I18N;
using NosCore.GameObject.Ecs.Extensions;
using NosCore.Data.StaticEntities;
using NosCore.GameObject.Networking.ClientSession;
using NosCore.GameObject.Services.ItemGenerationService.Item;
using NosCore.GameObject.Services.MateService;
using NosCore.GameObject.Services.SpecialistCardService;
using NosCore.Networking;
using NosCore.Networking.SessionGroup.ChannelMatcher;
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
using NosCore.Packets.ServerPackets.Entities;

namespace NosCore.GameObject.Services.TransformationService
{
    public class TransformationService(IClock clock, IExperienceService experienceService,
            IJobExperienceService jobExperienceService, IHeroExperienceService heroExperienceService, ISpExperienceService spExperienceService, ISkillPointService skillPointService, ILogger<TransformationService> logger,
            ILogLanguageLocalizer<LogLanguageKey> logLanguage, IOptions<WorldConfiguration> worldConfiguration,
            SpeedCalculationService.ISpeedCalculationService speedCalculationService,
            SkillService.ISkillService skillService,
            BattleService.IBattleStatsProvider battleStatsProvider,
            BattleService.IBuffService buffService,
            BattleService.ICardCatalog cardCatalog,
            ISpecialistWingsService specialistWingsService)
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
            await specialistWingsService.RefreshAsync(character);
            character.SpCooldown = 30;
            character.Speed = speedCalculationService.CalculateSpeed(character);

            var characterId = character.CharacterId;
            var spCooldown = character.SpCooldown;
            var mapInstance = character.MapInstance;
            var condPacket = session.Character.GenerateCond();
            var levPacket = session.Character.GenerateLev(experienceService, jobExperienceService, heroExperienceService, spExperienceService, skillPointService);
            var cModePacket = session.Character.GenerateCMode();
            var statPacket = session.Character.GenerateStat();
            var scPacket = session.Character.GenerateSc(battleStatsProvider);

            await session.SendPacketAsync(condPacket);
            await session.SendPacketAsync(levPacket);
            await session.SendPacketAsync(scPacket);
            await session.SendPacketAsync(character.GenerateSayi(SayColorType.Red, Game18NConstString.DurationOfSideEffect, 4, spCooldown));
            await session.SendPacketAsync(new SdPacket { Cooldown = spCooldown });
            await mapInstance.SendPacketAsync(cModePacket);
            await mapInstance.SendPacketAsync(new GuriDancePacket
            {
                VisualType = VisualType.Player,
                VisualId = characterId
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
            if (character.InventoryService.LoadBySlotAndType(EquipmentType.Sp, NoscorePocketType.Wear)?.ItemInstance is
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
            if (inventoryService.LoadBySlotAndType(EquipmentType.Fairy, NoscorePocketType.Wear)?.ItemInstance is
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
            await buffService.ClearAsync(character);
            character.LastSp = clock.GetCurrentInstant();
            character.UseSp = true;
            character.InMonsterForm = false;
            character.Morph = sp.ShownMorph;
            character.MorphUpgrade = (byte)sp.Upgrade;
            character.MorphDesign = (byte)sp.Design;

            await specialistWingsService.RefreshAsync(character);
            character.Speed = speedCalculationService.CalculateSpeed(character);

            var mapInstance = character.MapInstance;
            await session.SendPacketAsync(character.GenerateCond());
            await session.SendPacketAsync(character.GenerateSc(battleStatsProvider));
            await session.SendPacketAsync(character.GenerateStat());
            await mapInstance.SendPacketAsync(character.GenerateCMode());
            await skillService.LoadSpecialistSkillsAsync(character, sp.Item.Morph, sp.SpLevel);
            await session.SendPacketAsync(character.GenerateSpPoint(worldConfiguration));
            await session.SendPacketAsync(character.GenerateLev(experienceService, jobExperienceService, heroExperienceService, spExperienceService, skillPointService));
            await mapInstance.SendPacketAsync(character.GenerateEq(), new EveryoneBut(session.ConnectedChannel.Id));
            await mapInstance.SendPacketAsync(character.GenerateEff(196));
            await mapInstance.SendPacketAsync(new GuriPacket
            {
                Type = GuriPacketType.CharacterAnimation,
                Argument = (uint)VisualType.Player,
                EntityId = character.CharacterId,
                Value = 0,
                SecondValue = 0
            });
        }

        public async Task ChangeVehicleAsync(ClientSession session, ItemDto vehicle, ItemDto look)
        {
            var character = session.Character;
            character.IsVehicled = true;
            character.InMonsterForm = false;
            character.VehicleSpeed = vehicle.Speed;
            character.VehicleVNum = vehicle.VNum;
            if (cardCatalog.GetItemCard(vehicle.VNum) is { } ridingCard)
            {
                await buffService.ApplyAsync(character, ridingCard, cardCatalog.GetCardBCards(ridingCard.CardId), character);
            }

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

            // The team waits off the map while its owner rides.
            foreach (var mate in character.Mates.Values.Where(m => m.IsTeamMember))
            {
                await mate.DespawnAsync(mapInstance);
            }
        }

        public async Task ChangeMonsterFormAsync(ClientSession session, short morph)
        {
            var character = session.Character;
            character.InMonsterForm = true;
            character.Morph = morph;
            character.MorphUpgrade = 0;
            character.MorphDesign = 0;
            await character.MapInstance.SendPacketAsync(character.GenerateCMode());
        }

        public async Task RemoveMonsterFormAsync(ClientSession session)
        {
            var character = session.Character;
            character.InMonsterForm = false;
            character.Morph = 0;
            await character.MapInstance.SendPacketAsync(character.GenerateCMode());
        }

        public async Task RemoveVehicleAsync(ClientSession session)
        {
            var character = session.Character;
            if (character.UseSp)
            {
                if (character.InventoryService.LoadBySlotAndType(EquipmentType.Sp, NoscorePocketType.Wear)?.ItemInstance is SpecialistInstance sp)
                {
                    character.Morph = sp.ShownMorph;
                    character.MorphDesign = (byte)sp.Design;
                    character.MorphUpgrade = (byte)sp.Upgrade;
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

            // A boost can swap the riding card for another of its group, which would otherwise outlive the ride.
            if (cardCatalog.GetItemCard(character.VehicleVNum) is { } ridingCard)
            {
                foreach (var buff in buffService.GetActiveBuffs(character)
                             .Where(b => b.CardId == ridingCard.CardId
                                 || (ridingCard.GroupId != 0 && cardCatalog.GetCard(b.CardId)?.GroupId == ridingCard.GroupId))
                             .ToList())
                {
                    await buffService.RemoveAsync(character, buff.CardId);
                }
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

            var team = character.Mates.Values.Where(m => m.IsTeamMember && m.RevivesAt == null && m.Entity == null).ToList();
            foreach (var (mate, x, y) in MatePlacement.Arrange(character.PositionX, character.PositionY, mapInstance.Map, team))
            {
                await mate.SpawnAsync(character, mapInstance, x, y);
            }

            // Without a fresh party list the client keeps the returned mates marked as away.
            await session.SendPacketAsync(character.GeneratePinit());
            if (character.Group is { IsEmpty: false } group)
            {
                await session.SendPacketsAsync(group.GeneratePst());
            }

            await session.SendPacketsAsync(team.Select(m => m.GenerateCond()));
            await session.SendPacketsAsync(team.Select(m => m.GeneratePst()));
        }
    }
}
