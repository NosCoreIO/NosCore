using ChickenAPI.Packets.ClientPackets.Parcel;
using ChickenAPI.Packets.Enumerations;
using Microsoft.AspNetCore.JsonPatch;
using NosCore.Core;
using NosCore.Core.I18N;
using NosCore.Data.Dto;
using NosCore.Data.Enumerations.I18N;
using NosCore.GameObject;
using NosCore.GameObject.ComponentEntities.Extensions;
using NosCore.GameObject.HttpClients.FriendHttpClient;
using NosCore.GameObject.Networking.ClientSession;

namespace NosCore.PacketHandlers.Parcel
{
    public class PstClientPacketHandler : PacketHandler<PstClientPacket>, IWorldPacketHandler
    {
        private readonly IMailHttpClient _mailHttpClient;
        private readonly IGenericDao<CharacterDto> _characterDao;

        public PstClientPacketHandler(IMailHttpClient mailHttpClient, IGenericDao<CharacterDto> characterDao)
        {
            _mailHttpClient = mailHttpClient;
            _characterDao = characterDao;
        }

        public override void Execute(PstClientPacket pstClientPacket, ClientSession clientSession)
        {
            var isCopy = pstClientPacket.Type == 2;
            var mail = _mailHttpClient.GetGift(pstClientPacket.Id, clientSession.Character.VisualId, isCopy);
            switch (pstClientPacket.ActionType)
            {
                case 3:
                    if (mail == null)
                    {
                        return;
                    }
                    var patch = new JsonPatchDocument<MailDto>();
                    patch.Replace(link => link.IsOpened, true);
                    _mailHttpClient.ViewGift(mail.MailDto.MailId, patch);
                    clientSession.SendPacket(mail.GeneratePostMessage(pstClientPacket.Type));
                    break;
                case 2:
                    if (mail == null)
                    {
                        return;
                    }
                    _mailHttpClient.DeleteGift(pstClientPacket.Id, clientSession.Character.VisualId, isCopy);
                    clientSession.SendPacket(
                        clientSession.Character.GenerateSay(Language.Instance.GetMessageFromKey(LanguageKey.MAIL_DELETED, clientSession.Account.Language),
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
                        _mailHttpClient.SendMessage(clientSession.Character, dest.CharacterId, pstClientPacket.Title, pstClientPacket.Text);
                        clientSession.SendPacket(clientSession.Character.GenerateSay(Language.Instance.GetMessageFromKey(
                            LanguageKey.MAILED,
                            clientSession.Account.Language), SayColorType.Yellow));
                    }
                    else
                    {
                        clientSession.SendPacket(
                            clientSession.Character.GenerateSay(Language.Instance.GetMessageFromKey(LanguageKey.USER_NOT_FOUND, clientSession.Account.Language),
                            SayColorType.Yellow));
                    }
                    break;
                default:
                    return;
            }
        }
    }
}
