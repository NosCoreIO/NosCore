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
using System.Collections.Concurrent;
using System.Collections.Generic;
using DotNetty.Transport.Channels;
using NosCore.Data.Enumerations;
using NosCore.Data.Enumerations.Account;
using NosCore.GameObject.Providers.InventoryService;
using ChickenAPI.Packets.Interfaces;
using ChickenAPI.Packets.Enumerations;
using ChickenAPI.Packets.ServerPackets.Visibility;
using ChickenAPI.Packets.ServerPackets.Inventory;
using NosCore.Data;

namespace NosCore.GameObject.ComponentEntities.Interfaces
{
    public interface ICharacterEntity : INamedEntity, IRequestableEntity
    {
        bool FriendRequestBlocked { get; set; }
        AuthorityType Authority { get; }

        GenderType Gender { get; }

        HairStyleType HairStyle { get; }

        HairColorType HairColor { get; }

        CharacterClassType Class { get; }

        InEquipmentSubPacket Equipment { get; }

        int ReputIcon { get; }

        int DignityIcon { get; }

        bool Camouflage { get; }

        bool Invisible { get; }

        IChannel Channel { get; }

        bool GroupRequestBlocked { get; }

        ConcurrentDictionary<long, long> GroupRequestCharacterIds { get; }
        UpgradeRareSubPacket WeaponUpgradeRareSubPacket { get; }
        UpgradeRareSubPacket ArmorUpgradeRareSubPacket { get; }

        long Gold { get; }

        long BankGold { get; }

        IInventoryService Inventory { get; }

        RegionType AccountLanguage { get; }

        void SendPacket(IPacket packetDefinition);

        void SendPackets(IEnumerable<IPacket> packetDefinitions);

        void LeaveGroup();

        void JoinGroup(Group group);

        void Save();

        void SetJobLevel(byte level);

        void SetHeroLevel(byte level);

        void SetReputation(long reput);

        void SetGold(long gold);

        void AddGold(long gold);

        void RemoveGold(long gold);

        void AddBankGold(long bankGold);

        void RemoveBankGold(long bankGold);

        List<StaticBonusDto> StaticBonusList { get; set; }

        void ChangeClass(CharacterClassType classType);
    }
}