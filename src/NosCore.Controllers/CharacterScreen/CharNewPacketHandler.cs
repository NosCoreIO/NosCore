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
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using NosCore.Packets.ClientPackets.CharacterSelectionScreen;
using NosCore.Packets.Enumerations;
using NosCore.Packets.ServerPackets.UI;
using NosCore.Core;
using NosCore.Dao.Interfaces;
using NosCore.Data.CommandPackets;
using NosCore.Data.Dto;
using NosCore.Data.Enumerations.Character;
using NosCore.Data.Enumerations.I18N;
using NosCore.GameObject;
using NosCore.GameObject.Networking.ClientSession;
using NosCore.Packets.ServerPackets.CharacterSelectionScreen;

namespace NosCore.PacketHandlers.CharacterScreen
{
    public class CharNewPacketHandler : PacketHandler<CharNewPacket>, IWorldPacketHandler
    {
        private readonly IDao<CharacterDto, long> _characterDao;
        private readonly IDao<MinilandDto, Guid> _minilandDao;

        public CharNewPacketHandler(IDao<CharacterDto, long> characterDao, IDao<MinilandDto, Guid> minilandDao)
        {
            _characterDao = characterDao;
            _minilandDao = minilandDao;
        }

        public override async Task ExecuteAsync(CharNewPacket packet, ClientSession clientSession)
        {
            if (clientSession.HasSelectedCharacter)
            {
                return;
            }

            // TODO: Hold Account Information in Authorized object
            var accountId = clientSession.Account.AccountId;
            var slot = packet.Slot;
            var characterName = packet.Name;
            if (await _characterDao.FirstOrDefaultAsync(s =>
                (s.AccountId == accountId) && (s.Slot == slot) && (s.State == CharacterState.Active)).ConfigureAwait(false) != null)
            {
                return;
            }

            var rg = new Regex(
                @"^[\u0021-\u007E\u00A1-\u00AC\u00AE-\u00FF\u4E00-\u9FA5\u0E01-\u0E3A\u0E3F-\u0E5B\u002E]*$");
            if (rg.Matches(characterName).Count == 1)
            {
                var character = await
                    _characterDao.FirstOrDefaultAsync(s =>
                        (s.Name == characterName) && (s.State == CharacterState.Active)).ConfigureAwait(false);
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
                        State = CharacterState.Active
                    };
                    await _characterDao.TryInsertOrUpdateAsync(chara).ConfigureAwait(false);

                    var miniland = new MinilandDto
                    {
                        MinilandId = Guid.NewGuid(),
                        State = MinilandState.Open,
                        MinilandMessage = "Welcome",
                        OwnerId = chara.CharacterId,
                        WelcomeMusicInfo = "Spring^Melody"
                    };
                    await _minilandDao.TryInsertOrUpdateAsync(miniland).ConfigureAwait(false);
                    await clientSession.SendPacketAsync(new SuccessPacket()).ConfigureAwait(false);
                    await clientSession.HandlePacketsAsync(new[] { new EntryPointPacket() }).ConfigureAwait(false);
                }
                else
                {
                    await clientSession.SendPacketAsync(new InfoPacket
                    {
                        Message = clientSession.GetMessageFromKey(LanguageKey.ALREADY_TAKEN)
                    }).ConfigureAwait(false);
                }
            }
            else
            {
                await clientSession.SendPacketAsync(new InfoPacket
                {
                    Message = clientSession.GetMessageFromKey(LanguageKey.INVALID_CHARNAME)
                }).ConfigureAwait(false);
            }
        }
    }
}