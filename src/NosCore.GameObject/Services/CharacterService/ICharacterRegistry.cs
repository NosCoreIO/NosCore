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
using System.Reactive.Subjects;
using NodaTime;
using NosCore.Data.Dto;
using NosCore.Data.Enumerations.Group;
using NosCore.Data.StaticEntities;
using NosCore.GameObject.Networking;
using NosCore.GameObject.Services.BattleService;
using NosCore.GameObject.Services.InventoryService;
using NosCore.GameObject.Services.NRunService;
using NosCore.GameObject.Services.QuestService;
using NosCore.Networking.SessionGroup;

namespace NosCore.GameObject.Services.CharacterService;

public class CharacterGameState
{
    private readonly ISessionGroupFactory _sessionGroupFactory;

    public CharacterGameState(long characterId, AccountDto account, IInventoryService inventoryService, ISessionGroupFactory sessionGroupFactory)
    {
        CharacterId = characterId;
        Account = account;
        InventoryService = inventoryService;
        _sessionGroupFactory = sessionGroupFactory;
    }

    public long CharacterId { get; init; }
    public AccountDto Account { get; init; }
    public IInventoryService InventoryService { get; init; }

    public ConcurrentDictionary<short, CharacterSkill> Skills { get; } = new();
    public ConcurrentDictionary<Guid, CharacterQuest> Quests { get; set; } = new();
    public List<QuicklistEntryDto> QuicklistEntries { get; set; } = new();
    public List<StaticBonusDto> StaticBonusList { get; set; } = new();
    public List<TitleDto> Titles { get; set; } = new();

    public Instant LastPortal { get; set; }
    public Instant LastMove { get; set; }
    public Instant LastSp { get; set; }
    public short SpCooldown { get; set; }
    public Instant? LastGroupRequest { get; set; }

    public Group? Group { get; set; }
    public Shop? Shop { get; set; }
    public ScriptDto? Script { get; set; }
    public bool InShop { get; set; }
    public bool InExchange { get; set; }
    public bool IsChangingMapInstance { get; set; }
    public bool IsDisconnecting { get; set; }

    public ConcurrentDictionary<long, long> GroupRequestCharacterIds { get; } = new();

    public Dictionary<Type, Subject<RequestData>> Requests { get; set; } = new()
    {
        [typeof(INrunEventHandler)] = new Subject<RequestData>()
    };

    public void InitializeGroup()
    {
        if (Group != null)
        {
            return;
        }

        Group = new Group(GroupType.Group, _sessionGroupFactory);
    }
}

public interface ICharacterRegistry
{
    void Register(long characterId, CharacterGameState state);
    void Unregister(long characterId);
    CharacterGameState? GetState(long characterId);
    bool TryGetState(long characterId, out CharacterGameState? state);
    IEnumerable<CharacterGameState> GetAll();
}
