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
        private readonly IGenericDao<CharacterDto> _characterDao;
        private readonly IMailHttpClient _mailHttpClient;

        public PstClientPacketHandler(IMailHttpClient mailHttpClient, IGenericDao<CharacterDto> characterDao)
        {
            _mailHttpClient = mailHttpClient;
            _characterDao = characterDao;
        }

        public override async Task Execute(PstClientPacket pstClientPacket, ClientSession clientSession)
        {
            var isCopy = pstClientPacket.Type == 2;
            var mail = await _mailHttpClient.GetGift(pstClientPacket.Id, clientSession.Character.VisualId, isCopy);
            switch (pstClientPacket.ActionType)
            {
                case 3:
                    if (mail == null)
                    {
                        return;
                    }

                    var patch = new JsonPatchDocument<MailDto>();
                    patch.Replace(link => link.IsOpened, true);
                    await _mailHttpClient.ViewGift(mail.MailDto.MailId, patch);
                    await clientSession.SendPacket(mail.GeneratePostMessage(pstClientPacket.Type));
                    break;
                case 2:
                    if (mail == null)
                    {
                        return;
                    }

                    await _mailHttpClient.DeleteGift(pstClientPacket.Id, clientSession.Character.VisualId, isCopy);
                    await clientSession.SendPacket(
                        clientSession.Character.GenerateSay(
                            Language.Instance.GetMessageFromKey(LanguageKey.MAIL_DELETED,
                                clientSession.Account.Language),
                            SayColorType.Purple));
                    break;
                case 1:
                    if (string.IsNullOrEmpty(pstClientPacket.Text) || string.IsNullOrEmpty(pstClientPacket.Title))
                    {
                        return;
                    }

                    var dest = _characterDao.FirstOrDefault(s => s.Name == pstClientPacket.ReceiverName);
                    if (dest != null)
                    {
                        await _mailHttpClient.SendMessage(clientSession.Character, dest.CharacterId, pstClientPacket.Title,
                            pstClientPacket.Text);
                        await clientSession.SendPacket(clientSession.Character.GenerateSay(
                            Language.Instance.GetMessageFromKey(
                                LanguageKey.MAILED,
                                clientSession.Account.Language), SayColorType.Yellow));
                    }
                    else
                    {
                        await clientSession.SendPacket(
                            clientSession.Character.GenerateSay(
                                Language.Instance.GetMessageFromKey(LanguageKey.USER_NOT_FOUND,
                                    clientSession.Account.Language),
                                SayColorType.Yellow));
                    }

                    break;
                default:
                    return;
            }
        }
    }
}