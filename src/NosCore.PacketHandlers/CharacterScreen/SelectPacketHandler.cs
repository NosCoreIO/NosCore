//  __  _  __    __   ___ __  ___ ___
// |  \| |/__\ /' _/ / _//__\| _ \ __|
// | | ' | \/ |`._`.| \_| \/ | v / _|
// |_|\__|\__/ |___/ \__/\__/|_|_\___|
// 
// Copyright (C) 2019 - NosCore
// 
// NosCore is a free software: you can redistribute it and/or modify
// it under the terms of the GNU General Public License as published by
// the Free Software Foundation, either version 3 of the License, or any later version.
// 
// This program is distributed in the hope that it will be useful,
// but WITHOUT ANY WARRANTY; without even the implied warranty of
// MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
// GNU General Public License for more details.
// 
// You should have received a copy of the GNU General Public License
// along with this program.  If not, see <http://www.gnu.org/licenses/>.

using Mapster;
using NosCore.Dao.Interfaces;
using NosCore.Data.Dto;
using NosCore.Data.Enumerations.Character;
using NosCore.Data.Enumerations.I18N;
using NosCore.Data.StaticEntities;
using NosCore.GameObject;
using NosCore.GameObject.Ecs.Systems;
using NosCore.GameObject.Services.CharacterService;
using NosCore.GameObject.Services.InventoryService;
using NosCore.GameObject.Services.ItemGenerationService;
using NosCore.GameObject.Services.MapInstanceAccessService;
using NosCore.GameObject.Services.QuestService;
using NosCore.Networking.SessionGroup;
using NosCore.Packets.ClientPackets.CharacterSelectionScreen;
using NosCore.Packets.ServerPackets.CharacterSelectionScreen;
using Serilog;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.Options;
using NosCore.Core.Configuration;
using NosCore.Shared.I18N;
using NosCore.Data.WebApi;
using NosCore.GameObject.InterChannelCommunication.Hubs.PubSub;
using NosCore.GameObject.Networking;

