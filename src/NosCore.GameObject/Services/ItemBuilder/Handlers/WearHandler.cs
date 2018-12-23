//  __  _  __    __   ___ __  ___ ___  
// |  \| |/__\ /' _/ / _//__\| _ \ __| 
// | | ' | \/ |`._`.| \_| \/ | v / _|  
// |_|\__|\__/ |___/ \__/\__/|_|_\___| 
// 
// Copyright (C) 2018 - NosCore
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


using NosCore.GameObject.ComponentEntities.Extensions;
using NosCore.GameObject.Handling;
using NosCore.GameObject.Networking.ClientSession;
using NosCore.GameObject.Networking.Group;
using NosCore.GameObject.Services.ItemBuilder.Item;
using NosCore.Packets.ClientPackets;
using NosCore.Shared.Enumerations.Items;
using NosCore.Shared.I18N;
using Serilog;
using System;
using System.Diagnostics;

namespace NosCore.GameObject.Services.ItemBuilder.Handling
{
    public class WearHandler : IHandler<Item.Item, Tuple<IItemInstance, UseItemPacket>>
    {
        private readonly ILogger _logger = Logger.GetLoggerConfiguration().CreateLogger();

        public bool Condition(Item.Item item) => item.ItemType == ItemType.Weapon
        || item.ItemType == ItemType.Jewelery
        || item.ItemType == ItemType.Armor
        || item.ItemType == ItemType.Fashion
        || item.ItemType == ItemType.Specialist;

        public void Execute(RequestData<Tuple<IItemInstance, UseItemPacket>> requestData)
        {
            requestData.ClientSession.SendPacket(requestData.ClientSession.Character.GenerateEff(123));

            var itemInstance = requestData.Data.Item1;
            var packet = requestData.Data.Item2;
            if (requestData.ClientSession.Character.InExchangeOrTrade)
            {
                return;
            }

            if (itemInstance.Item.LevelMinimum > (itemInstance.Item.IsHeroic ? requestData.ClientSession.Character.HeroLevel : requestData.ClientSession.Character.Level)
                || itemInstance.Item.Sex != 0 && itemInstance.Item.Sex != (byte)requestData.ClientSession.Character.Gender + 1
                || itemInstance.Item.Class != 0 && ((itemInstance.Item.Class >> requestData.ClientSession.Character.Class) & 1) != 1)
            {
                requestData.ClientSession.SendPacket(
                    requestData.ClientSession.Character.GenerateSay(
                        requestData.ClientSession.GetMessageFromKey(LanguageKey.BAD_EQUIPMENT), Shared.Enumerations.SayColorType.Yellow));
                return;
            }

            if (requestData.ClientSession.Character.UseSp && itemInstance.Item.EquipmentSlot == EquipmentType.Sp)
            {
                requestData.ClientSession.SendPacket(
                requestData.ClientSession.Character.GenerateSay(
                    requestData.ClientSession.GetMessageFromKey(LanguageKey.SP_BLOCKED), Shared.Enumerations.SayColorType.Yellow));
                return;
            }

            if (requestData.ClientSession.Character.JobLevel < itemInstance.Item.LevelJobMinimum)
            {
                requestData.ClientSession.SendPacket(
             requestData.ClientSession.Character.GenerateSay(
                 requestData.ClientSession.GetMessageFromKey(LanguageKey.LOW_JOB_LVL), Shared.Enumerations.SayColorType.Yellow));
                return;
            }

            requestData.ClientSession.Character.Inventory.MoveInPocket(packet.Slot, packet.Type, PocketType.Wear, (short)itemInstance.Item.EquipmentSlot, true);
            var newItem = requestData.ClientSession.Character.Inventory.LoadBySlotAndType<IItemInstance>(packet.Slot, packet.Type);

            requestData.ClientSession.SendPacket(newItem.GeneratePocketChange(packet.Type, packet.Slot));

            requestData.ClientSession.Character.MapInstance.Sessions.SendPacket(requestData.ClientSession.Character.GenerateEq());
            requestData.ClientSession.SendPacket(requestData.ClientSession.Character.GenerateEquipment());
        }
    }
}
