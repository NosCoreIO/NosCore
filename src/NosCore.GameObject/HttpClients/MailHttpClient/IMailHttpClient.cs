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

using System.Collections.Generic;
using System.Threading.Tasks;
using Json.Patch;
using NosCore.Data.Dto;
using NosCore.Data.WebApi;
using NosCore.GameObject.ComponentEntities.Interfaces;

namespace NosCore.GameObject.HttpClients.MailHttpClient
{
    public interface IMailHttpClient
    {
        Task SendGiftAsync(ICharacterEntity characterEntity, long receiverId, IItemInstanceDto itemInstance, bool isNosmall);

        Task SendGiftAsync(ICharacterEntity characterEntity, long receiverId, short vnum, short amount, sbyte rare,
            byte upgrade, bool isNosmall);

        Task<IEnumerable<MailData>> GetGiftsAsync(long characterId);
        Task<MailData?> GetGiftAsync(long id, long characterId, bool isCopy);
        Task DeleteGiftAsync(long giftId, long visualId, bool isCopy);
        Task ViewGiftAsync(long giftId, JsonPatch mailData);
        Task SendMessageAsync(ICharacterEntity character, long characterId, string title, string text);
    }
}