﻿//  __  _  __    __   ___ __  ___ ___
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

using DotNetty.Transport.Channels;
using NosCore.Data.Dto;
using NosCore.Data.Enumerations.I18N;
using NosCore.Data.StaticEntities;
using NosCore.Data.WebApi;
using NosCore.GameObject.Services.InventoryService;
using NosCore.GameObject.Services.QuestService;
using NosCore.Packets.Enumerations;
using NosCore.Packets.Interfaces;
using NosCore.Shared.Enumerations;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace NosCore.GameObject.ComponentEntities.Interfaces
{
    public interface ICharacterEntity : INamedEntity, IRequestableEntity
    {
        bool FriendRequestBlocked { get; set; }

        AuthorityType Authority { get; }

        short MapId { get; set; }

        GenderType Gender { get; }

        HairStyleType HairStyle { get; }

        HairColorType HairColor { get; }

        CharacterClassType Class { get; }

        ReputationType ReputIcon { get; }

        DignityType DignityIcon { get; }

        bool Camouflage { get; }

        bool Invisible { get; }

        IChannel? Channel { get; }

        bool GroupRequestBlocked { get; }

        ConcurrentDictionary<long, long> GroupRequestCharacterIds { get; }

        long Gold { get; }

        long BankGold { get; }

        IInventoryService InventoryService { get; }

        RegionType AccountLanguage { get; }

        List<StaticBonusDto> StaticBonusList { get; set; }

        List<TitleDto> Titles { get; set; }

        bool IsDisconnecting { get; }
        ScriptDto? Script { get; set; }
        Guid? CurrentScriptId { get; set; }
        ConcurrentDictionary<Guid, CharacterQuest> Quests { get; set; }
        short Compliment { get; }
        long Reput { get; set; }
        short Dignity { get; set; }

        Task GenerateMailAsync(IEnumerable<MailData> data);

        Task SendPacketAsync(IPacket packetDefinition);

        Task SendPacketsAsync(IEnumerable<IPacket> packetDefinitions);

        Task LeaveGroupAsync();

        void JoinGroup(Group group);

        Task SaveAsync();

        Task SetJobLevelAsync(byte level);

        Task SetHeroLevelAsync(byte level);

        Task SetReputationAsync(long reput);

        Task SetGoldAsync(long gold);

        Task AddGoldAsync(long gold);

        Task RemoveGoldAsync(long gold);

        void AddBankGold(long bankGold);

        void RemoveBankGold(long bankGold);

        Task ChangeClassAsync(CharacterClassType classType);

        Task ChangeMapAsync(short mapId, short mapX, short mapY);

        string GetMessageFromKey(LanguageKey support);

        Dictionary<Guid, CharacterBattlepassDto> BattlepassLogs { get; set; }
    }
}
