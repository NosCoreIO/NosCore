//  __  _  __    __   ___ __  ___ ___
// |  \| |/__\ /' _/ / _//__\| _ \ __|
// | | ' | \/ |`._`.| \_| \/ | v / _|
// |_|\__|\__/ |___/ \__/\__/|_|_\___|
//

using Microsoft.Extensions.Options;
using NosCore.Algorithm.HpService;
using NosCore.Algorithm.MpService;
using NosCore.Core.Configuration;
using NosCore.Dao.Interfaces;
using NosCore.Data.CommandPackets;
using NosCore.Data.Dto;
using NosCore.Data.Enumerations.Character;
using NosCore.Data.StaticEntities;
using NosCore.GameObject.Infastructure;
using NosCore.GameObject.Networking.ClientSession;
using NosCore.GameObject.Services.InventoryService;
using NosCore.GameObject.Services.ItemGenerationService;
using NosCore.Packets.ClientPackets.CharacterSelectionScreen;
using NosCore.Packets.Enumerations;
using NosCore.Packets.ServerPackets.CharacterSelectionScreen;
using NosCore.Packets.ServerPackets.UI;
using NosCore.Shared.Enumerations;
using NosCore.Shared.Helpers;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace NosCore.PacketHandlers.CharacterScreen
{
    public class CharNewPacketHandler(IDao<CharacterDto, long> characterDao,
            IItemGenerationService itemBuilderService,
            IDao<QuicklistEntryDto, Guid> quicklistEntryDao, IDao<IItemInstanceDto?, Guid> itemInstanceDao,
            IDao<InventoryItemInstanceDto, Guid> inventoryItemInstanceDao, IHpService hpService, IMpService mpService,
            IOptions<WorldConfiguration> worldConfiguration, IDao<CharacterSkillDto, Guid> characterSkillDao,
            List<ItemDto> items, ILoggerFactory loggerFactory)
        : PacketHandler<CharNewPacket>, IWorldPacketHandler
    {
        private readonly WorldConfiguration _worldConfiguration = worldConfiguration.Value;

        public const string Nameregex =
            @"^[\u0021-\u007E\u00A1-\u00AC\u00AE-\u00FF\u4E00-\u9FA5\u0E01-\u0E3A\u0E3F-\u0E5B\u002E]*$";

        private static byte ResolveCreationLevel(CharacterClassType @class, bool allClassAvailable)
        {
            if (allClassAvailable && @class != CharacterClassType.Adventurer)
            {
                return 80;
            }
            return @class switch
            {
                CharacterClassType.Adventurer => 1,
                CharacterClassType.MartialArtist => 81,
                _ => 56
            };
        }

        private static StarterOrigin ResolveStarterOrigin(CharacterClassType @class, bool allClassAvailable)
        {
            if (@class == CharacterClassType.Adventurer)
            {
                return StarterOrigin.CreateAndUpgrade;
            }
            return allClassAvailable ? StarterOrigin.Create80 : StarterOrigin.Create56;
        }

        private static List<TItem> ResolvePack<TItem>(
            Dictionary<string, Dictionary<StarterOrigin, List<TItem>>> packs,
            CharacterClassType @class,
            StarterOrigin origin,
            List<TItem> fallback)
        {
            return packs.TryGetValue(@class.ToString(), out var byOrigin)
                && byOrigin.TryGetValue(origin, out var list)
                ? list
                : fallback;
        }

        public override async Task ExecuteAsync(CharNewPacket packet, ClientSession clientSession)
        {
            if (clientSession.HasSelectedCharacter)
            {
                return;
            }

            var accountId = clientSession.Account.AccountId;
            var slot = packet.Slot;
            var characterName = packet.Name;
            if (await characterDao.FirstOrDefaultAsync(s =>
                (s.AccountId == accountId) && (s.Slot == slot) && (s.State == CharacterState.Active) && (s.ServerId == _worldConfiguration.ServerId)) != null)
            {
                return;
            }

            var rg = new Regex(Nameregex);
            if (rg.Matches(characterName!).Count == 1)
            {
                var character = await
                    characterDao.FirstOrDefaultAsync(s =>
                        (s.Name == characterName) && (s.State == CharacterState.Active) && (s.ServerId == _worldConfiguration.ServerId));
                if (character == null)
                {
                    var @class = (CharacterClassType)(packet.TargetClass ?? (byte)CharacterClassType.Adventurer);
                    var level = ResolveCreationLevel(@class, _worldConfiguration.AllClassAvailableOnCreate);
                    var jobLevel = (byte)(@class == CharacterClassType.Adventurer ? 1 : 50);
                    var chara = new CharacterDto
                    {
                        Class = @class,
                        Gender = packet.Gender,
                        HairColor = packet.HairColor,
                        HairStyle = packet.HairStyle,
                        Hp = (int)hpService.GetHp(@class, level),
                        JobLevel = jobLevel,
                        Level = level,
                        MapId = 1,
                        MapX = (short)RandomHelper.Instance.RandomNumber(78, 81),
                        MapY = (short)RandomHelper.Instance.RandomNumber(114, 118),
                        Mp = (int)mpService.GetMp(@class, level),
                        MaxMateCount = 10,
                        SpPoint = 10000,
                        SpAdditionPoint = 0,
                        Name = characterName,
                        Slot = slot,
                        AccountId = accountId,
                        State = CharacterState.Active
                    };
                    chara = await characterDao.TryInsertOrUpdateAsync(chara);

                    var inventory = new InventoryService(items, worldConfiguration, loggerFactory.CreateLogger<InventoryService>());
                    var origin = ResolveStarterOrigin(@class, _worldConfiguration.AllClassAvailableOnCreate);
                    var itemsToAdd = ResolvePack(_worldConfiguration.BasicEquipments, @class, origin, new List<BasicEquipment>());

                    foreach (var itemToAdd in itemsToAdd)
                    {
                        inventory.AddItemToPocket(InventoryItemInstance.Create(itemBuilderService.Create(itemToAdd.VNum, itemToAdd.Amount, itemToAdd.Rare, itemToAdd.Upgrade), chara.CharacterId), itemToAdd.NoscorePocketType);
                    }

                    var skillsToAdd = ResolvePack(_worldConfiguration.BasicSkills, @class, origin, new List<short>());

                    foreach (var skillToAdd in skillsToAdd)
                    {
                        await characterSkillDao.TryInsertOrUpdateAsync(new CharacterSkillDto
                        { CharacterId = chara.CharacterId, SkillVNum = skillToAdd, Id = Guid.NewGuid() });
                    }

                    await quicklistEntryDao.TryInsertOrUpdateAsync(new[] {
                        new QuicklistEntryDto
                        {
                            Id = Guid.NewGuid(),
                            CharacterId = chara.CharacterId,
                            Slot = 1,
                            Type = 0,
                            IconType = 2,
                            IconVNum = 0
                        },
                        new QuicklistEntryDto
                        {
                            Id = Guid.NewGuid(),
                            CharacterId = chara.CharacterId,
                            Slot = 9,
                            Type = 1,
                            IconType = 3,
                            IconVNum = 1
                        },
                        new QuicklistEntryDto
                        {
                            Id = Guid.NewGuid(),
                            CharacterId = chara.CharacterId,
                            Slot = 0,
                            Type = 1,
                            IconType = 1,
                            IconVNum = 1
                        },
                        new QuicklistEntryDto
                        {
                            Id = Guid.NewGuid(),
                            CharacterId = chara.CharacterId,
                            Slot = 8,
                            Type = 1,
                            IconType = 1,
                            IconVNum = 16
                        },
                    });

                    await itemInstanceDao.TryInsertOrUpdateAsync(inventory.Values.Select(s => s.ItemInstance).ToArray());
                    await inventoryItemInstanceDao.TryInsertOrUpdateAsync(inventory.Values.ToArray());

                    await clientSession.SendPacketAsync(new SuccessPacket());
                    await clientSession.HandlePacketsAsync(new[] { new EntryPointPacket()
                    {
                        Name = clientSession.Account.Name,
                    } });
                }
                else
                {
                    await clientSession.SendPacketAsync(new InfoiPacket
                    {
                        Message = Game18NConstString.CharacterNameAlreadyTaken
                    });
                }
            }
            else
            {
                await clientSession.SendPacketAsync(new InfoiPacket
                {
                    Message = Game18NConstString.NameIsInvalid
                });
            }
        }
    }
}
