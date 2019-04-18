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
using ChickenAPI.Packets.ClientPackets.Login;
using ChickenAPI.Packets.Enumerations;
using ChickenAPI.Packets.ServerPackets.CharacterSelectionScreen;
using Mapster;
using NosCore.Core;
using NosCore.Core.Encryption;
using NosCore.Core.I18N;
using NosCore.Core.Networking;
using NosCore.Data;
using NosCore.Data.AliveEntities;
using NosCore.Data.Enumerations;
using NosCore.Data.Enumerations.Character;
using NosCore.Data.Enumerations.I18N;
using NosCore.Data.WebApi;
using NosCore.GameObject;
using NosCore.GameObject.Networking.ClientSession;
using NosCore.GameObject.Providers.ItemProvider.Item;
using Serilog;
using Character = NosCore.GameObject.Character;

namespace NosCore.PacketHandlers.CharacterScreen
{
    public class EntryPointPacketHandler : PacketHandler<EntryPointPacket>, IWorldPacketHandler
    {
        private readonly IAdapter _adapter;
        private readonly ILogger _logger;
        private readonly IGenericDao<CharacterDto> _characterDao;
        private readonly IGenericDao<AccountDto> _accountDao;
        private readonly IGenericDao<MateDto> _mateDao;

        public EntryPointPacketHandler(IAdapter adapter, IGenericDao<CharacterDto> characterDao, IGenericDao<AccountDto> accountDao,  IGenericDao<MateDto> mateDao, ILogger logger)
        {
            _adapter = adapter;
            _characterDao = characterDao;
            _accountDao = accountDao;
            _mateDao = mateDao;
            _logger = logger;
        }

        public override void Execute(EntryPointPacket packet, ClientSession session)
        {
            if (session.Account == null)
            {
                var servers = WebApiAccess.Instance.Get<List<ChannelInfo>>(WebApiRoute.Channel)
                    ?.Where(c => c.Type == ServerType.WorldServer).ToList();
                var name = packet.Name;
                var alreadyConnnected = false;
                foreach (var server in servers ?? new List<ChannelInfo>())
                {
                    if (WebApiAccess.Instance
                        .Get<List<ConnectedAccount>>(WebApiRoute.ConnectedAccount, server.WebApi)
                        .Any(a => a.Name == name))
                    {
                        alreadyConnnected = true;
                        break;
                    }
                }

                if (alreadyConnnected)
                {
                    session.Disconnect();
                    return;
                }

                var account = _accountDao.FirstOrDefault(s => s.Name == name);

                if (account != null)
                {
                    if (account.Password.Equals(packet.Password.ToSha512(),
                        StringComparison.OrdinalIgnoreCase))
                    {
                        var accountobject = new AccountDto
                        {
                            AccountId = account.AccountId,
                            Name = account.Name,
                            Password = account.Password.ToLower(),
                            Authority = account.Authority,
                            Language = account.Language
                        };
                        SessionFactory.Instance.Sessions.FirstOrDefault(s => s.Value.SessionId == session.SessionId)
                            .Value.RegionType = account.Language;
                        session.InitializeAccount(accountobject);
                        //Send Account Connected
                    }
                    else
                    {
                        _logger.Error(LogLanguage.Instance.GetMessageFromKey(LogLanguageKey.INVALID_PASSWORD));
                        session.Disconnect();
                        return;
                    }
                }
                else
                {
                    _logger.Error(LogLanguage.Instance.GetMessageFromKey(LogLanguageKey.INVALID_ACCOUNT));
                    session.Disconnect();
                    return;
                }
            }

            var characters = _characterDao.Where(s =>
                s.AccountId == session.Account.AccountId && s.State == CharacterState.Active);
            _logger.Information(LogLanguage.Instance.GetMessageFromKey(LogLanguageKey.ACCOUNT_ARRIVED),
                session.Account.Name);

            // load characterlist packet for each character in Character
            session.SendPacket(new ClistStartPacket { Type = 0 });
            foreach (var character in characters.Select(characterDto => _adapter.Adapt<Character>(characterDto)))
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
                session.SendPacket(new ClistPacket
                {
                    Slot = character.Slot,
                    Name = character.Name,
                    Unknown = 0,
                    Gender = (byte)character.Gender,
                    HairStyle = (byte)character.HairStyle,
                    HairColor = (byte)character.HairColor,
                    Unknown1 = 0,
                    Class = character.Class,
                    Level = character.Level,
                    HeroLevel = character.HeroLevel,
                    Equipments = new List<short?>
                    {
                        equipment[(byte) EquipmentType.Hat]?.ItemVNum ?? -1,
                        equipment[(byte) EquipmentType.Armor]?.ItemVNum ?? -1,
                        equipment[(byte) EquipmentType.WeaponSkin]?.ItemVNum ??
                        (equipment[(byte) EquipmentType.MainWeapon]?.ItemVNum ?? -1),
                        equipment[(byte) EquipmentType.SecondaryWeapon]?.ItemVNum ?? -1,
                        equipment[(byte) EquipmentType.Mask]?.ItemVNum ?? -1,
                        equipment[(byte) EquipmentType.Fairy]?.ItemVNum ?? -1,
                        equipment[(byte) EquipmentType.CostumeSuit]?.ItemVNum ?? -1,
                        equipment[(byte) EquipmentType.CostumeHat]?.ItemVNum ?? -1
                    },
                    JobLevel = character.JobLevel,
                    QuestCompletion = 1,
                    QuestPart = 1,
                    Pets = petlist,
                    Design = equipment[(byte)EquipmentType.Hat]?.Item.IsColored ?? false
                        ? equipment[(byte)EquipmentType.Hat].Design : 0,
                    Unknown3 = 0
                });
            }

            session.SendPacket(new ClistEndPacket());
        }
    }
}