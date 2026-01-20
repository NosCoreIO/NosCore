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
using NosCore.GameObject.ComponentEntities.Entities;
using NosCore.GameObject.ComponentEntities.Extensions;
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
        public async Task RemoveSpAsync(Character character)
        {
            character.UseSp = false;
            character.Morph = 0;
            character.MorphUpgrade = 0;
            character.MorphDesign = 0;
            await character.SendPacketAsync(character.GenerateCond());
            await character.SendPacketAsync(character.GenerateLev(experienceService, jobExperienceService, heroExperienceService));
            character.SpCooldown = 30;
            await character.SendPacketAsync(new SayiPacket
            {
                VisualType = VisualType.Player,
                VisualId = character.CharacterId,
                Type = SayColorType.Red,
                Message = Game18NConstString.DurationOfSideEffect,
                ArgumentType = 4,
                Game18NArguments = { character.SpCooldown }
            });
            await character.SendPacketAsync(new SdPacket { Cooldown = character.SpCooldown });
            await character.MapInstance.SendPacketAsync(character.GenerateCMode());
            await character.MapInstance.SendPacketAsync(new GuriPacket
            {
                Type = GuriPacketType.Dance,
                Value = 1,
                EntityId = character.CharacterId
            });
            await character.SendPacketAsync(character.GenerateStat());

            async Task CoolDown()
            {
                await character.SendPacketAsync(new SayiPacket
                {
                    VisualType = VisualType.Player,
                    VisualId = character.CharacterId,
                    Type = SayColorType.Red,
                    Message = Game18NConstString.TransformationSideEffectGone
                });
                await character.SendPacketAsync(new SdPacket { Cooldown = 0 });
            }

            Observable.Timer(TimeSpan.FromMilliseconds(character.SpCooldown * 1000)).Select(_ => CoolDown()).Subscribe();
        }

        public async Task ChangeSpAsync(Character character)
        {
            if (character.InventoryService.LoadBySlotAndType((byte)EquipmentType.Sp, NoscorePocketType.Wear)?.ItemInstance is
                not SpecialistInstance sp)
            {
                logger.Error(logLanguage[LogLanguageKey.USE_SP_WITHOUT_SP_ERROR]);
                return;
            }

            if ((byte)character.ReputIcon < sp.Item.ReputationMinimum)
            {
                await character.SendPacketAsync(new SayiPacket
                {
                    VisualType = VisualType.Player,
                    VisualId = character.CharacterId,
                    Type = SayColorType.Yellow,
                    Message = Game18NConstString.CanNotBeWornReputationLow
                });
                return;
            }

            if (character.InventoryService.LoadBySlotAndType((byte)EquipmentType.Fairy, NoscorePocketType.Wear)?.ItemInstance is
                    WearableInstance fairy
                && (sp.Item.Element != 0) && (fairy.Item.Element != sp.Item.Element)
                && (fairy.Item.Element != sp.Item.SecondaryElement))
            {
                await character.SendPacketAsync(new MsgiPacket
                {
                    Type = MessageType.Default,
                    Message = Game18NConstString.SpecialistAndFairyDifferentElement
                });
                return;
            }

            character.LastSp = clock.GetCurrentInstant();
            character.UseSp = true;
            character.Morph = sp.Item.Morph;
            character.MorphUpgrade = sp.Upgrade;
            character.MorphDesign = sp.Design;
            await character.MapInstance.SendPacketAsync(character.GenerateCMode());
            await character.SendPacketAsync(character.GenerateLev(experienceService, jobExperienceService, heroExperienceService));
            await character.MapInstance.SendPacketAsync(character.GenerateEff(196));
            await character.MapInstance.SendPacketAsync(new GuriPacket
            {
                Type = GuriPacketType.Dance,
                Value = 1,
                EntityId = character.CharacterId
            });
            await character.SendPacketAsync(character.GenerateSpPoint(worldConfiguration));
            await character.SendPacketAsync(character.GenerateCond());
            await character.SendPacketAsync(character.GenerateStat());
        }

        public async Task ChangeVehicleAsync(Character character, Item item)
        {
            character.IsVehicled = true;
            character.VehicleSpeed = item.Speed;
            character.MorphUpgrade = 0;
            character.MorphDesign = 0;
            character.Morph = item.SecondMorph == 0
                ? (short)((short)character.Gender + item.Morph)
                : character.Gender == GenderType.Male
                    ? item.Morph
                    : item.SecondMorph;

            await character.MapInstance.SendPacketAsync(
                character.GenerateEff(196));
            await character.MapInstance.SendPacketAsync(character
                .GenerateCMode());
            await character.SendPacketAsync(character.GenerateCond());
        }

        public async Task RemoveVehicleAsync(Character character)
        {
            if (character.UseSp)
            {
                var sp = character.InventoryService.LoadBySlotAndType((byte)EquipmentType.Sp, NoscorePocketType.Wear);
                if (sp != null)
                {
                    character.Morph = sp.ItemInstance.Item.Morph;
                    character.MorphDesign = sp.ItemInstance.Design;
                    character.MorphUpgrade = sp.ItemInstance.Upgrade;
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
            await character.SendPacketAsync(character.GenerateCond());
            await character.MapInstance.SendPacketAsync(character.GenerateCMode());
        }
    }
}

