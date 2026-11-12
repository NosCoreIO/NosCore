//  __  _  __    __   ___ __  ___ ___
// |  \| |/__\ /' _/ / _//__\| _ \ __|
// | | ' | \/ |`._`.| \_| \/ | v / _|
// |_|\__|\__/ |___/ \__/\__/|_|_\___|
//

using NosCore.GameObject.Ecs.Extensions;
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
            var mail = pstClientPacket.Id < 0 ? null
                : (await mailHttpClient.GetMails(pstClientPacket.Id, clientSession.Character.VisualId, isCopy))
                    .FirstOrDefault(s => s.MailId == pstClientPacket.Id);
            switch (pstClientPacket.ActionType)
            {
                case PostActionType.Read:
                    if (mail == null)
                    {
                        return;
                    }

                    var patch = new JsonPatch(PatchOperation.Replace(JsonPointer.Parse("/IsOpened"), true.AsJsonElement().AsNode()));
                    await mailHttpClient.ViewMailAsync(mail.MailDto.MailId, patch);
                    await clientSession.SendPacketAsync(mail.GeneratePostMessage(pstClientPacket.Type));
                    break;
                case PostActionType.Delete:
                    if (mail == null
                        || !await mailHttpClient.DeleteMailAsync(pstClientPacket.Id, clientSession.Character.VisualId, isCopy))
                    {
                        return;
                    }

                    await clientSession.SendPacketAsync(clientSession.Character.GenerateSayi(SayColorType.Red, Game18NConstString.NoteDeleted));
                    break;
                case PostActionType.Send:
                    if (string.IsNullOrEmpty(pstClientPacket.Text) || string.IsNullOrEmpty(pstClientPacket.Title))
                    {
                        return;
                    }

                    var serverId = clientSession.Character.ServerId;
                    var dest = await characterDao.FirstOrDefaultAsync(s => s.Name == pstClientPacket.ReceiverName && s.ServerId == serverId);
                    if (dest != null)
                    {
                        await mailHttpClient.SendMailAsync(GiftHelper.GenerateMailRequest(clock, clientSession.Character, dest.CharacterId, null, null, null, null, null, false, pstClientPacket.Title, pstClientPacket.Text));
                        await clientSession.SendPacketAsync(clientSession.Character.GenerateSayi(SayColorType.Red, Game18NConstString.NoteSent));
                    }
                    else
                    {
                        await clientSession.SendPacketAsync(clientSession.Character.GenerateSayi(SayColorType.Yellow, Game18NConstString.CanNotFindPlayer));
                    }

                    break;
                default:
                    return;
            }
        }
    }
}
