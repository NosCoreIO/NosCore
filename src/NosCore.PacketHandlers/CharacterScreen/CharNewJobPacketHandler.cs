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
using NosCore.GameObject;
using NosCore.GameObject.Networking.ClientSession;
using NosCore.Packets.ClientPackets.CharacterSelectionScreen;
using NosCore.Shared.Enumerations;
using System.Threading.Tasks;
using Microsoft.Extensions.Options;
using NosCore.Core.Configuration;

namespace NosCore.PacketHandlers.CharacterScreen
{
    public class CharNewJobPacketHandler : PacketHandler<CharNewJobPacket>, IWorldPacketHandler
    {
        private readonly IDao<CharacterDto, long> _characterDao;
        private readonly IOptions<WorldConfiguration> _configuration;

        public CharNewJobPacketHandler(IDao<CharacterDto, long> characterDao, IOptions<WorldConfiguration> configuration)
        {
            _characterDao = characterDao;
            _configuration = configuration;
        }

        public override async Task ExecuteAsync(CharNewJobPacket packet, ClientSession clientSession)
        {
            //TODO add a flag on Account
            if (await _characterDao.FirstOrDefaultAsync(s =>
                (s.Level >= 80) && (s.AccountId == clientSession.Account.AccountId) && (s.ServerId == _configuration.Value.ServerId) &&
                (s.State == CharacterState.Active)).ConfigureAwait(false) == null)
            {
                //Needs at least a level 80 to create a martial artist
                //TODO log
                return;
            }

            if (await _characterDao.FirstOrDefaultAsync(s =>
                (s.AccountId == clientSession.Account.AccountId) &&
                (s.Class == CharacterClassType.MartialArtist) && (s.State == CharacterState.Active)).ConfigureAwait(false) != null)
            {
                //If already a martial artist, can't create another
                //TODO log
                return;
            }
            //todo add cooldown for recreate 30days

            await clientSession.HandlePacketsAsync(new[] { packet.Adapt<CharNewPacket>() }).ConfigureAwait(false);
        }
    }
}