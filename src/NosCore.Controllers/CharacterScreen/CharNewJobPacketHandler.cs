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
    public class CharNewJobPacketHandler : PacketHandler<CharNewJobPacket>, IWorldPacketHandler
    {
        private readonly IGenericDao<CharacterDto> _characterDao;
        public CharNewJobPacketHandler(IGenericDao<CharacterDto> characterDao)
        {
            _characterDao = characterDao;
        }

        public override void Execute(CharNewJobPacket packet, ClientSession session)
        {
            //TODO add a flag on Account
            if (_characterDao.FirstOrDefault(s =>
                s.Level >= 80 && s.AccountId == session.Account.AccountId && s.State == CharacterState.Active) == null)
            {
                //Needs at least a level 80 to create a martial artist
                //TODO log
                return;
            }

            if (_characterDao.FirstOrDefault(s =>
                s.AccountId == session.Account.AccountId &&
                s.Class == CharacterClassType.MartialArtist && s.State == CharacterState.Active) != null)
            {
                //If already a martial artist, can't create another
                //TODO log
                return;
            }
            //todo add cooldown for recreate 30days

            CreateCharacter(packet);
        }
    }
}