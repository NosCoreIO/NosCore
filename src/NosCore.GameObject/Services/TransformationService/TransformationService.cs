//  __  _  __    __   ___ __  ___ ___
// |  \| |/__\ /' _/ / _//__\| _ \ __|
// | | ' | \/ |`._`.| \_| \/ | v / _|
// |_|\__|\__/ |___/ \__/\__/|_|_\___|
//

using Microsoft.Extensions.Options;
using NodaTime;
using NosCore.Algorithm.ExperienceService;
using NosCore.Algorithm.HeroExperienceService;
using NosCore.Algorithm.JobExperienceService;
using NosCore.Core.Configuration;
using NosCore.Data.Enumerations;
using NosCore.Data.Enumerations.I18N;
using NosCore.GameObject.Ecs.Extensions;
using NosCore.GameObject.Networking.ClientSession;
using NosCore.GameObject.Services.ItemGenerationService.Item;
using NosCore.Networking;
using NosCore.Packets.Enumerations;
using NosCore.Packets.ServerPackets.Chats;
using NosCore.Packets.ServerPackets.Specialists;
using NosCore.Packets.ServerPackets.UI;
using NosCore.Shared.Enumerations;
using NosCore.Shared.I18N;
using Serilog;
using System;
using System.Reactive.Linq;
using System.Threading.Tasks;

namespace NosCore.GameObject.Services.TransformationService
{
    public class TransformationService(IClock clock, IExperienceService experienceService,
            IJobExperienceService jobExperienceService, IHeroExperienceService heroExperienceService, ILogger logger,
            ILogLanguageLocalizer<LogLanguageKey> logLanguage, IOptions<WorldConfiguration> worldConfiguration)
        : ITransformationService
    {
        public async Task RemoveSpAsync(ClientSession session)
        {
            var character = session.Character;
            character.UseSp = false;
            character.Morph = 0;
            character.MorphUpgrade = 0;
            character.MorphDesign = 0;
            character.SpCooldown = 30;

            var characterId = character.CharacterId;
            var spCooldown = character.SpCooldown;
            var mapInstance = character.MapInstance;
            var condPacket = session.Character.GenerateCond();
            var levPacket = session.Character.GenerateLev(experienceService, jobExperienceService, heroExperienceService);
            var cModePacket = session.Character.GenerateCMode();
            var statPacket = session.Character.GenerateStat();

            await session.SendPacketAsync(condPacket);
            await session.SendPacketAsync(levPacket);
            await session.SendPacketAsync(new SayiPacket
            {
                VisualType = VisualType.Player,
                VisualId = characterId,
                Type = SayColorType.Red,
                Message = Game18NConstString.DurationOfSideEffect,
                ArgumentType = 4,
                Game18NArguments = { spCooldown }
            });
            await session.SendPacketAsync(new SdPacket { Cooldown = spCooldown });
            await mapInstance.SendPacketAsync(cModePacket);
            await mapInstance.SendPacketAsync(new GuriPacket
            {
                Type = GuriPacketType.Dance,
                Value = 1,
                EntityId = characterId
            });
            await session.SendPacketAsync(statPacket);

            async Task CoolDown()
            {
                await session.SendPacketAsync(new SayiPacket
                {
                    VisualType = VisualType.Player,
                    VisualId = characterId,
                    Type = SayColorType.Red,
                    Message = Game18NConstString.TransformationSideEffectGone
                });
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
                logger.Error(logLanguage[LogLanguageKey.USE_SP_WITHOUT_SP_ERROR]);
                return;
            }

            if (character.ReputIconValue < sp.Item.ReputationMinimum)
            {
                var characterId = character.CharacterId;
                await session.SendPacketAsync(new SayiPacket
                {
                    VisualType = VisualType.Player,
                    VisualId = characterId,
                    Type = SayColorType.Yellow,
                    Message = Game18NConstString.CanNotBeWornReputationLow
                });
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

            var characterId2 = character.CharacterId;
            var mapInstance = character.MapInstance;
            var cModePacket = session.Character.GenerateCMode();
            var levPacket = session.Character.GenerateLev(experienceService, jobExperienceService, heroExperienceService);
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
        }

        public async Task ChangeVehicleAsync(ClientSession session, Item item)
        {
            var character = session.Character;
            character.IsVehicled = true;
            character.VehicleSpeed = item.Speed;
            character.MorphUpgrade = 0;
            character.MorphDesign = 0;
            character.Morph = item.SecondMorph == 0
                ? (short)((short)character.Gender + item.Morph)
                : character.Gender == GenderType.Male
                    ? item.Morph
                    : item.SecondMorph;

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
                    logger.Error(logLanguage[LogLanguageKey.USE_SP_WITHOUT_SP_ERROR]);
                }
            }
            else
            {
                character.Morph = 0;
            }

            character.IsVehicled = false;
            character.VehicleSpeed = 0;

            var mapInstance = character.MapInstance;
            var condPacket = session.Character.GenerateCond();
            var cModePacket = session.Character.GenerateCMode();

            await session.SendPacketAsync(condPacket);
            await mapInstance.SendPacketAsync(cModePacket);
        }
    }
}
