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
using NosCore.Packets.Enumerations;
using NosCore.Packets.ServerPackets.CharacterSelectionScreen;
using Mapster;
using MapsterMapper;
using NosCore.Core;
using NosCore.Core.Encryption;
using NosCore.Core.HttpClients.AuthHttpClient;
using NosCore.Core.HttpClients.ChannelHttpClient;
using NosCore.Core.HttpClients.ConnectedAccountHttpClient;
using NosCore.Core.I18N;
using NosCore.Core.Networking;
using NosCore.Data.CommandPackets;
using NosCore.Data.Dto;
using NosCore.Data.Enumerations;
using NosCore.Data.Enumerations.Character;
using NosCore.Data.Enumerations.I18N;
using NosCore.GameObject;
using NosCore.GameObject.Networking.ClientSession;
using NosCore.GameObject.Providers.ItemProvider.Item;
using Serilog;

namespace NosCore.PacketHandlers.CharacterScreen
{
    public class EntryPointPacketHandler : PacketHandler<EntryPointPacket>, IWorldPacketHandler
    {
        private readonly IGenericDao<AccountDto> _accountDao;
        private readonly IAuthHttpClient _authHttpClient;
        private readonly IChannelHttpClient _channelHttpClient;
        private readonly IGenericDao<CharacterDto> _characterDao;
        private readonly IConnectedAccountHttpClient _connectedAccountHttpClient;
        private readonly ILogger _logger;
        private readonly IGenericDao<MateDto> _mateDao;

        public EntryPointPacketHandler(IGenericDao<CharacterDto> characterDao,
            IGenericDao<AccountDto> accountDao,
            IGenericDao<MateDto> mateDao, ILogger logger, IAuthHttpClient authHttpClient,
            IConnectedAccountHttpClient connectedAccountHttpClient,
            IChannelHttpClient channelHttpClient)
        {
            _characterDao = characterDao;
            _accountDao = accountDao;
            _mateDao = mateDao;
            _logger = logger;
            _authHttpClient = authHttpClient;
            _connectedAccountHttpClient = connectedAccountHttpClient;
            _channelHttpClient = channelHttpClient;
        }

        public override void Execute(EntryPointPacket packet, ClientSession clientSession)
        {
            if (clientSession == null)
            {
                throw new ArgumentNullException(nameof(clientSession));
            }

            if (clientSession.Account == null)
            {
                var alreadyConnnected = false;
                var name = packet.Name;
                foreach (var channel in _channelHttpClient.GetChannels().Where(c => c.Type == ServerType.WorldServer))
                {
                    var accounts = _connectedAccountHttpClient.GetConnectedAccount(channel);
                    var target = accounts.FirstOrDefault(s => s.Name == name);

                    if (target != null)
                    {
                        alreadyConnnected = true;
                        break;
                    }
                }

                if (alreadyConnnected)
                {
                    clientSession.Disconnect();
                    return;
                }

                var account = _accountDao.FirstOrDefault(s => s.Name == name);

                if (account != null)
                {
                    if (_authHttpClient.GetAwaitingConnection(name, packet.Password, clientSession.SessionId) != null ||
                        (account.Password.Equals(packet.Password.ToSha512(), StringComparison.OrdinalIgnoreCase) &&
                            _authHttpClient.GetAwaitingConnection(name, "", clientSession.SessionId) == null))
                    {
                        var accountobject = new AccountDto
                        {
                            AccountId = account.AccountId,
                            Name = account.Name,
                            Password = account.Password.ToLower(),
                            Authority = account.Authority,
                            Language = account.Language
                        };
                        SessionFactory.Instance.Sessions
                            .FirstOrDefault(s => s.Value.SessionId == clientSession.SessionId)
                            .Value.RegionType = account.Language;
                        clientSession.InitializeAccount(accountobject);
                        //Send Account Connected
                    }
                    else
                    {
                        _logger.Error(LogLanguage.Instance.GetMessageFromKey(LogLanguageKey.INVALID_PASSWORD));
                        clientSession.Disconnect();
                        return;
                    }
                }
                else
                {
                    _logger.Error(LogLanguage.Instance.GetMessageFromKey(LogLanguageKey.INVALID_ACCOUNT));
                    clientSession.Disconnect();
                    return;
                }
            }

            var characters = _characterDao.Where(s =>
                (s.AccountId == clientSession.Account!.AccountId) && (s.State == CharacterState.Active));
            _logger.Information(LogLanguage.Instance.GetMessageFromKey(LogLanguageKey.ACCOUNT_ARRIVED),
                clientSession.Account!.Name);

            // load characterlist packet for each character in Character
            clientSession.SendPacket(new ClistStartPacket {Type = 0});
            foreach (var character in characters.Select(characterDto => characterDto.Adapt<Character>()))
            {
                var equipment = new WearableInstance[16];
                /* IEnumerable<ItemInstanceDTO> inventory = _iteminstanceDAO.Where(s => s.CharacterId == character.CharacterId && s.Type == (byte)InventoryType.Wear);


                 foreach (ItemInstanceDTO equipmentEntry in inventory)
                 {
                     // explicit load of iteminstance
                     WearableInstance currentInstance = equipmentEntry as WearableInstance;
                     equipment[(short)currentInstance.Item.EquipmentSlot] = currentInstance;

                 }
                    */
                var petlist = new List<short?>();
                var mates = _mateDao.Where(s => s.CharacterId == character.CharacterId)
                    .ToList();
                for (var i = 0; i < 26; i++)
                {
                    if (mates.Count > i)
                    {
                        petlist.Add(mates[i].Skin);
                        petlist.Add(mates[i].VNum);
                    }
                    else
                    {
                        petlist.Add(-1);
                    }
                }

                // 1 1 before long string of -1.-1 = act completion
                clientSession.SendPacket(new ClistPacket
                {
                    Slot = character.Slot,
                    Name = character.Name,
                    Unknown = 0,
                    Gender = character.Gender,
                    HairStyle = character.HairStyle,
                    HairColor = character.HairColor,
                    Unknown1 = 0,
                    Class = character.Class,
                    Level = character.Level,
                    HeroLevel = character.HeroLevel,
                    Equipments = new List<short?>
                    {
                        equipment[(byte) EquipmentType.Hat]?.ItemVNum,
                        equipment[(byte) EquipmentType.Armor]?.ItemVNum,
                        equipment[(byte) EquipmentType.WeaponSkin]?.ItemVNum ??
                        equipment[(byte) EquipmentType.MainWeapon]?.ItemVNum,
                        equipment[(byte) EquipmentType.SecondaryWeapon]?.ItemVNum,
                        equipment[(byte) EquipmentType.Mask]?.ItemVNum,
                        equipment[(byte) EquipmentType.Fairy]?.ItemVNum,
                        equipment[(byte) EquipmentType.CostumeSuit]?.ItemVNum,
                        equipment[(byte) EquipmentType.CostumeHat]?.ItemVNum
                    },
                    JobLevel = character.JobLevel,
                    QuestCompletion = 1,
                    QuestPart = 1,
                    Pets = petlist,
                    Design = equipment[(byte) EquipmentType.Hat]?.Item.IsColored ?? false
                        ? equipment[(byte) EquipmentType.Hat].Design : 0,
                    Unknown3 = 0
                });
            }

            clientSession.SendPacket(new ClistEndPacket());
        }
    }
}