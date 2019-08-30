//using NosCore.GameObject;
//using NosCore.GameObject.Networking.ClientSession;
//using System;
//using System.Collections.Generic;
//using System.Text;

//namespace NosCore.PacketHandlers.Parcel
//{
//    public class PclPacketHandler : PacketHandler<PclPacket>, IWorldPacketHandler
//    {
//        public override void Execute(PclPacket packet, ClientSession clientSession)
//        {
//            //    int giftId = getGiftPacket.GiftId;
//            //    if (!Session.Character.MailList.ContainsKey(giftId))
//            //    {
//            //        return;
//            //    }
//            //    MailDTO mail = Session.Character.MailList[giftId];
//            //    if (getGiftPacket.Type == 4 && mail.AttachmentVNum != null)
//            //    {
//            //        if (Session.Character.Inventory.CanAddItem((short)mail.AttachmentVNum))
//            //        {
//            //            ItemInstance newInv = Session.Character.Inventory.AddNewToInventory((short)mail.AttachmentVNum, mail.AttachmentAmount, Upgrade: mail.AttachmentUpgrade, Rare: (sbyte)mail.AttachmentRarity).FirstOrDefault();
//            //            if (newInv == null)
//            //            {
//            //                return;
//            //            }
//            //            if (newInv.Rare != 0)
//            //            {
//            //                WearableInstance wearable = newInv as WearableInstance;
//            //                wearable?.SetRarityPoint();
//            //            }
//            //            GeneralLogDTO log = new GeneralLogDTO
//            //            {
//            //                LogType = "CLAIM_GIFT",
//            //                LogData = $"CLAIM GIFT {giftId}",
//            //                IpAddress = Session.IpAddress,
//            //                Timestamp = DateTime.Now,
//            //            };
//            //            DAOFactory.GeneralLogDAO.InsertOrUpdate(ref log);

//            //            Session.SendPacket(Session.Character.GenerateSay($"{Language.Instance.GetMessageFromKey("ITEM_GIFTED")}: {newInv.Item.Name} x {mail.AttachmentAmount}", 12));

//            //            Session.Character.MailList.Remove(giftId);

//            //            Session.SendPacket($"parcel 2 1 {giftId}");
//            //            if (Session.Character.MailList.ContainsKey(giftId))
//            //            {
//            //                Session.Character.MailList.Remove(giftId);
//            //            }
//            //        }
//            //        else
//            //        {
//            //            Session.SendPacket("parcel 5 1 0");
//            //            Session.SendPacket(UserInterfaceHelper.Instance.GenerateMsg(Language.Instance.GetMessageFromKey("NOT_ENOUGH_PLACE"), 0));
//            //        }
//            //    }
//            //    else if (getGiftPacket.Type == 5)
//            //    {
//            //        Session.SendPacket($"parcel 7 1 {giftId}");

//            //        if (Session.Character.MailList.ContainsKey(giftId))
//            //        {
//            //            Session.Character.MailList.Remove(giftId);
//            //        }
//            //    }
//        }
//    }
//}
