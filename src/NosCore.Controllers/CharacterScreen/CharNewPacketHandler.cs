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
using NosCore.Configuration;
using NosCore.Core;
using NosCore.Core.Networking;
using NosCore.Data;
using NosCore.Data.Enumerations;
using NosCore.Data.Enumerations.Account;
using NosCore.Data.WebApi;
using ChickenAPI.Packets.Enumerations;
using ChickenAPI.Packets.ServerPackets.Login;
using ChickenAPI.Packets.ClientPackets.Login;
using ChickenAPI.Packets.Interfaces;
using ChickenAPI.Packets.ServerPackets.CharacterSelectionScreen;
using Mapster;
using NosCore.Core.Encryption;
using NosCore.Core.I18N;
using NosCore.Data.AliveEntities;
using NosCore.Data.Enumerations.Character;
using NosCore.Data.Enumerations.I18N;
using NosCore.GameObject;
using NosCore.GameObject.Networking.Clientsession;
using NosCore.GameObject.Providers.ItemProvider;
using NosCore.GameObject.Providers.ItemProvider.Item;
using NosCore.GameObject.Providers.MapInstanceProvider;
using Serilog;
using Character = NosCore.GameObject.Character;
using ChickenAPI.Packets.ClientPackets.CharacterSelectionScreen;
using ChickenAPI.Packets.ServerPackets.UI;
using NosCore.GameObject.Networking.ClientSession;

namespace NosCore.Controllers
{
    public class CharNewPacketHandler : PacketHandler<CharNewPacket>, IWorldPacketHandler
    {
        private readonly IGenericDao<CharacterDto> _characterDao;
        public CharNewPacketHandler(IGenericDao<CharacterDto> characterDao)
        {
            _characterDao = characterDao;
        }

        public override void Execute(CharNewPacket packet, ClientSession session)
        {
            if (session.HasCurrentMapInstance)
            {
                return;
            }

            // TODO: Hold Account Information in Authorized object
            var accountId = session.Account.AccountId;
            var slot = packet.Slot;
            var characterName = packet.Name;
            if (_characterDao.FirstOrDefault(s =>
                s.AccountId == accountId && s.Slot == slot && s.State == CharacterState.Active) != null)
            {
                return;
            }

            var rg = new Regex(
                @"^[\u0021-\u007E\u00A1-\u00AC\u00AE-\u00FF\u4E00-\u9FA5\u0E01-\u0E3A\u0E3F-\u0E5B\u002E]*$");
            if (rg.Matches(characterName).Count == 1)
            {
                var character =
                    _characterDao.FirstOrDefault(s =>
                        s.Name == characterName && s.State == CharacterState.Active);
                if (character == null)
                {
                    var chara = new CharacterDto
                    {
                        Class = packet.IsMartialArtist ? CharacterClassType.MartialArtist
                            : CharacterClassType.Adventurer,
                        Gender = packet.Gender,
                        HairColor = packet.HairColor,
                        HairStyle = packet.HairStyle,
                        Hp = packet.IsMartialArtist ? 12965 : 221,
                        JobLevel = 1,
                        Level = (byte)(packet.IsMartialArtist ? 81 : 1),
                        MapId = 1,
                        MapX = (short)RandomFactory.Instance.RandomNumber(78, 81),
                        MapY = (short)RandomFactory.Instance.RandomNumber(114, 118),
                        Mp = packet.IsMartialArtist ? 2369 : 221,
                        MaxMateCount = 10,
                        SpPoint = 10000,
                        SpAdditionPoint = 0,
                        Name = characterName,
                        Slot = slot,
                        AccountId = accountId,
                        MinilandMessage = "Welcome",
                        State = CharacterState.Active
                    };
                    _characterDao.InsertOrUpdate(ref chara);
                    LoadCharacters(null);
                }
                else
                {
                    session.SendPacket(new InfoPacket
                    {
                        Message = session.GetMessageFromKey(LanguageKey.ALREADY_TAKEN)
                    });
                }
            }
            else
            {
                session.SendPacket(new InfoPacket
                {
                    Message = session.GetMessageFromKey(LanguageKey.INVALID_CHARNAME)
                });
            }
        }
    }
}