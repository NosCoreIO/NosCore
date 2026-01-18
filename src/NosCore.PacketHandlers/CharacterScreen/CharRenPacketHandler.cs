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

using NosCore.Dao.Interfaces;
using NosCore.Data.CommandPackets;
using NosCore.Data.Dto;
using NosCore.Data.Enumerations.Character;
using NosCore.GameObject.Networking.ClientSession;
using NosCore.Packets.ClientPackets.CharacterSelectionScreen;
using NosCore.Packets.Enumerations;
using NosCore.Packets.ServerPackets.CharacterSelectionScreen;
using NosCore.Packets.ServerPackets.UI;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Microsoft.Extensions.Options;
using NosCore.Core.Configuration;
using NosCore.GameObject.Infastructure;

namespace NosCore.PacketHandlers.CharacterScreen
{
    public class CharRenPacketHandler(IDao<CharacterDto, long> characterDao, IOptions<WorldConfiguration> configuration)
        : PacketHandler<CharRenamePacket>, IWorldPacketHandler
    {
        public override async Task ExecuteAsync(CharRenamePacket packet, ClientSession clientSession)
        {
            // TODO: Hold Account Information in Authorized object
            var accountId = clientSession.Account.AccountId;
            var slot = packet.Slot;
            var characterName = packet.Name;
            var chara = await characterDao.FirstOrDefaultAsync(s =>
                    (s.AccountId == accountId) && (s.Slot == slot) && (s.State == CharacterState.Active) && (s.ServerId == configuration.Value.ServerId))
                .ConfigureAwait(false);
            if ((chara == null) || (chara.ShouldRename == false))
            {
                return;
            }

            var rg = new Regex(CharNewPacketHandler.Nameregex);
            if (rg.Matches(characterName!).Count == 1)
            {
                var character = await
                    characterDao.FirstOrDefaultAsync(s =>
                        (s.Name == characterName) && (s.State == CharacterState.Active)).ConfigureAwait(false);
                if (character == null)
                {
                    chara.Name = characterName;
                    chara.ShouldRename = false;
                    await characterDao.TryInsertOrUpdateAsync(chara).ConfigureAwait(false);
                    await clientSession.SendPacketAsync(new SuccessPacket()).ConfigureAwait(false);
                    await clientSession.HandlePacketsAsync(new[] { new EntryPointPacket()
                    {
                        Name = clientSession.Account.Name,
                    } }).ConfigureAwait(false);
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