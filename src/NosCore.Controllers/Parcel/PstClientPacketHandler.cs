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
using NosCore.Data.AliveEntities;

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
            var mail = _mailHttpClient.GetGift(pstClientPacket.Id, clientSession.Character.VisualId);
            switch (pstClientPacket.ActionType)
            {
                case 3:
                    if (mail == null)
                    {
                        return;
                    }
                    var patch = new JsonPatchDocument<MailDto>();
                    patch.Replace(link => link.IsOpened, true);
                    _mailHttpClient.ViewGift(mail.MailDbKey, patch);
                    //open packet
                    break;
                case 2:
                    if (mail == null)
                    {
                        return;
                    }
                    _mailHttpClient.DeleteGift(pstClientPacket.Id, clientSession.Character.VisualId);
                    clientSession.SendPacket(
                        clientSession.Character.GenerateSay(Language.Instance.GetMessageFromKey(LanguageKey.MAIL_DELETED, clientSession.Account.Language),
                        SayColorType.Purple));
                    //delete packet
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
