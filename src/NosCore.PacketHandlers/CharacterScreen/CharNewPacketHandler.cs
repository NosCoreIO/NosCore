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

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Mapster;
using Microsoft.Extensions.Options;
using Newtonsoft.Json;
using NosCore.Algorithm.HpService;
using NosCore.Algorithm.MpService;
using NosCore.Core.Configuration;
using NosCore.Packets.ClientPackets.CharacterSelectionScreen;
using NosCore.Packets.Enumerations;
using NosCore.Packets.ServerPackets.UI;
using NosCore.Dao.Interfaces;
using NosCore.Data.CommandPackets;
using NosCore.Data.Dto;
using NosCore.Data.Enumerations;
using NosCore.Data.Enumerations.Character;
using NosCore.GameObject;
using NosCore.GameObject.Networking.ClientSession;
using NosCore.GameObject.Providers.InventoryService;
using NosCore.GameObject.Providers.ItemProvider;
using NosCore.Packets.ServerPackets.CharacterSelectionScreen;
using NosCore.Shared.Enumerations;
using NosCore.Shared.Helpers;

namespace NosCore.PacketHandlers.CharacterScreen
{
    public class CharNewPacketHandler : PacketHandler<CharNewPacket>, IWorldPacketHandler
    {
        private readonly IDao<CharacterDto, long> _characterDao;
        private readonly IDao<MinilandDto, Guid> _minilandDao;
        private readonly IDao<QuicklistEntryDto, Guid> _quicklistEntryDao;
        private readonly IDao<InventoryItemInstanceDto, Guid> _inventoryItemInstanceDao;
        private readonly IDao<IItemInstanceDto?, Guid> _itemInstanceDao;
        private readonly IItemProvider _itemBuilderService;
        private readonly IHpService _hpService;
        private readonly IMpService _mpService;
        private readonly WorldConfiguration _worldConfiguration;

        public const string Nameregex =
            @"^[\u0021-\u007E\u00A1-\u00AC\u00AE-\u00FF\u4E00-\u9FA5\u0E01-\u0E3A\u0E3F-\u0E5B\u002E]*$";
        public CharNewPacketHandler(IDao<CharacterDto, long> characterDao, IDao<MinilandDto, Guid> minilandDao, IItemProvider itemBuilderService,
            IDao<QuicklistEntryDto, Guid> quicklistEntryDao, IDao<IItemInstanceDto?, Guid> itemInstanceDao, IDao<InventoryItemInstanceDto, Guid> inventoryItemInstanceDao, IHpService hpService, IMpService mpService, IOptions<WorldConfiguration> worldConfiguration)
        {
            _characterDao = characterDao;
            _minilandDao = minilandDao;
            _itemBuilderService = itemBuilderService;
            _quicklistEntryDao = quicklistEntryDao;
            _inventoryItemInstanceDao = inventoryItemInstanceDao;
            _itemInstanceDao = itemInstanceDao;
            _hpService = hpService;
            _mpService = mpService;
            _worldConfiguration = worldConfiguration.Value;
        }

