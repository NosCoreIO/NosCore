using System;
using ChickenAPI.Packets.ClientPackets.Parcel;
using ChickenAPI.Packets.Enumerations;
using ChickenAPI.Packets.ServerPackets.Parcel;
using ChickenAPI.Packets.ServerPackets.UI;
using NosCore.Core.I18N;
using NosCore.Data.Enumerations.I18N;
using NosCore.GameObject;
using NosCore.GameObject.ComponentEntities.Extensions;
using NosCore.GameObject.HttpClients.FriendHttpClient;
using NosCore.GameObject.Networking.ClientSession;
using NosCore.GameObject.Providers.InventoryService;
using System.Collections.Generic;
using System.Linq;
using NosCore.Data;
using NosCore.GameObject.Providers.ItemProvider;
using NosCore.Core;
using Microsoft.AspNetCore.JsonPatch;

namespace NosCore.PacketHandlers.Parcel
{
    public class PstClientPacketHandler : PacketHandler<PstClientPacket>, IWorldPacketHandler
    {
        private readonly IMailHttpClient _mailHttpClient;

        public PstClientPacketHandler(IMailHttpClient mailHttpClient)
        {
            _mailHttpClient = mailHttpClient;
        }

        public override void Execute(PstClientPacket pstClientPacket, ClientSession clientSession)
        {
            var mail = _mailHttpClient.GetGift(pstClientPacket.Id, clientSession.Character.VisualId);
            if (mail == null)
            {
                return;
            }

            switch (pstClientPacket.ActionType)
            {
                case 3:
                    var patch = new JsonPatchDocument<MailDto>();
                    patch.Replace(link => link.IsOpened, true);
                    _mailHttpClient.ViewGift(mail.MailDbKey, patch);
                    break;
                case 2:
                    _mailHttpClient.DeleteGift(pstClientPacket.Id, clientSession.Character.VisualId);
                    clientSession.SendPacket(
                        clientSession.Character.GenerateSay(Language.Instance.GetMessageFromKey(LanguageKey.MAIL_DELETED, clientSession.Account.Language),
                        SayColorType.Purple));
                    break;
                case 1:
                    if (string.IsNullOrEmpty(pstClientPacket.Text) || string.IsNullOrEmpty(pstClientPacket.Title))
                    {
                        return;
                    }

                    if (true)
                    {
                        //_mailHttpClient.SendGift(session.Character, receiver.Item2.ConnectedCharacter.Id, giftPacket.VNum, giftPacket.Amount, giftPacket.Rare, giftPacket.Upgrade, false);
                        clientSession.SendPacket(clientSession.Character.GenerateSay(Language.Instance.GetMessageFromKey(
                            LanguageKey.MAILED,
                            clientSession.Account.Language), SayColorType.Yellow));
                        //sendmessage
                        //var mail = new MailDTO
                        //{
                        //    AttachmentAmount = 0,
                        //    IsOpened = false,
                        //    Date = DateTime.Now,
                        //    Title = datasplit[0],
                        //    Message = datasplit[1],
                        //    ReceiverId = receiver.CharacterId,
                        //    SenderId = Session.Character.CharacterId,
                        //    IsSenderCopy = false,
                        //    SenderClass = Session.Character.Class,
                        //    SenderGender = Session.Character.Gender,
                        //    SenderHairColor = Enum.IsDefined(typeof(HairColorType), color) ? (HairColorType)color : 0,
                        //    SenderHairStyle = Session.Character.HairStyle,
                        //    EqPacket = Session.Character.GenerateEqListForPacket(),
                        //    SenderMorphId = Session.Character.Morph == 0 ? (short)-1 : (short)(Session.Character.Morph > short.MaxValue ? 0 : Session.Character.Morph)
                        //};
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
