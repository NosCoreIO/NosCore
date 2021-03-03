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
using NosCore.Core;
using NosCore.Core.Encryption;
using NosCore.Core.I18N;
using NosCore.Core.Networking;
using NosCore.Dao.Interfaces;
using NosCore.Data.CommandPackets;
using NosCore.Data.Dto;
using NosCore.Data.Enumerations.Character;
using NosCore.Data.Enumerations.I18N;
using NosCore.GameObject;
using NosCore.GameObject.Networking.ClientSession;
using NosCore.GameObject.Services.ItemGenerationService.Item;
using NosCore.Packets.Enumerations;
using NosCore.Packets.ServerPackets.CharacterSelectionScreen;
using NosCore.Packets.ServerPackets.UI;
using Serilog;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.Options;
using NosCore.Core.Configuration;
using NosCore.GameObject.HubClients.ChannelHubClient;

namespace NosCore.PacketHandlers.CharacterScreen
{
    public class EntryPointPacketHandler : PacketHandler<EntryPointPacket>, IWorldPacketHandler
    {
        private readonly IDao<AccountDto, long> _accountDao;
        private readonly IDao<CharacterDto, long> _characterDao;
        private readonly ILogger _logger;
        private readonly IDao<MateDto, long> _mateDao;
        private readonly IChannelHubClient _channelHubClient;
        private readonly IOptions<WorldConfiguration> _configuration;

        public EntryPointPacketHandler(IDao<CharacterDto, long> characterDao,
            IDao<AccountDto, long> accountDao,
            IDao<MateDto, long> mateDao, ILogger logger, IChannelHubClient channelHubClient, IOptions<WorldConfiguration> configuration)
        {
            _characterDao = characterDao;
            _accountDao = accountDao;
            _mateDao = mateDao;
            _logger = logger;
            _channelHubClient = channelHubClient;
            _configuration = configuration;
        }

        public static async Task VerifyConnectionAsync(ClientSession clientSession, ILogger _logger, IDao<AccountDto, long> accountDao, string accountName, int sessionId, IChannelHubClient channelHubClient)
        {
            var alreadyConnnected = (await channelHubClient.GetConnectedAccountsAsync()).Any(s => s.Name == accountName);
            if (alreadyConnnected)
            {
                _logger.Error(LogLanguage.Instance.GetMessageFromKey(LogLanguageKey.ALREADY_CONNECTED), new
                {
                    accountName
                });
                await clientSession.DisconnectAsync().ConfigureAwait(false);
                return;
            }

            var account = await accountDao.FirstOrDefaultAsync(s => s.Name == accountName).ConfigureAwait(false);

            if (account == null)
            {
                _logger.Error(LogLanguage.Instance.GetMessageFromKey(LogLanguageKey.INVALID_ACCOUNT), new
                {
                    accountName
                });
                await clientSession.DisconnectAsync().ConfigureAwait(false);
                return;
            }

            var awaitingConnection = await channelHubClient
                    .GetAwaitingConnectionAsync(accountName, "thisisgfmode", sessionId)
                    .ConfigureAwait(false) != null;

            if (!awaitingConnection)
            {
                _logger.Error(LogLanguage.Instance.GetMessageFromKey(LogLanguageKey.INVALID_PASSWORD), new
                {
                    accountName
                });
                await clientSession.DisconnectAsync().ConfigureAwait(false);
                return;
            }

            var sessionMapping = SessionFactory.Instance.Sessions
                .FirstOrDefault(s => s.Value.SessionId == clientSession.SessionId);
            if (!sessionMapping.Equals(default(KeyValuePair<string, RegionTypeMapping>)))
            {
                sessionMapping.Value.RegionType = account.Language;
            }
            clientSession.InitializeAccount(account);
            //todo Send Account Connected
        }

        public override async Task ExecuteAsync(EntryPointPacket packet, ClientSession clientSession)
        {
            if (clientSession.Account == null!) // we bypass this when create new char
            {
                await VerifyConnectionAsync(clientSession, _logger,
                    _accountDao, packet.Name,
                    clientSession.SessionId, _channelHubClient);
                if (clientSession.Account == null!)
                {
                    return;
                }

                clientSession.MfaValidated = true;
                _logger.Information(LogLanguage.Instance.GetMessageFromKey(LogLanguageKey.ACCOUNT_ARRIVED),
                    clientSession.Account!.Name);
                if (!clientSession.MfaValidated && clientSession.Account.MfaSecret != null)
                {
                    await clientSession.SendPacketAsync(new GuriPacket
                    {
                        Type = GuriPacketType.Effect,
                        Argument = 3,
                        EntityId = 0
                    }).ConfigureAwait(false);
                    return;
                }
            }

            var characters = _characterDao.Where(s =>
                (s.AccountId == clientSession.Account!.AccountId) && (s.State == CharacterState.Active) && s.ServerId == _configuration.Value.ServerId);

            // load characterlist packet for each character in Character
            await clientSession.SendPacketAsync(new ClistStartPacket { Type = 0 }).ConfigureAwait(false);
            foreach (var character in characters!.Select(characterDto => characterDto.Adapt<Character>()))
            {
                var equipment = new WearableInstance?[16];
                /* IEnumerable<ItemInstanceDTO> inventory = _iteminstanceDAO.Where(s => s.CharacterId == character.CharacterId && s.Type == (byte)InventoryType.Wear);


                 foreach (ItemInstanceDTO equipmentEntry in inventory)
                 {
                     // explicit load of iteminstance
                     WearableInstance currentInstance = equipmentEntry as WearableInstance;
                     equipment[(short)currentInstance.Item.EquipmentSlot] = currentInstance;

                 }
                    */
                var petlist = new List<short?>();
                var mates = _mateDao.Where(s => s.CharacterId == character.CharacterId)!
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
                await clientSession.SendPacketAsync(new ClistPacket
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
                    Design = equipment[(byte)EquipmentType.Hat]?.Item?.IsColored ?? false
                        ? equipment[(byte)EquipmentType.Hat]?.Design ?? 0 : 0,
                    Rename = character.ShouldRename
                }).ConfigureAwait(false);
            }

            await clientSession.SendPacketAsync(new ClistEndPacket()).ConfigureAwait(false);
        }
    }
}