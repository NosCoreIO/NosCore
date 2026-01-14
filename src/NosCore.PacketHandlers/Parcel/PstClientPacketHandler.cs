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

using System.Linq;
using Json.More;
using Json.Patch;
using Json.Pointer;
using NosCore.Dao.Interfaces;
using NosCore.Data.Dto;
using NosCore.GameObject;
using NosCore.GameObject.Networking.ClientSession;
using NosCore.Packets.ClientPackets.Parcel;
using NosCore.Packets.Enumerations;
using NosCore.Packets.ServerPackets.Chats;
using NosCore.Shared.Enumerations;
using System.Threading.Tasks;
using NodaTime;
using NosCore.GameObject.Helper;
using NosCore.GameObject.InterChannelCommunication.Hubs.MailHub;

namespace NosCore.PacketHandlers.Parcel
{
    public class PstClientPacketHandler(IMailHub mailHttpClient, IDao<CharacterDto, long> characterDao, IClock clock)
        : PacketHandler<PstClientPacket>, IWorldPacketHandler
    {
        public override async Task ExecuteAsync(PstClientPacket pstClientPacket, ClientSession clientSession)
        {
            var isCopy = pstClientPacket.Type == 2;
            var mails = await mailHttpClient.GetMails(pstClientPacket.Id, clientSession.Character.VisualId, isCopy).ConfigureAwait(false);
            var mail = mails.FirstOrDefault();
            switch (pstClientPacket.ActionType)
            {
                case 3:
                    if (mail == null)
                    {
                        return;
                    }

                    var patch = new JsonPatch(PatchOperation.Replace(JsonPointer.Parse("/IsOpened"), true.AsJsonElement().AsNode()));
                    await mailHttpClient.ViewMailAsync(mail.MailDto.MailId, patch).ConfigureAwait(false);
                    await clientSession.SendPacketAsync(mail.GeneratePostMessage(pstClientPacket.Type)).ConfigureAwait(false);
                    break;
                case 2:
                    if (mail == null)
                    {
                        return;
                    }

                    await mailHttpClient.DeleteMailAsync(pstClientPacket.Id, clientSession.Character.VisualId, isCopy).ConfigureAwait(false);
                    await clientSession.SendPacketAsync(new SayiPacket
                    {
                        VisualType = VisualType.Player,
                        VisualId = clientSession.Character.CharacterId,
                        Type = SayColorType.Red,
                        Message = Game18NConstString.NoteDeleted
                    }).ConfigureAwait(false);
                    break;
                case 1:
                    if (string.IsNullOrEmpty(pstClientPacket.Text) || string.IsNullOrEmpty(pstClientPacket.Title))
                    {
                        return;
                    }

                    var dest = await characterDao.FirstOrDefaultAsync(s => s.Name == pstClientPacket.ReceiverName && s.ServerId == clientSession.Character.ServerId).ConfigureAwait(false);
                    if (dest != null)
                    {
                        await mailHttpClient.SendMailAsync(GiftHelper.GenerateMailRequest(clock, clientSession.Character, dest.CharacterId, null, null, null, null, null, false, pstClientPacket.Title, pstClientPacket.Text)).ConfigureAwait(false);
                        await clientSession.SendPacketAsync(new SayiPacket
                        {
                            VisualType = VisualType.Player,
                            VisualId = clientSession.Character.CharacterId,
                            Type = SayColorType.Red,
                            Message = Game18NConstString.NoteSent
                        }).ConfigureAwait(false);
                    }
                    else
                    {
                        await clientSession.SendPacketAsync(new SayiPacket
                        {
                            VisualType = VisualType.Player,
                            VisualId = clientSession.Character.CharacterId,
                            Type = SayColorType.Yellow,
                            Message = Game18NConstString.CanNotFindPlayer
                        }).ConfigureAwait(false);
                    }

                    break;
                default:
                    return;
            }
        }
    }
}