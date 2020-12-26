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

using System.Threading.Tasks;
using NosCore.Packets.ClientPackets.CharacterSelectionScreen;
using NosCore.Packets.ServerPackets.UI;
using NosCore.Core.Encryption;
using NosCore.Dao.Interfaces;
using NosCore.Data.CommandPackets;
using NosCore.Data.Dto;
using NosCore.Data.Enumerations.Character;
using NosCore.GameObject;
using NosCore.GameObject.Networking.ClientSession;
using NosCore.Packets.Enumerations;
using NosCore.Shared.Authentication;

namespace NosCore.PacketHandlers.CharacterScreen
{
    public class CharacterDeletePacketHandler : PacketHandler<CharacterDeletePacket>, IWorldPacketHandler
    {
        private readonly IDao<AccountDto, long> _accountDao;
        private readonly IDao<CharacterDto, long> _characterDao;
        private readonly IHasher _hasher;

        public CharacterDeletePacketHandler(IDao<CharacterDto, long> characterDao, IDao<AccountDto, long> accountDao, IHasher hasher)
        {
            _characterDao = characterDao;
            _accountDao = accountDao;
            _hasher = hasher;
        }

        public override async Task ExecuteAsync(CharacterDeletePacket packet, ClientSession clientSession)
        {
            var account = await _accountDao
                .FirstOrDefaultAsync(s => s.AccountId.Equals(clientSession.Account.AccountId)).ConfigureAwait(false);
            if (account == null)
            {
                return;
            }

            if ((account.Password!.ToLower() == _hasher.Hash(packet.Password!)) || (account.Name == packet.Password))
            {
                var character = await _characterDao.FirstOrDefaultAsync(s =>
                    (s.AccountId == account.AccountId) && (s.Slot == packet.Slot)
                    && (s.State == CharacterState.Active)).ConfigureAwait(false);
                if (character == null)
                {
                    return;
                }

                character.State = CharacterState.Inactive;
                character = await _characterDao.TryInsertOrUpdateAsync(character).ConfigureAwait(false);

                await clientSession.HandlePacketsAsync(new[]
                {
                    new EntryPointPacket
                    {
                        Header = "EntryPoint",
                        Name = account.Name
                    }
                }).ConfigureAwait(false);
            }
            else
            {
                await clientSession.SendPacketAsync(new InfoiPacket
                {
                    Message = Game18NConstString.IncorrectPassword
                }).ConfigureAwait(false);
            }
        }
    }
}