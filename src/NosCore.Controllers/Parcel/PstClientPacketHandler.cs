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
using NosCore.Packets.ClientPackets.Parcel;
using NosCore.Packets.Enumerations;
using Microsoft.AspNetCore.JsonPatch;
using NosCore.Core;
using NosCore.Core.I18N;
using NosCore.Dao.Interfaces;
using NosCore.Data.Dto;
using NosCore.Data.Enumerations.I18N;
using NosCore.GameObject;
using NosCore.GameObject.ComponentEntities.Extensions;
using NosCore.GameObject.HttpClients.MailHttpClient;
using NosCore.GameObject.Networking.ClientSession;

namespace NosCore.PacketHandlers.Parcel
{
    public class PstClientPacketHandler : PacketHandler<PstClientPacket>, IWorldPacketHandler
    {
        private readonly IDao<CharacterDto, long> _characterDao;
        private readonly IMailHttpClient _mailHttpClient;

        public PstClientPacketHandler(IMailHttpClient mailHttpClient, IDao<CharacterDto, long> characterDao)
        {
            _mailHttpClient = mailHttpClient;
            _characterDao = characterDao;
        }

        public override async Task ExecuteAsync(PstClientPacket pstClientPacket, ClientSession clientSession)
        {
            var isCopy = pstClientPacket.Type == 2;
            var mail = await _mailHttpClient.GetGiftAsync(pstClientPacket.Id, clientSession.Character.VisualId, isCopy).ConfigureAwait(false);
            switch (pstClientPacket.ActionType)
            {
                case 3:
                    if (mail == null)
                    {
                        return;
                    }

                    var patch = new JsonPatchDocument<MailDto>();
                    patch.Replace(link => link.IsOpened, true);
                    await _mailHttpClient.ViewGiftAsync(mail.MailDto.MailId, patch).ConfigureAwait(false);
                    await clientSession.SendPacketAsync(mail.GeneratePostMessage(pstClientPacket.Type)).ConfigureAwait(false);
                    break;
                case 2:
                    if (mail == null)
                    {
                        return;
                    }

                    await _mailHttpClient.DeleteGiftAsync(pstClientPacket.Id, clientSession.Character.VisualId, isCopy).ConfigureAwait(false);
                    await clientSession.SendPacketAsync(
                        clientSession.Character.GenerateSay(
                            GameLanguage.Instance.GetMessageFromKey(LanguageKey.MAIL_DELETED,
                                clientSession.Account.Language),
                            SayColorType.Purple)).ConfigureAwait(false);
                    break;
                case 1:
                    if (string.IsNullOrEmpty(pstClientPacket.Text) || string.IsNullOrEmpty(pstClientPacket.Title))
                    {
                        return;
                    }

                    var dest = await _characterDao.FirstOrDefaultAsync(s => s.Name == pstClientPacket.ReceiverName).ConfigureAwait(false);
                    if (dest != null)
                    {
                        await _mailHttpClient.SendMessageAsync(clientSession.Character, dest.CharacterId, pstClientPacket.Title,
                            pstClientPacket.Text).ConfigureAwait(false);
                        await clientSession.SendPacketAsync(clientSession.Character.GenerateSay(
                            GameLanguage.Instance.GetMessageFromKey(
                                LanguageKey.MAILED,
                                clientSession.Account.Language), SayColorType.Yellow)).ConfigureAwait(false);
                    }
                    else
                    {
                        await clientSession.SendPacketAsync(
                            clientSession.Character.GenerateSay(
                                GameLanguage.Instance.GetMessageFromKey(LanguageKey.USER_NOT_FOUND,
                                    clientSession.Account.Language),
                                SayColorType.Yellow)).ConfigureAwait(false);
                    }

                    break;
                default:
                    return;
            }
        }
    }
}