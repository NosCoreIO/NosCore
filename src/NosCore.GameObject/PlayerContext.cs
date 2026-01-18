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
using Arch.Core;
using NodaTime;
using NosCore.Data.Dto;
using NosCore.Data.Enumerations;
using NosCore.Data.StaticEntities;
using NosCore.GameObject.Ecs;
using NosCore.GameObject.Ecs.Components;
using NosCore.GameObject.Networking;
using NosCore.GameObject.Services.BattleService;
using NosCore.GameObject.Services.CharacterService;
using NosCore.GameObject.Services.InventoryService;
using NosCore.GameObject.Services.MapInstanceGenerationService;
using NosCore.GameObject.Services.NRunService;
using NosCore.GameObject.Services.QuestService;
using NosCore.Networking;
using NosCore.Packets.Enumerations;
using NosCore.Shared.Enumerations;

namespace NosCore.GameObject;

public readonly struct PlayerContext
{
    public Entity Entity { get; }
    public MapWorld World { get; }
    public CharacterGameState GameState { get; }
    public CharacterDto CharacterData { get; }
    public MapInstance MapInstance { get; }
    public IChannel? Channel { get; }

    public PlayerContext(Entity entity, MapInstance mapInstance, CharacterGameState gameState, CharacterDto characterData, IChannel? channel = null)
    {
        Entity = entity;
        World = mapInstance.EcsWorld;
        MapInstance = mapInstance;
        GameState = gameState;
        CharacterData = characterData;
        Channel = channel;
    }

    // Identity properties
    public long CharacterId => Entity.GetVisualId(World);
    public long AccountId => Entity.GetAccountId(World);
    public VisualType VisualType => VisualType.Player;
    public short VNum => 0;
    public long VisualId => CharacterId;
    public Guid MapInstanceId => MapInstance.MapInstanceId;

    // Position properties (delegated to ECS)
    public short PositionX => Entity.GetPositionX(World);
    public short PositionY => Entity.GetPositionY(World);
    public byte Direction => Entity.GetDirection(World);

    public void SetPosition(short x, short y) => Entity.SetPosition(World, x, y);
    public void SetDirection(byte direction) => Entity.SetDirection(World, direction);

    // Save position (for persistence)
    public short MapX
    {
        get => CharacterData.MapX;
        set => CharacterData.MapX = value;
    }

    public short MapY
    {
        get => CharacterData.MapY;
        set => CharacterData.MapY = value;
    }

    public short MapId
    {
        get => CharacterData.MapId;
        set => CharacterData.MapId = value;
    }

    // Health/Mana properties
    public int Hp => Entity.GetHp(World);
    public int MaxHp => Entity.GetMaxHp(World);
    public int Mp => Entity.GetMp(World);
    public int MaxMp => Entity.GetMaxMp(World);
    public bool IsAlive => Entity.GetIsAlive(World);

    public void SetHp(int hp) => Entity.SetHp(World, hp);
    public void SetMp(int mp) => Entity.SetMp(World, mp);
    public void SetIsAlive(bool isAlive) => Entity.SetIsAlive(World, isAlive);

    // Experience/Level properties
    public byte Level => Entity.GetCharacterLevel(World);
    public long LevelXp => Entity.GetLevelXp(World);
    public byte JobLevel => Entity.GetJobLevel(World);
    public long JobLevelXp => Entity.GetJobLevelXp(World);
    public byte HeroLevel => Entity.GetHeroLevel(World);
    public long HeroXp => Entity.GetHeroXp(World);

    public void SetLevel(byte level) => Entity.SetCharacterLevel(World, level);
    public void SetLevelXp(long xp) => Entity.SetLevelXp(World, xp);
    public void SetJobLevel(byte level) => Entity.SetJobLevel(World, level);
    public void SetJobLevelXp(long xp) => Entity.SetJobLevelXp(World, xp);
    public void SetHeroLevel(byte level) => Entity.SetHeroLevel(World, level);
    public void SetHeroXp(long xp) => Entity.SetHeroXp(World, xp);

    // Appearance properties
    public CharacterClassType Class => Entity.GetClass(World);
    public GenderType Gender => Entity.GetGender(World);
    public HairStyleType HairStyle => Entity.GetHairStyle(World);
    public HairColorType HairColor => Entity.GetHairColor(World);

    public void SetClass(CharacterClassType characterClass) => Entity.SetClass(World, characterClass);
    public void SetGender(GenderType gender) => Entity.SetGender(World, gender);

    // Visual properties
    public byte Size => Entity.GetSize(World);
    public short Morph => Entity.GetMorph(World);
    public byte MorphUpgrade => Entity.GetMorphUpgrade(World);
    public short MorphDesign => Entity.GetMorphDesign(World);
    public byte MorphBonus => Entity.GetMorphBonus(World);
    public bool IsSitting => Entity.GetIsSitting(World);
    public bool Invisible => Entity.GetInvisible(World);
    public bool Camouflage => Entity.GetCamouflage(World);
    public bool UseSp => Entity.GetUseSp(World);
    public bool IsVehicled => Entity.GetIsVehicled(World);
    public byte? VehicleSpeed => Entity.GetVehicleSpeed(World);

    public void SetSize(byte size) => Entity.SetSize(World, size);
    public void SetIsSitting(bool isSitting) => Entity.SetIsSitting(World, isSitting);
    public void SetInvisible(bool invisible) => Entity.SetInvisible(World, invisible);
    public void SetCamouflage(bool camouflage) => Entity.SetCamouflage(World, camouflage);
    public void SetUseSp(bool useSp) => Entity.SetUseSp(World, useSp);
    public void SetIsVehicled(bool isVehicled) => Entity.SetIsVehicled(World, isVehicled);
    public void SetVehicleSpeed(byte? vehicleSpeed) => Entity.SetVehicleSpeed(World, vehicleSpeed);
    public void SetMorph(short morph, byte morphUpgrade, short morphDesign, byte morphBonus)
        => Entity.SetMorph(World, morph, morphUpgrade, morphDesign, morphBonus);
    public void SetEffect(short effect, short effectDelay) => Entity.SetEffect(World, effect, effectDelay);

    // Movement properties
    public byte Speed => Entity.GetSpeed(World);
    public bool NoMove => Entity.GetNoMove(World);
    public bool NoAttack => Entity.GetNoAttack(World);
    public bool CanFight => Entity.GetCanFight(World);

    public void SetSpeed(byte speed) => Entity.SetSpeed(World, speed);
    public void SetNoMove(bool noMove) => Entity.SetNoMove(World, noMove);
    public void SetNoAttack(bool noAttack) => Entity.SetNoAttack(World, noAttack);
    public void SetCanFight(bool canFight) => Entity.SetCanFight(World, canFight);

    // Gold/Reputation properties
    public long Gold => Entity.GetGold(World);
    public long Reput => Entity.GetReput(World);
    public short Dignity => Entity.GetDignity(World);

    public void SetGold(long gold) => Entity.SetGold(World, gold);
    public void SetReput(long reput) => Entity.SetReput(World, reput);
    public void SetDignity(short dignity) => Entity.SetDignity(World, dignity);

    // SP properties
    public int SpPoint => Entity.GetSpPoint(World);
    public int SpAdditionPoint => Entity.GetSpAdditionPoint(World);
    public short Compliment => Entity.GetCompliment(World);

    public void SetSpPoint(int spPoint) => Entity.SetSpPoint(World, spPoint);
    public void SetSpAdditionPoint(int spAdditionPoint) => Entity.SetSpAdditionPoint(World, spAdditionPoint);
    public void SetCompliment(short compliment) => Entity.SetCompliment(World, compliment);

    // Name properties
    public string Name => Entity.GetName(World);
    public string? Prefix => Entity.GetPrefix(World);
    public AuthorityType Authority => Entity.GetAuthority(World);

    // Combat state
    public CombatState? CombatState => Entity.GetCombatState(World);

    // Character game state properties (delegated to GameState)
    public ConcurrentDictionary<short, CharacterSkill> Skills => GameState.Skills;
    public ConcurrentDictionary<Guid, CharacterQuest> Quests
    {
        get => GameState.Quests;
        set => GameState.Quests = value;
    }
    public List<QuicklistEntryDto> QuicklistEntries
    {
        get => GameState.QuicklistEntries;
        set => GameState.QuicklistEntries = value;
    }
    public List<StaticBonusDto> StaticBonusList
    {
        get => GameState.StaticBonusList;
        set => GameState.StaticBonusList = value;
    }
    public List<TitleDto> Titles
    {
        get => GameState.Titles;
        set => GameState.Titles = value;
    }
    public IInventoryService InventoryService => GameState.InventoryService;
    public AccountDto Account => GameState.Account;
    public Group? Group
    {
        get => GameState.Group;
        set => GameState.Group = value;
    }
    public Shop? Shop
    {
        get => GameState.Shop;
        set => GameState.Shop = value;
    }
    public ScriptDto? Script
    {
        get => GameState.Script;
        set => GameState.Script = value;
    }
    public ConcurrentDictionary<long, long> GroupRequestCharacterIds => GameState.GroupRequestCharacterIds;
    public Dictionary<Type, Subject<RequestData>> Requests
    {
        get => GameState.Requests;
        set => GameState.Requests = value;
    }

    // Timing properties (delegated to GameState)
    public Instant LastPortal
    {
        get => GameState.LastPortal;
        set => GameState.LastPortal = value;
    }
    public Instant LastMove
    {
        get => GameState.LastMove;
        set => GameState.LastMove = value;
    }
    public Instant LastSp
    {
        get => GameState.LastSp;
        set => GameState.LastSp = value;
    }
    public short SpCooldown
    {
        get => GameState.SpCooldown;
        set => GameState.SpCooldown = value;
    }
    public Instant? LastGroupRequest
    {
        get => GameState.LastGroupRequest;
        set => GameState.LastGroupRequest = value;
    }

    // Flags (delegated to GameState)
    public bool IsChangingMapInstance
    {
        get => GameState.IsChangingMapInstance;
        set => GameState.IsChangingMapInstance = value;
    }
    public bool InShop
    {
        get => GameState.InShop;
        set => GameState.InShop = value;
    }
    public bool InExchange
    {
        get => GameState.InExchange;
        set => GameState.InExchange = value;
    }
    public bool InExchangeOrShop => InShop || InExchange;
    public bool IsDisconnecting
    {
        get => GameState.IsDisconnecting;
        set => GameState.IsDisconnecting = value;
    }

    // Derived properties
    public long BankGold => Account.BankMoney;
    public RegionType AccountLanguage => Account.Language;

    // Utility methods
    public void LoadExpensions()
    {
        InventoryService.LoadExpensions(StaticBonusList);
    }

    // Packet generation methods
    public NosCore.Packets.ServerPackets.Player.StatPacket GenerateStat()
    {
        return new NosCore.Packets.ServerPackets.Player.StatPacket
        {
            Hp = Hp,
            HpMaximum = MaxHp,
            Mp = Mp,
            MpMaximum = MaxMp,
            Unknown = 0,
            Option = 0
        };
    }

    public NosCore.Packets.ServerPackets.Specialists.SpPacket GenerateSpPoint(int maxSpPoints, int maxAdditionalSpPoints)
    {
        return new NosCore.Packets.ServerPackets.Specialists.SpPacket
        {
            AdditionalPoint = SpAdditionPoint,
            MaxAdditionalPoint = maxAdditionalSpPoints,
            SpPoint = SpPoint,
            MaxSpPoint = maxSpPoints
        };
    }
}