        public override async Task ExecuteAsync(CharNewPacket packet, ClientSession clientSession)
        {
            // TODO: Hold Account Information in Authorized object
            var accountId = clientSession.Account.AccountId;
            var slot = packet.Slot;
            var characterName = packet.Name;
            if (await _characterDao.FirstOrDefaultAsync(s =>
                (s.AccountId == accountId) && (s.Slot == slot) && (s.State == CharacterState.Active)).ConfigureAwait(false) != null)
            {
                return;
            }

            var rg = new Regex(Nameregex);
            if (rg.Matches(characterName!).Count == 1)
            {
                var character = await
                    _characterDao.FirstOrDefaultAsync(s =>
                        (s.Name == characterName) && (s.State == CharacterState.Active)).ConfigureAwait(false);
                if (character == null)
                {
                    var level = (byte)(packet.IsMartialArtist ? 81 : 1);
                    var @class = packet.IsMartialArtist ? CharacterClassType.MartialArtist
                        : CharacterClassType.Adventurer;
                    var chara = new CharacterDto
                    {
                        Class = @class,
                        Gender = packet.Gender,
                        HairColor = packet.HairColor,
                        HairStyle = packet.HairStyle,
                        Hp = (int)_hpService.GetHp(@class, level),
                        JobLevel = 1,
                        Level = level,
                        MapId = 1,
                        MapX = (short)RandomHelper.Instance.RandomNumber(78, 81),
                        MapY = (short)RandomHelper.Instance.RandomNumber(114, 118),
                        Mp = (int)_mpService.GetMp(@class, level),
                        MaxMateCount = 10,
                        SpPoint = 10000,
                        SpAdditionPoint = 0,
                        Name = characterName,
                        Slot = slot,
                        AccountId = accountId,
                        State = CharacterState.Active
                    };
                    chara = await _characterDao.TryInsertOrUpdateAsync(chara).ConfigureAwait(false);

                    var miniland = new MinilandDto
                    {
                        MinilandId = Guid.NewGuid(),
                        State = MinilandState.Open,
                        MinilandMessage = ((short)Game18NConstString.Welcome).ToString(),
                        OwnerId = chara.CharacterId,
                        WelcomeMusicInfo = 3800
                    };
                    await _minilandDao.TryInsertOrUpdateAsync(miniland).ConfigureAwait(false);

                    var charaGo = chara.Adapt<Character>();
                    var itemsToAdd = new List<BasicEquipment>();
                    foreach (var item in _worldConfiguration.BasicEquipments)
                    {
                        switch (item.Key)
                        {
                            case nameof(CharacterClassType.Adventurer) when @class != CharacterClassType.Adventurer:
                            case nameof(CharacterClassType.Archer) when @class != CharacterClassType.Archer:
                            case nameof(CharacterClassType.Mage) when @class != CharacterClassType.Mage:
                            case nameof(CharacterClassType.MartialArtist) when @class != CharacterClassType.MartialArtist:
                            case nameof(CharacterClassType.Swordsman) when @class != CharacterClassType.Swordsman:
                                break;
                            default:
                                itemsToAdd.AddRange(_worldConfiguration.BasicEquipments[item.Key]);
                                break;
                        }
                    }

                    foreach (var itemToAdd in itemsToAdd)
                    {
                        charaGo.InventoryService.AddItemToPocket(InventoryItemInstance.Create(_itemBuilderService.Create(itemToAdd.VNum, itemToAdd.Amount), charaGo.CharacterId), itemToAdd.NoscorePocketType);
                    }


                    await _quicklistEntryDao.TryInsertOrUpdateAsync(new[] {
                        new QuicklistEntryDto
                        { 
                            Id = Guid.NewGuid(),
                            CharacterId = chara.CharacterId,
                            Type = QSetType.Default,
                            IconType = 2,
                            Slot = 1,
                            IconVNum = 0
                        },
                        new QuicklistEntryDto
                        {
                            Id = Guid.NewGuid(),
                            CharacterId = chara.CharacterId,
                            Slot = 9,
                            Type = QSetType.Set,
                            IconType = 3,
                            IconVNum = 1
                        }
                    });

                    await _itemInstanceDao.TryInsertOrUpdateAsync(charaGo.InventoryService.Values.Select(s => s.ItemInstance!).ToArray()).ConfigureAwait(false);
                    await _inventoryItemInstanceDao.TryInsertOrUpdateAsync(charaGo.InventoryService.Values.ToArray()).ConfigureAwait(false);

                    await clientSession.SendPacketAsync(new SuccessPacket()).ConfigureAwait(false);
                    await clientSession.HandlePacketsAsync(new[] { new EntryPointPacket() }).ConfigureAwait(false);
                }
                else
                {
                    await clientSession.SendPacketAsync(new InfoiPacket
                    {
                        Message = Game18NConstString.CharacterNameAlreadyTaken
                    }).ConfigureAwait(false);
                }
            }
            else
            {
                await clientSession.SendPacketAsync(new InfoiPacket
                {
                    Message = Game18NConstString.NameIsInvalid
                }).ConfigureAwait(false);
            }
        }
    }
}