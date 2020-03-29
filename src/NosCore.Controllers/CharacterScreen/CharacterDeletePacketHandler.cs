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
using NosCore.Core;
using NosCore.Core.Encryption;
using NosCore.Data.CommandPackets;
using NosCore.Data.Dto;
using NosCore.Data.Enumerations.Character;
using NosCore.Data.Enumerations.I18N;
using NosCore.GameObject;
using NosCore.GameObject.Networking.ClientSession;

namespace NosCore.PacketHandlers.CharacterScreen
{
    public class CharacterDeletePacketHandler : PacketHandler<CharacterDeletePacket>, IWorldPacketHandler
    {
        private readonly IGenericDao<AccountDto> _accountDao;
        private readonly IGenericDao<CharacterDto> _characterDao;

        public CharacterDeletePacketHandler(IGenericDao<CharacterDto> characterDao, IGenericDao<AccountDto> accountDao)
        {
            _characterDao = characterDao;
            _accountDao = accountDao;
        }

        public override async Task Execute(CharacterDeletePacket packet, ClientSession clientSession)
        {
            if (clientSession.HasCurrentMapInstance)
            {
                return;
            }

            var account = _accountDao
                .FirstOrDefault(s => s.AccountId.Equals(clientSession.Account.AccountId));
            if (account == null)
            {
                return;
            }

            if ((account.Password!.ToLower() == packet.Password!.ToSha512()) || (account.Name == packet.Password))
            {
                var character = _characterDao.FirstOrDefault(s =>
                    (s.AccountId == account.AccountId) && (s.Slot == packet.Slot)
                    && (s.State == CharacterState.Active));
                if (character == null)
                {
                    return;
                }

                character.State = CharacterState.Inactive;
                _characterDao.InsertOrUpdate(ref character);

                await clientSession.HandlePackets(new[]
                {
                    new EntryPointPacket
                    {
                        Header = "EntryPoint",
                        Title = "EntryPoint",
                        Name = account.Name
                    }
                });
            }
            else
            {
                await clientSession.SendPacket(new InfoPacket
                {
                    Message = clientSession.GetMessageFromKey(LanguageKey.BAD_PASSWORD)
                });
            }
        }
    }
}