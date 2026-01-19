//  __  _  __    __   ___ __  ___ ___
// |  \| |/__\ /' _/ / _//__\| _ \ __|
// | | ' | \/ |`._`.| \_| \/ | v / _|
// |_|\__|\__/ |___/ \__/\__/|_|_\___|
//

using Mapster;
using Microsoft.Extensions.Options;
using NosCore.Core.Configuration;
using NosCore.Dao.Interfaces;
using NosCore.Data.Dto;
using NosCore.Data.Enumerations.Character;
using NosCore.Data.Enumerations.I18N;
using NosCore.Data.StaticEntities;
using NosCore.Data.WebApi;
using NosCore.GameObject.ComponentEntities.Extensions;
using NosCore.GameObject.Infastructure;
using NosCore.GameObject.InterChannelCommunication.Hubs.PubSub;
using NosCore.GameObject.Networking.ClientSession;
using NosCore.GameObject.Services.InventoryService;
using NosCore.GameObject.Services.ItemGenerationService;
using NosCore.GameObject.Services.MapInstanceAccessService;
using NosCore.GameObject.Services.QuestService;
using NosCore.Packets.ClientPackets.CharacterSelectionScreen;
using NosCore.Packets.ServerPackets.CharacterSelectionScreen;
using NosCore.Shared.I18N;
using Serilog;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace NosCore.PacketHandlers.CharacterScreen
{
    public class SelectPacketHandler(IDao<CharacterDto, long> characterDao, ILogger logger,
            IItemGenerationService itemProvider,
            IMapInstanceAccessorService mapInstanceAccessorService, IDao<IItemInstanceDto?, Guid> itemInstanceDao,
            IDao<InventoryItemInstanceDto, Guid> inventoryItemInstanceDao, IDao<StaticBonusDto, long> staticBonusDao,
            IDao<QuicklistEntryDto, Guid> quickListEntriesDao, IDao<TitleDto, Guid> titleDao,
            IDao<CharacterQuestDto, Guid> characterQuestDao,
            IDao<ScriptDto, Guid> scriptDao, List<QuestDto> quests, List<QuestObjectiveDto> questObjectives,
            IOptions<WorldConfiguration> configuration, ILogLanguageLocalizer<LogLanguageKey> logLanguage, IPubSubHub pubSubHub)
        : PacketHandler<SelectPacket>, IWorldPacketHandler
    {
        public override async Task ExecuteAsync(SelectPacket packet, ClientSession clientSession)
        {
            try
            {
                var characterDto = await
                    characterDao.FirstOrDefaultAsync(s =>
                        (s.AccountId == clientSession.Account.AccountId) && (s.Slot == packet.Slot)
                        && (s.State == CharacterState.Active) && s.ServerId == configuration.Value.ServerId);
                if (characterDto == null)
                {
                    logger.Error(logLanguage[LogLanguageKey.CHARACTER_SLOT_EMPTY], new
                    {
                        clientSession.Account.AccountId,
                        packet.Slot
                    });
                    return;
                }

                var character = characterDto.Adapt<GameObject.ComponentEntities.Entities.Character>();
                character.InitializeGroup();
                await pubSubHub.SubscribeAsync(new Subscriber
                {
                    Id = clientSession.SessionId,
                    Name = clientSession.Account.Name,
                    Language = clientSession.Account.Language,
                    ConnectedCharacter = new Data.WebApi.Character
                    {
                        Name = character.Name,
                        Id = character.CharacterId,
                        FriendRequestBlocked = character.FriendRequestBlocked
                    }
                });
                character.MapInstance = mapInstanceAccessorService.GetBaseMapById(character.MapId)!;
                character.PositionX = character.MapX;
                character.PositionY = character.MapY;
                character.Direction = 2;
                character.Script = character.CurrentScriptId != null ? await scriptDao.FirstOrDefaultAsync(s => s.Id == character.CurrentScriptId) : null;
                character.Group!.JoinGroup(character);

                var inventories = inventoryItemInstanceDao
                    .Where(s => s.CharacterId == character.CharacterId)
                    ?.ToList() ?? new List<InventoryItemInstanceDto>();
                var ids = inventories.Select(o => o.ItemInstanceId).ToList();
                var items = itemInstanceDao.Where(s => ids.Contains(s!.Id))?.ToList() ?? new List<IItemInstanceDto?>();
                inventories.ForEach(k => character.InventoryService[k.ItemInstanceId] =
                    InventoryItemInstance.Create(itemProvider.Convert(items.First(s => s!.Id == k.ItemInstanceId)!),
                        character.CharacterId, k));
                await clientSession.SetCharacterAsync(character);

#pragma warning disable CS0618
                await clientSession.SendPacketsAsync(clientSession.Character.GenerateInv(logger, logLanguage));
#pragma warning restore CS0618
                await clientSession.SendPacketAsync(clientSession.Character.GenerateMlobjlst());
                if (clientSession.Character.Hp > clientSession.Character.MaxHp)
                {
                    clientSession.Character.Hp = clientSession.Character.MaxHp;
                }

                if (clientSession.Character.Mp > clientSession.Character.MaxMp)
                {
                    clientSession.Character.Mp = clientSession.Character.MaxMp;
                }

                var daoQuests = characterQuestDao
                    .Where(s => s.CharacterId == clientSession.Character.CharacterId) ?? new List<CharacterQuestDto>();
                clientSession.Character.Quests = new ConcurrentDictionary<Guid, CharacterQuest>(daoQuests.ToDictionary(x => x.Id, x =>
                    {
                        var charquest = x.Adapt<CharacterQuest>();
                        charquest.Quest = quests.First(s => s.QuestId == charquest.QuestId).Adapt<GameObject.Services.QuestService.Quest>();
                        charquest.Quest.QuestObjectives =
                            questObjectives.Where(s => s.QuestId == charquest.QuestId).ToList();
                        return charquest;
                    }));
                clientSession.Character.QuicklistEntries = quickListEntriesDao
                    .Where(s => s.CharacterId == clientSession.Character.CharacterId)?.ToList() ?? new List<QuicklistEntryDto>();
                clientSession.Character.StaticBonusList = staticBonusDao
                    .Where(s => s.CharacterId == clientSession.Character.CharacterId)?.ToList() ?? new List<StaticBonusDto>();
                clientSession.Character.Titles = titleDao
                    .Where(s => s.CharacterId == clientSession.Character.CharacterId)?.ToList() ?? new List<TitleDto>();
                await clientSession.SendPacketAsync(new OkPacket());
            }
            catch (Exception ex)
            {
                logger.Error(logLanguage[LogLanguageKey.CHARACTER_SELECTION_FAILED], ex, new
                {
                    clientSession.Account.AccountId,
                    packet.Slot
                });
            }
        }
    }
}