namespace NosCore.PacketHandlers.CharacterScreen
{
    public class SelectPacketHandler(IDao<CharacterDto, long> characterDao, ILogger logger,
            IItemGenerationService itemProvider,
            IMapInstanceAccessorService mapInstanceAccessorService, IDao<IItemInstanceDto?, Guid> itemInstanceDao,
            IDao<InventoryItemInstanceDto, Guid> inventoryItemInstanceDao, IDao<StaticBonusDto, long> staticBonusDao,
            IDao<QuicklistEntryDto, Guid> quickListEntriesDao, IDao<TitleDto, Guid> titleDao,
            IDao<CharacterQuestDto, Guid> characterQuestDao,
            IDao<ScriptDto, Guid> scriptDao, List<QuestDto> quests, List<QuestObjectiveDto> questObjectives,
            IOptions<WorldConfiguration> configuration, ILogLanguageLocalizer<LogLanguageKey> logLanguage, IPubSubHub pubSubHub,
            ICharacterPacketSystem characterPacketSystem,
            ISessionGroupFactory sessionGroupFactory, Func<IInventoryService> inventoryServiceFactory,
            ICharacterRegistry characterRegistry)
        : PacketHandler<SelectPacket>, IWorldPacketHandler
    {
        public override async Task ExecuteAsync(SelectPacket packet, ClientSession clientSession)
        {
            try
            {
                var characterDto = await
                    characterDao.FirstOrDefaultAsync(s =>
                        (s.AccountId == clientSession.Account.AccountId) && (s.Slot == packet.Slot)
                        && (s.State == CharacterState.Active) && s.ServerId == configuration.Value.ServerId).ConfigureAwait(false);
                if (characterDto == null)
                {
                    logger.Error(logLanguage[LogLanguageKey.CHARACTER_SLOT_EMPTY], new
                    {
                        clientSession.Account.AccountId,
                        packet.Slot
                    });
                    return;
                }

                var inventoryService = inventoryServiceFactory();
                var gameState = new CharacterGameState(
                    characterDto.CharacterId,
                    clientSession.Account,
                    inventoryService,
                    sessionGroupFactory
                );

                characterRegistry.Register(characterDto.CharacterId, gameState);

                var mapInstance = mapInstanceAccessorService.GetBaseMapById(characterDto.MapId)!;
                gameState.Script = characterDto.CurrentScriptId != null ? await scriptDao.FirstOrDefaultAsync(s => s.Id == characterDto.CurrentScriptId).ConfigureAwait(false) : null;

                var inventories = inventoryItemInstanceDao
                    .Where(s => s.CharacterId == characterDto.CharacterId)
                    ?.ToList() ?? new List<InventoryItemInstanceDto>();
                var ids = inventories.Select(o => o.ItemInstanceId).ToList();
                var items = itemInstanceDao.Where(s => ids.Contains(s!.Id))?.ToList() ?? new List<IItemInstanceDto?>();
                inventories.ForEach(k => inventoryService[k.ItemInstanceId] =
                    InventoryItemInstance.Create(itemProvider.Convert(items.First(s => s!.Id == k.ItemInstanceId)!),
                        characterDto.CharacterId, k));

                await clientSession.SetPlayerAsync(gameState, characterDto, mapInstance).ConfigureAwait(false);

                var player = clientSession.Player;
                player.Group = new GameObject.Group(Data.Enumerations.Group.GroupType.Group, sessionGroupFactory);
                player.Group.JoinGroup(player);

                await pubSubHub.SubscribeAsync(new Subscriber
                {
                    Id = clientSession.SessionId,
                    Name = clientSession.Account.Name,
                    Language = clientSession.Account.Language,
                    ConnectedCharacter = new Data.WebApi.Character
                    {
                        Name = characterDto.Name,
                        Id = characterDto.CharacterId,
                        FriendRequestBlocked = characterDto.FriendRequestBlocked
                    }
                });

#pragma warning disable CS0618
                await clientSession.SendPacketsAsync(characterPacketSystem.GenerateInv(clientSession.Player, logger, logLanguage)).ConfigureAwait(false);
#pragma warning restore CS0618
                await clientSession.SendPacketAsync(characterPacketSystem.GenerateMlobjlst(clientSession.Player)).ConfigureAwait(false);
                var currentHp = clientSession.Player.Hp;
                var maxHp = clientSession.Player.MaxHp;
                if (currentHp > maxHp)
                {
                    clientSession.Player.SetHp(maxHp);
                }

                var currentMp = clientSession.Player.Mp;
                var maxMp = clientSession.Player.MaxMp;
                if (currentMp > maxMp)
                {
                    clientSession.Player.SetMp(maxMp);
                }

                var daoQuests = characterQuestDao
                    .Where(s => s.CharacterId == clientSession.Player.CharacterId) ?? new List<CharacterQuestDto>();
                gameState.Quests = new ConcurrentDictionary<Guid, CharacterQuest>(daoQuests.ToDictionary(x => x.Id, x =>
                    {
                        var charquest = x.Adapt<CharacterQuest>();
                        charquest.Quest = quests.First(s => s.QuestId == charquest.QuestId).Adapt<GameObject.Services.QuestService.Quest>();
                        charquest.Quest.QuestObjectives =
                            questObjectives.Where(s => s.QuestId == charquest.QuestId).ToList();
                        return charquest;
                    }));
                gameState.QuicklistEntries = quickListEntriesDao
                    .Where(s => s.CharacterId == clientSession.Player.CharacterId)?.ToList() ?? new List<QuicklistEntryDto>();
                gameState.StaticBonusList = staticBonusDao
                    .Where(s => s.CharacterId == clientSession.Player.CharacterId)?.ToList() ?? new List<StaticBonusDto>();
                gameState.Titles = titleDao
                    .Where(s => s.CharacterId == clientSession.Player.CharacterId)?.ToList() ?? new List<TitleDto>();
                await clientSession.SendPacketAsync(new OkPacket()).ConfigureAwait(false);
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