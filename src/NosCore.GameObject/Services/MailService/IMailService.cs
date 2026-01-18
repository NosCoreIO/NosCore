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

using Json.Patch;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using NosCore.Dao.Interfaces;
using NosCore.Data.Dto;
using NosCore.Data.StaticEntities;
using NosCore.GameObject.Services.BroadcastService;
using MailData = NosCore.GameObject.InterChannelCommunication.Messages.MailData;

namespace NosCore.GameObject.Services.MailService
{
    public interface IMailService
    {
        Task InitializeAsync(IDao<CharacterDto, long> characterDao, IDao<MailDto, long> mailDao, List<ItemDto> items,
            IDao<IItemInstanceDto?, Guid> itemInstanceDao);

        List<MailData> GetMails(long id, long characterId, bool senderCopy);

        Task<bool> DeleteMailAsync(long id, long characterId, bool senderCopy);

        Task<MailData?> EditMailAsync(long id, JsonPatch mailData);

        Task<bool> SendMailAsync(MailDto mail, short? vNum, short? amount, sbyte? rare, byte? upgrade);

        Task GenerateMailAsync(IPacketSender sender, long characterId, IEnumerable<MailData> mails);
    }
}