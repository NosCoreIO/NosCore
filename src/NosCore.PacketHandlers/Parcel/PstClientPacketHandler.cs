//  __  _  __    __   ___ __  ___ ___
// |  \| |/__\ /' _/ / _//__\| _ \ __|
// | | ' | \/ |`._`.| \_| \/ | v / _|
// |_|\__|\__/ |___/ \__/\__/|_|_\___|
//

using Json.More;
using Json.Patch;
using Json.Pointer;
using NodaTime;
using NosCore.Dao.Interfaces;
using NosCore.Data.Dto;
using NosCore.GameObject.Infastructure;
using NosCore.GameObject.InterChannelCommunication.Hubs.MailHub;
using NosCore.GameObject.Networking.ClientSession;
using NosCore.GameObject.Services.MailService;
using NosCore.Packets.ClientPackets.Parcel;
using NosCore.Packets.Enumerations;
using NosCore.Packets.ServerPackets.Chats;
using NosCore.Shared.Enumerations;
using System.Linq;
using System.Threading.Tasks;

namespace NosCore.PacketHandlers.Parcel
{
    public class PstClientPacketHandler(IMailHub mailHttpClient, IDao<CharacterDto, long> characterDao, IClock clock)
        : PacketHandler<PstClientPacket>, IWorldPacketHandler
    {
        public override async Task ExecuteAsync(PstClientPacket pstClientPacket, ClientSession clientSession)
        {
            var isCopy = pstClientPacket.Type == 2;
            var mails = await mailHttpClient.GetMails(pstClientPacket.Id, clientSession.Character.VisualId, isCopy);
            var mail = mails.FirstOrDefault();
            switch (pstClientPacket.ActionType)
            {
                case 3:
                    if (mail == null)
                    {
                        return;
                    }

                    var patch = new JsonPatch(PatchOperation.Replace(JsonPointer.Parse("/IsOpened"), true.AsJsonElement().AsNode()));
                    await mailHttpClient.ViewMailAsync(mail.MailDto.MailId, patch);
                    await clientSession.SendPacketAsync(mail.GeneratePostMessage(pstClientPacket.Type));
                    break;
                case 2:
                    if (mail == null)
                    {
                        return;
                    }

                    await mailHttpClient.DeleteMailAsync(pstClientPacket.Id, clientSession.Character.VisualId, isCopy);
                    await clientSession.SendPacketAsync(new SayiPacket
                    {
                        VisualType = VisualType.Player,
                        VisualId = clientSession.Character.CharacterId,
                        Type = SayColorType.Red,
                        Message = Game18NConstString.NoteDeleted
                    });
                    break;
                case 1:
                    if (string.IsNullOrEmpty(pstClientPacket.Text) || string.IsNullOrEmpty(pstClientPacket.Title))
                    {
                        return;
                    }

                    var dest = await characterDao.FirstOrDefaultAsync(s => s.Name == pstClientPacket.ReceiverName && s.ServerId == clientSession.Character.ServerId);
                    if (dest != null)
                    {
                        await mailHttpClient.SendMailAsync(GiftHelper.GenerateMailRequest(clock, clientSession.Character, dest.CharacterId, null, null, null, null, null, false, pstClientPacket.Title, pstClientPacket.Text));
                        await clientSession.SendPacketAsync(new SayiPacket
                        {
                            VisualType = VisualType.Player,
                            VisualId = clientSession.Character.CharacterId,
                            Type = SayColorType.Red,
                            Message = Game18NConstString.NoteSent
                        });
                    }
                    else
                    {
                        await clientSession.SendPacketAsync(new SayiPacket
                        {
                            VisualType = VisualType.Player,
                            VisualId = clientSession.Character.CharacterId,
                            Type = SayColorType.Yellow,
                            Message = Game18NConstString.CanNotFindPlayer
                        });
                    }

                    break;
                default:
                    return;
            }
        }
    }
}
