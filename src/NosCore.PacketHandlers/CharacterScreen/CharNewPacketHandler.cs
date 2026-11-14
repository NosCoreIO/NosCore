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
using NosCore.Data.Enumerations;
using NosCore.Data.Enumerations.Character;
using NosCore.Data.StaticEntities;
using NosCore.GameObject.Infastructure;
using NosCore.GameObject.Networking.ClientSession;
using NosCore.GameObject.Services.CharacterService;
using NosCore.GameObject.Services.InventoryService;
using NosCore.GameObject.Services.ItemGenerationService;
using NosCore.GameObject.Services.ItemGenerationService.Item;
using NosCore.Packets.ClientPackets.CharacterSelectionScreen;
using NosCore.Packets.Enumerations;
using NosCore.Packets.ServerPackets.CharacterSelectionScreen;
using NosCore.Packets.ServerPackets.UI;
using NosCore.Shared.Enumerations;
using NosCore.Shared.Helpers;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Data.Common;
using System.Linq;
using System.Threading.Tasks;
using NosCore.Data.Enumerations.I18N;
using NosCore.Shared.I18N;

namespace NosCore.PacketHandlers.CharacterScreen
{
    public class CharNewPacketHandler(IDao<CharacterDto, long> characterDao,
            IItemGenerationService itemBuilderService,
            IDao<QuicklistEntryDto, Guid> quicklistEntryDao, IDao<IItemInstanceDto?, Guid> itemInstanceDao,
            IDao<InventoryItemInstanceDto, Guid> inventoryItemInstanceDao, IHpService hpService, IMpService mpService,
            IOptions<WorldConfiguration> worldConfiguration, IDao<CharacterSkillDto, Guid> characterSkillDao,
            List<ItemDto> items, ILoggerFactory loggerFactory, IDaoTransactionScope daoTransactionScope,
            ILogLanguageLocalizer<LogLanguageKey> logLanguage)
        : PacketHandler<CharNewPacket>, IWorldPacketHandler
    {
        private readonly ILogger<CharNewPacketHandler> logger = loggerFactory.CreateLogger<CharNewPacketHandler>();

        private readonly WorldConfiguration _worldConfiguration = worldConfiguration.Value;

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

        private static QuicklistEntryDto[] StarterQuicklist(long characterId) =>
        [
            new() { Id = Guid.NewGuid(), CharacterId = characterId, Slot = 1, Type = 0, IconType = 2, IconVNum = 0 },
            new() { Id = Guid.NewGuid(), CharacterId = characterId, Slot = 9, Type = 1, IconType = 3, IconVNum = 1 },
            new() { Id = Guid.NewGuid(), CharacterId = characterId, Slot = 0, Type = 1, IconType = 1, IconVNum = 1 },
            new() { Id = Guid.NewGuid(), CharacterId = characterId, Slot = 8, Type = 1, IconType = 1, IconVNum = 16 },
        ];

        private async Task<bool> StoreAsync(CharacterDto character, InventoryService inventory, List<short> skills)
        {
            try
            {
                await using var transaction = daoTransactionScope.Begin();
                var stored = await characterDao.TryInsertOrUpdateAsync(character);
                if (stored == null)
                {
                    return false;
                }

                var characterId = stored.CharacterId;
                foreach (var item in inventory.Values)
                {
                    item.CharacterId = characterId;
                }

                var written = await characterSkillDao.TryInsertOrUpdateAsync(skills.Distinct().Select(skill =>
                        new CharacterSkillDto { CharacterId = characterId, SkillVNum = skill, Id = Guid.NewGuid() }).ToArray())
                    && await quicklistEntryDao.TryInsertOrUpdateAsync(StarterQuicklist(characterId))
                    && await itemInstanceDao.TryInsertOrUpdateAsync(inventory.Values.Select(s => s.ItemInstance).ToArray())
                    && await inventoryItemInstanceDao.TryInsertOrUpdateAsync(inventory.Values.ToArray());
                if (!written)
                {
                    return false;
                }

                await transaction.CommitAsync();
                return true;
            }
            catch (DbException e)
            {
                logger.LogError(e, logLanguage[LogLanguageKey.CHARACTER_CREATION_NOT_COMMITTED], character.Name);
                return false;
            }
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

            if (characterName != null && NameRules.HasOnlyAllowedCharacters(characterName))
            {
                var character = await
                    characterDao.FirstOrDefaultAsync(s =>
                        (s.Name == characterName) && (s.State == CharacterState.Active) && (s.ServerId == _worldConfiguration.ServerId));
                if (character == null)
                {
                    var @class = packet.TargetClass ?? CharacterClassType.Adventurer;
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
                    // Everything is built before anything is written, and written in one transaction:
                    // a creation that fails must not leave a character without its equipment and skills.
                    var origin = ResolveStarterOrigin(@class, _worldConfiguration.AllClassAvailableOnCreate);
                    var inventory = new InventoryService(items, worldConfiguration, loggerFactory.CreateLogger<InventoryService>());
                    try
                    {
                        foreach (var itemToAdd in ResolvePack(_worldConfiguration.BasicEquipments, @class, origin, new List<BasicEquipment>()))
                        {
                            var item = itemBuilderService.Create(itemToAdd.VNum, itemToAdd.Amount, itemToAdd.Rare, itemToAdd.Upgrade);
                            if (inventory.AddItemToPocket(InventoryItemInstance.Create(item, 0), itemToAdd.NoscorePocketType) is not { Count: > 0 })
                            {
                                throw new InvalidOperationException($"Starter item {itemToAdd.VNum} does not fit in {itemToAdd.NoscorePocketType}.");
                            }
                        }
                    }
                    catch (InvalidOperationException e)
                    {
                        logger.LogError(e, logLanguage[LogLanguageKey.STARTER_ITEMS_FAILED], @class, characterName);
                        await clientSession.SendPacketAsync(new InfoiPacket { Message = Game18NConstString.CreationFailed });
                        return;
                    }

                    // The primary and secondary attack and the capture skill, which every character
                    // has whatever its class; BasicSkills adds what the class and origin bring.
                    var skillsToAdd = new List<short> { 200, 201, 209 };
                    skillsToAdd.AddRange(ResolvePack(_worldConfiguration.BasicSkills, @class, origin, new List<short>()));

                    if (!await StoreAsync(chara, inventory, skillsToAdd))
                    {
                        logger.LogError(logLanguage[LogLanguageKey.CHARACTER_NOT_STORED], characterName);
                        await clientSession.SendPacketAsync(new InfoiPacket { Message = Game18NConstString.CreationFailed });
                        return;
                    }

                    await clientSession.SendPacketAsync(new SuccessPacket());
                    await clientSession.ShowCharacterListAsync();
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
