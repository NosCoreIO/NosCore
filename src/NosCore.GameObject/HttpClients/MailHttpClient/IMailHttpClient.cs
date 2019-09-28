using System.Collections.Generic;
using Microsoft.AspNetCore.JsonPatch;
using NosCore.Data;
using NosCore.Data.Dto;
using NosCore.Data.WebApi;
using NosCore.GameObject.ComponentEntities.Interfaces;

namespace NosCore.GameObject.HttpClients.FriendHttpClient
{
    public interface IMailHttpClient
    {
        void SendGift(ICharacterEntity characterEntity, long receiverId, IItemInstanceDto itemInstance, bool isNosmall);

        void SendGift(ICharacterEntity characterEntity, long receiverId, short vnum, short amount, sbyte rare,
            byte upgrade, bool isNosmall);

        IEnumerable<MailData> GetGifts(long characterId);
        MailData GetGift(long id, long characterId, bool isCopy);
        void DeleteGift(long giftId, long visualId, bool isCopy);
        void ViewGift(long giftId, JsonPatchDocument<MailDto> mailData);
        void SendMessage(ICharacterEntity character, long characterId, string title, string text);
    }
}