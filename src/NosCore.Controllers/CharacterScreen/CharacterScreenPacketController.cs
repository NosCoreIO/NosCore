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
using JetBrains.Annotations;
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
using NosCore.GameObject.Networking;
using NosCore.GameObject.Providers.ItemProvider;
using NosCore.GameObject.Providers.ItemProvider.Item;
using NosCore.GameObject.Providers.MapInstanceProvider;
using Serilog;
using Character = NosCore.GameObject.Character;
using ChickenAPI.Packets.Enumerations;
using ChickenAPI.Packets.ClientPackets.CharacterSelectionScreen;
using ChickenAPI.Packets.ServerPackets.CharacterSelectionScreen;
using ChickenAPI.Packets.ServerPackets.UI;
using ChickenAPI.Packets.ClientPackets.Login;
using NosCore.GameObject;
using NosCore.GameObject.Networking.ClientSession;

namespace NosCore.Controllers
{
    public class CharacterDeletePacketHandler : PacketHandler<CharacterDeletePacket>, IWorldPacketHandler
    {
        private readonly IGenericDao<CharacterDto> _characterDao;
        private readonly IGenericDao<AccountDto> _accountDao;

        public CharacterDeletePacketHandler(IGenericDao<CharacterDto> characterDao, IGenericDao<AccountDto> accountDao)
        {
            _characterDao = characterDao;
            _accountDao = accountDao;
        }

        public override void Execute(CharacterDeletePacket packet, ClientSession session)
        {
            if (session.HasCurrentMapInstance)
            {
                return;
            }

            var account = _accountDao
                .FirstOrDefault(s => s.AccountId.Equals(session.Account.AccountId));
            if (account == null)
            {
                return;
            }

            if (account.Password.ToLower() == packet.Password.ToSha512())
            {
                var character = _characterDao.FirstOrDefault(s =>
                    s.AccountId == account.AccountId && s.Slot == packet.Slot
                    && s.State == CharacterState.Active);
                if (character == null)
                {
                    return;
                }

                character.State = CharacterState.Inactive;
                _characterDao.InsertOrUpdate(ref character);

                LoadCharacters(null);
            }
            else
            {
                session.SendPacket(new InfoPacket
                {
                    Message = session.GetMessageFromKey(LanguageKey.BAD_PASSWORD)
                });
            }
        }


    }
}