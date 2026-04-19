//  __  _  __    __   ___ __  ___ ___
// |  \| |/__\ /' _/ / _//__\| _ \ __|
// | | ' | \/ |`._`.| \_| \/ | v / _|
// |_|\__|\__/ |___/ \__/\__/|_|_\___|
//

using NosCore.Data.Dto;
using NosCore.GameObject.Services.BattleService;
using NosCore.GameObject.Services.InventoryService;
using NosCore.GameObject.Services.QuestService;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;

namespace NosCore.GameObject.Ecs.Components;

public record struct PlayerInventoryComponent(
    IInventoryService InventoryService,
    ConcurrentDictionary<short, CharacterSkill> Skills,
    ConcurrentDictionary<Guid, CharacterQuest> Quests,
    List<QuicklistEntryDto> QuicklistEntries,
    List<StaticBonusDto> StaticBonusList,
    List<TitleDto> Titles);
