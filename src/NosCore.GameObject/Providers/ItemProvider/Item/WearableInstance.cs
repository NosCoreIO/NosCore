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

using System;
using System.Collections.Generic;
using System.Reactive.Subjects;
using ChickenAPI.Packets.ClientPackets.Inventory;
using ChickenAPI.Packets.Enumerations;
using ChickenAPI.Packets.ServerPackets.Inventory;
using ChickenAPI.Packets.ServerPackets.Shop;
using ChickenAPI.Packets.ServerPackets.UI;
using NosCore.Core;
using NosCore.Core.I18N;
using NosCore.Data.Dto;
using NosCore.Data.Enumerations.I18N;
using NosCore.Data.Enumerations.Items;
using NosCore.GameObject.ComponentEntities.Extensions;
using NosCore.GameObject.Helper;
using NosCore.GameObject.Networking.ClientSession;
using NosCore.GameObject.Providers.InventoryService;
using Serilog;

namespace NosCore.GameObject.Providers.ItemProvider.Item
{
    public class WearableInstance : WearableInstanceDto, IItemInstance
    {
        private readonly ILogger _logger = Logger.GetLoggerConfiguration().CreateLogger();

        public WearableInstance(Item item)
        {
            Id = Guid.NewGuid();
            Item = item;
            ItemVNum = item.VNum;
        }

        public WearableInstance()
        {
        }

        public bool IsBound => BoundCharacterId.HasValue && (Item.ItemType != ItemType.Armor)
            && (Item.ItemType != ItemType.Weapon);

        public Item Item { get; set; }

        public object Clone()
        {
            return (WearableInstance) MemberwiseClone();
        }

        public Subject<RequestData<Tuple<InventoryItemInstance, UseItemPacket>>> Requests { get; set; }

        public void SetRarityPoint()
        {
            switch (Item.EquipmentSlot)
            {
                case EquipmentType.MainWeapon:
                case EquipmentType.SecondaryWeapon:
                {
                    var point = CharacterHelper.Instance.RarityPoint(Rare,
                        Item.IsHeroic ? (short) (95 + Item.LevelMinimum) : Item.LevelMinimum);
                    Concentrate = 0;
                    HitRate = 0;
                    DamageMinimum = 0;
                    DamageMaximum = 0;
                    if (Rare >= 0)
                    {
                        for (var i = 0; i < point; i++)
                        {
                            var rndn = RandomFactory.Instance.RandomNumber(0, 3);
                            if (rndn == 0)
                            {
                                Concentrate++;
                                HitRate++;
                            }
                            else
                            {
                                DamageMinimum++;
                                DamageMaximum++;
                            }
                        }
                    }
                    else
                    {
                        for (var i = 0; i > Rare * 10; i--)
                        {
                            DamageMinimum--;
                            DamageMaximum--;
                        }
                    }
                }
                    break;

                case EquipmentType.Armor:
                {
                    var point = CharacterHelper.Instance.RarityPoint(Rare,
                        Item.IsHeroic ? (short) (95 + Item.LevelMinimum) : Item.LevelMinimum);
                    DefenceDodge = 0;
                    DistanceDefenceDodge = 0;
                    DistanceDefence = 0;
                    MagicDefence = 0;
                    CloseDefence = 0;
                    if (Rare < 0)
                    {
                        for (var i = 0; i > Rare * 10; i--)
                        {
                            DistanceDefence--;
                            MagicDefence--;
                            CloseDefence--;
                        }

                        return;
                    }

                    for (var i = 0; i < point; i++)
                    {
                        var rndn = RandomFactory.Instance.RandomNumber(0, 3);
                        if (rndn == 0)
                        {
                            DefenceDodge++;
                            DistanceDefenceDodge++;
                        }
                        else
                        {
                            DistanceDefence++;
                            MagicDefence++;
                            CloseDefence++;
                        }
                    }
                }
                    break;

                default:
                    _logger.Error(LogLanguage.Instance.GetMessageFromKey(LogLanguageKey.UNKNOWN_EQUIPMENTTYPE),
                        Item.EquipmentSlot);
                    break;
            }
        }

