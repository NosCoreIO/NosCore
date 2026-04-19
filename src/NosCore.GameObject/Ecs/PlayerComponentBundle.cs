using NosCore.Data.Enumerations.I18N;
using NosCore.GameObject.Ecs.Interfaces;
using NosCore.GameObject.Ecs.Attributes;
using NosCore.GameObject.Ecs.Components;
using NosCore.Packets.Interfaces;
using NosCore.Shared.Enumerations;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace NosCore.GameObject.Ecs;

[ComponentBundle(
    typeof(EntityIdentityComponent),
    typeof(HealthComponent),
    typeof(ManaComponent),
    typeof(PositionComponent),
    typeof(VisualComponent),
    typeof(AppearanceComponent),
    typeof(ExperienceComponent),
    typeof(GoldComponent),
    typeof(ReputationComponent),
    typeof(SpComponent),
    typeof(NameComponent),
    typeof(CombatComponent),
    typeof(PlayerComponent),
    typeof(PlayerFlagsComponent),
    typeof(TimingComponent),
    typeof(SpeedComponent),
    typeof(PlayerStateComponent),
    typeof(PlayerNetworkComponent),
    typeof(PlayerContextComponent),
    typeof(PlayerInventoryComponent),
    typeof(PlayerSocialComponent),
    typeof(PlayerRequestsComponent)
)]
public readonly partial struct PlayerComponentBundle : ICharacterEntity
{
    public Arch.Core.Entity Handle => Entity;
    public long CharacterId => PlayerCharacterId;
    public bool InExchangeOrShop => InShop || InExchange;

    public long HeroXp
    {
        get => HeroLevelXp;
        set => HeroLevelXp = value;
    }

    public long Reput
    {
        get => Reputation;
        set => Reputation = value;
    }

    public RegionType AccountLanguage => Account.Language;

    // IVisualEntity - VisualId and VisualType are generated
    public short VNum => 0;

    public short Race => (short)Class;

    // INamedEntity - LevelXp is generated

    // ICharacterEntity
    byte? ICharacterEntity.VehicleSpeed => VehicleSpeed;

    public short MapId
    {
        get => CharacterDto.MapId;
        set => CharacterDto.MapId = value;
    }

    public short MapX
    {
        get => CharacterDto.MapX;
        set => CharacterDto.MapX = value;
    }

    public short MapY
    {
        get => CharacterDto.MapY;
        set => CharacterDto.MapY = value;
    }

    public byte Slot => CharacterDto.Slot;

    public Guid? CurrentScriptId => Script?.Id;

    public long BankGold
    {
        get => Account.BankMoney;
        set => Account.BankMoney = value;
    }

    public string? Prefix => null;

    ReputationType ICharacterEntity.ReputIcon => ReputationLevels.FromReputation(Reputation);
    DignityType ICharacterEntity.DignityIcon => DignityLevels.FromDignity(Dignity);

    public int ReputIconValue => (int)ReputationLevels.FromReputation(Reputation);

    public Task SendPacketAsync(IPacket? packet)
    {
        return Sender?.SendPacketAsync(packet) ?? Task.CompletedTask;
    }

    public Task SendPacketsAsync(IEnumerable<IPacket?> packets)
    {
        return Sender?.SendPacketsAsync(packets) ?? Task.CompletedTask;
    }

    public string GetMessageFromKey(LanguageKey languageKey)
    {
        return GameLanguageLocalizer[languageKey, Account.Language];
    }
}
