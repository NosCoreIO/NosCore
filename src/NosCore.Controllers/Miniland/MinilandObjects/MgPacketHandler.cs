using ChickenAPI.Packets.ClientPackets.Miniland;
using ChickenAPI.Packets.Enumerations;
using NosCore.GameObject;
using NosCore.GameObject.Networking.ClientSession;
using NosCore.GameObject.Providers.MinilandProvider;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace NosCore.PacketHandlers.Miniland
{
    public class MgPacketHandler : PacketHandler<MinigamePacket>, IWorldPacketHandler
    {
        private readonly IMinilandProvider _minilandProvider;

        public MgPacketHandler(IMinilandProvider minilandProvider)
        {
            _minilandProvider = minilandProvider;
        }

        public override void Execute(MinigamePacket minigamePacket, ClientSession clientSession)
        {
            var miniland = _minilandProvider.GetMiniland(clientSession.Character.CharacterId);
            var minilandObject = clientSession.Character.MapInstance.MapDesignObjects.Values.FirstOrDefault(s => s.Slot == minigamePacket.Id);
            if (minilandObject == null || miniland == null)
            {
                return;
            }

            if (minilandObject.InventoryItemInstance.ItemInstance.Item.IsWarehouse)
            {
                return;
            }

            byte game = (byte)(minilandObject.InventoryItemInstance.ItemInstance.Item.EquipmentSlot == EquipmentType.MainWeapon
                    ? (4 + minilandObject.InventoryItemInstance.ItemInstance.ItemVNum) % 10
                    : (int)minilandObject.InventoryItemInstance.ItemInstance.Item.EquipmentSlot / 3);
            bool full = false;

            //switch (packet.Type)
            //{
            //    //play
            //    case 1:
            //        if (mlobj.ItemInstance.DurabilityPoint <= 0)
            //        {
            //            Session.SendPacket(UserInterfaceHelper.Instance.GenerateMsg(Language.Instance.GetMessageFromKey("NOT_ENOUGH_DURABILITY_POINT"), 0));
            //            return;
            //        }
            //        if (Session.Character.MinilandPoint <= 0)
            //        {
            //            Session.SendPacket($"qna #mg^1^7^3125^1^1 {Language.Instance.GetMessageFromKey("NOT_ENOUGH_MINILAND_POINT")}");
            //        }
            //        Session.Character.MapInstance.Broadcast(UserInterfaceHelper.Instance.GenerateGuri(2, 1, Session.Character.CharacterId));
            //        Session.Character.CurrentMinigame = (short)(game == 0 ? 5102 : game == 1 ? 5103 : game == 2 ? 5105 : game == 3 ? 5104 : game == 4 ? 5113 : 5112);
            //        Session.SendPacket($"mlo_st {game}");
            //        break;

            //    //stop
            //    case 2:
            //        Session.Character.CurrentMinigame = 0;
            //        Session.Character.MapInstance.Broadcast(UserInterfaceHelper.Instance.GenerateGuri(6, 1, Session.Character.CharacterId));
            //        break;

            //    case 3:
            //        Session.Character.CurrentMinigame = 0;
            //        Session.Character.MapInstance.Broadcast(UserInterfaceHelper.Instance.GenerateGuri(6, 1, Session.Character.CharacterId));
            //        int Level = -1;
            //        for (short i = 0; i < GetMinilandMaxPoint(game).Count(); i++)
            //        {
            //            if (packet.Point > GetMinilandMaxPoint(game)[i])
            //            {
            //                Level = i;
            //            }
            //            else
            //            {
            //                break;
            //            }
            //        }
            //        Session.SendPacket(Level != -1
            //            ? $"mlo_lv {Level}"
            //            : $"mg 3 {game} {packet.MinigameVNum} 0 0");
            //        break;

            //    // select gift
            //    case 4:
            //        if (Session.Character.MinilandPoint >= 100)
            //        {
            //            Gift obj = GetMinilandGift(packet.MinigameVNum, (int)packet.Point);
            //            if (obj != null)
            //            {
            //                Session.SendPacket($"mlo_rw {obj.VNum} {obj.Amount}");
            //                Session.SendPacket(Session.Character.GenerateMinilandPoint());
            //                List<ItemInstance> inv = Session.Character.Inventory.AddNewToInventory(obj.VNum, obj.Amount);
            //                Session.Character.MinilandPoint -= 100;
            //                if (!inv.Any())
            //                {
            //                    Session.Character.SendGift(Session.Character.CharacterId, obj.VNum, obj.Amount, 0, 0, false);
            //                }

            //                if (client != Session)
            //                {
            //                    switch (packet.Point)
            //                    {
            //                        case 0:
            //                            mlobj.Level1BoxAmount++;
            //                            break;

            //                        case 1:
            //                            mlobj.Level2BoxAmount++;
            //                            break;

            //                        case 2:
            //                            mlobj.Level3BoxAmount++;
            //                            break;

            //                        case 3:
            //                            mlobj.Level4BoxAmount++;
            //                            break;

            //                        case 4:
            //                            mlobj.Level5BoxAmount++;
            //                            break;
            //                    }
            //                }
            //            }
            //        }
            //        break;

            //    case 5:
            //        Session.SendPacket(Session.Character.GenerateMloMg(mlobj, packet));
            //        break;

            //    //refill
            //    case 6:
            //        if (packet.Point == null)
            //        {
            //            return;
            //        }
            //        if (Session.Character.Gold > packet.Point)
            //        {
            //            Session.Character.Gold -= (int)packet.Point;
            //            Session.SendPacket(Session.Character.GenerateGold());
            //            mlobj.ItemInstance.DurabilityPoint += (int)(packet.Point / 100);
            //            Session.SendPacket(UserInterfaceHelper.Instance.GenerateInfo(Language.Instance.GetMessageFromKey(string.Format("REFILL_MINIGAME", (int)packet.Point / 100))));
            //            Session.SendPacket(Session.Character.GenerateMloMg(mlobj, packet));
            //        }
            //        break;

            //    //gift
            //    case 7:
            //        Session.SendPacket(
            //            $"mlo_pmg {packet.MinigameVNum} {Session.Character.MinilandPoint} {(mlobj.ItemInstance.DurabilityPoint < 1000 ? 1 : 0)} {(full ? 1 : 0)} {(mlobj.Level1BoxAmount > 0 ? $"392 {mlobj.Level1BoxAmount}" : "0 0")} {(mlobj.Level2BoxAmount > 0 ? $"393 {mlobj.Level2BoxAmount}" : "0 0")} {(mlobj.Level3BoxAmount > 0 ? $"394 {mlobj.Level3BoxAmount}" : "0 0")} {(mlobj.Level4BoxAmount > 0 ? $"395 {mlobj.Level4BoxAmount}" : "0 0")} {(mlobj.Level5BoxAmount > 0 ? $"396 {mlobj.Level5BoxAmount}" : "0 0")} 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0");
            //        break;

            //    //get gift
            //    case 8:
            //        int amount = 0;
            //        switch (packet.Point)
            //        {
            //            case 0:
            //                amount = mlobj.Level1BoxAmount;
            //                break;

            //            case 1:
            //                amount = mlobj.Level2BoxAmount;
            //                break;

            //            case 2:
            //                amount = mlobj.Level3BoxAmount;
            //                break;

            //            case 3:
            //                amount = mlobj.Level4BoxAmount;
            //                break;

            //            case 4:
            //                amount = mlobj.Level5BoxAmount;
            //                break;
            //        }
            //        List<Gift> gifts = new List<Gift>();
            //        for (int i = 0; i < amount; i++)
            //        {
            //            Gift gift = GetMinilandGift(packet.MinigameVNum, (int)packet.Point);
            //            if (gift != null)
            //            {
            //                if (gifts.Any(o => o.VNum == gift.VNum))
            //                {
            //                    gifts.First(o => o.Amount == gift.Amount).Amount += gift.Amount;
            //                }
            //                else
            //                {
            //                    gifts.Add(gift);
            //                }
            //            }
            //        }
            //        string str = string.Empty;
            //        for (int i = 0; i < 9; i++)
            //        {
            //            if (gifts.Count > i)
            //            {
            //                List<ItemInstance> inv = Session.Character.Inventory.AddNewToInventory(gifts.ElementAt(i).VNum, gifts.ElementAt(i).Amount);
            //                if (inv.Any())
            //                {
            //                    Session.SendPacket(Session.Character.GenerateSay(
            //                        $"{Language.Instance.GetMessageFromKey("ITEM_ACQUIRED")}: {ServerManager.Instance.GetItem(gifts.ElementAt(i).VNum).Name} x {gifts.ElementAt(i).Amount}", 12));
            //                }
            //                else
            //                {
            //                    Session.Character.SendGift(Session.Character.CharacterId, gifts.ElementAt(i).VNum, gifts.ElementAt(i).Amount, 0, 0, false);
            //                }
            //                str += $" {gifts.ElementAt(i).VNum} {gifts.ElementAt(i).Amount}";
            //            }
            //            else
            //            {
            //                str += " 0 0";
            //            }
            //        }
            //        Session.SendPacket(
            //            $"mlo_pmg {packet.MinigameVNum} {Session.Character.MinilandPoint} {(mlobj.ItemInstance.DurabilityPoint < 1000 ? 1 : 0)} {(full ? 1 : 0)} {(mlobj.Level1BoxAmount > 0 ? $"392 {mlobj.Level1BoxAmount}" : "0 0")} {(mlobj.Level2BoxAmount > 0 ? $"393 {mlobj.Level2BoxAmount}" : "0 0")} {(mlobj.Level3BoxAmount > 0 ? $"394 {mlobj.Level3BoxAmount}" : "0 0")} {(mlobj.Level4BoxAmount > 0 ? $"395 {mlobj.Level4BoxAmount}" : "0 0")} {(mlobj.Level5BoxAmount > 0 ? $"396 {mlobj.Level5BoxAmount}" : "0 0")}{str}");
            //        break;

            //    //coupon
            //    case 9:
            //        List<ItemInstance> items = Session.Character.Inventory.Select(s => s.Value).Where(s => s.ItemVNum == 1269 || s.ItemVNum == 1271).OrderBy(s => s.Slot).ToList();
            //        if (items.Count > 0)
            //        {
            //            Session.Character.Inventory.RemoveItemAmount(items.ElementAt(0).ItemVNum);
            //            int point = items.ElementAt(0).ItemVNum == 1269 ? 300 : 500;
            //            mlobj.ItemInstance.DurabilityPoint += point;
            //            Session.SendPacket(UserInterfaceHelper.Instance.GenerateInfo(Language.Instance.GetMessageFromKey(string.Format("REFILL_MINIGAME", point))));
            //            Session.SendPacket(Session.Character.GenerateMloMg(mlobj, packet));
            //        }
            //        break;
            //}
        }
    }
}