        public void Sum(ClientSession clientSession, WearableInstance item)
        {
            short[] upSuccess = clientSession.WorldConfiguration.SumSuccessPercent;
            int[] goldPrice = clientSession.WorldConfiguration.SumGoldPrice;
            short[] sandAmount = clientSession.WorldConfiguration.SumSandAmount;

            if (clientSession.Character.Gold < goldPrice[Upgrade + item.Upgrade])
            {
                clientSession.SendPacket(clientSession.Character.GenerateSay(
                    Language.Instance.GetMessageFromKey(LanguageKey.NOT_ENOUGH_MONEY, clientSession.Account.Language),
                    SayColorType.Yellow));
                return;
            }

            if (clientSession.Character.Inventory.CountItem(clientSession.WorldConfiguration.SumSandVNum) < sandAmount[Upgrade + item.Upgrade])
            {
                clientSession.SendPacket(clientSession.Character.GenerateSay(
                    Language.Instance.GetMessageFromKey(LanguageKey.NOT_ENOUGH_ITEMS, clientSession.Account.Language),
                    SayColorType.Yellow));
                return;
            }

            clientSession.Character.Inventory.RemoveItemAmountFromInventoryByVNum(
                (byte) sandAmount[Upgrade + item.Upgrade], clientSession.WorldConfiguration.SumSandVNum);
            clientSession.Character.Gold -= goldPrice[Upgrade + item.Upgrade];

            var random = (short)RandomFactory.Instance.RandomNumber();
            if (random <= upSuccess[Upgrade + item.Upgrade])
            {
                HandleSuccessSum(clientSession, item);
            }
            else
            {
                HandleFailedSum(clientSession, item);
            }

            clientSession.SendPackets(clientSession.Character.GenerateInv());
            clientSession.SendPacket(clientSession.Character.GenerateGold());
            clientSession.SendPacket(new ShopEndPacket
            {
                Type = ShopEndPacketType.CloseSubWindow
            });
        }

        public void HandleSuccessSum(ClientSession clientSession, WearableInstance item)
        {
            Upgrade += (byte)(item.Upgrade + 1);
            DarkResistance += (short)((item.DarkResistance ?? 0) + item.Item.DarkResistance);
            LightResistance += (short)((item.LightResistance ?? 0) + item.Item.LightResistance);
            WaterResistance += (short)((item.WaterResistance ?? 0) + item.Item.WaterResistance);
            FireResistance += (short)((item.FireResistance ?? 0) + item.Item.FireResistance);

            clientSession.SendPacket(new PdtiPacket
            {
                Unknow = 10,
                ItemVnum = ItemVNum,
                RecipeAmount = 1,
                Unknow3 = 27,
                ItemUpgrade = Upgrade,
                Unknow4 = 0
            });
            SendSumResult(clientSession, item, true);
        }

        public void HandleFailedSum(ClientSession clientSession, WearableInstance item)
        {
            clientSession.Character.Inventory.RemoveItemAmountFromInventory(1, Id);
            SendSumResult(clientSession, item, false);
        }

        public void SendSumResult(ClientSession clientSession, WearableInstance item, bool success)
        {
            clientSession.Character.Inventory.RemoveItemAmountFromInventory(1, item.Id);
            clientSession.SendPacket(new MsgPacket
            {
                Message = Language.Instance.GetMessageFromKey(
                    success ? LanguageKey.SUM_SUCCESS : LanguageKey.SUM_FAILED,
                    clientSession.Account.Language)
            });
            clientSession.SendPacket(clientSession.Character.GenerateSay(
                Language.Instance.GetMessageFromKey(
                    success ? LanguageKey.SUM_SUCCESS : LanguageKey.SUM_FAILED,
                    clientSession.Account.Language),
                success ? SayColorType.Green : SayColorType.Purple));
            clientSession.SendPacket(new GuriPacket
            {
                Type = GuriPacketType.AfterSumming,
                Unknown = 1,
                EntityId = clientSession.Character.VisualId,
                Value = success ? (uint)1324 : 1332
            });
        }
    }
}