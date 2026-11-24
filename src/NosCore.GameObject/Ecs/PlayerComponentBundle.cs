using NosCore.GameObject.Ecs.Interfaces;
using NosCore.GameObject.Ecs.Extensions;
using NosCore.GameObject.Ecs.Attributes;
using NosCore.GameObject.Ecs.Components;
using NosCore.Packets.Interfaces;
using NosCore.Shared.Enumerations;
using System;
using System.Collections.Generic;
using System.Linq;
using NosCore.Data.Dto;
using NosCore.Data.Enumerations.Buff;
using System.Threading.Tasks;
using NosCore.Data.Enumerations;
using NosCore.GameObject.Services.InventoryService;
using NosCore.Packets.Enumerations;

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
    typeof(BuffStateComponent),
    typeof(PlayerComponent),
    typeof(PlayerFlagsComponent),
    typeof(TimingComponent),
    typeof(SpeedComponent),
    typeof(PlayerStateComponent),
    typeof(PlayerNetworkComponent),
    typeof(PlayerContextComponent),
    typeof(PlayerInventoryComponent),
    typeof(PlayerSocialComponent),
    typeof(PlayerMatesComponent),
    typeof(DigestionComponent),
    typeof(PendingDelayComponent),
    typeof(WalkAllowanceComponent),
    typeof(WalkTrailComponent),
    typeof(NpcDialogComponent),
    typeof(ListedTargetsComponent),
    typeof(MinigameSessionComponent),
    typeof(HitListComponent),
    typeof(BaseMapPositionComponent),
    typeof(SkillResetComponent),
    typeof(FactionComponent),
    typeof(SkillComboComponent),
    typeof(WarehouseWindowComponent),
    typeof(ChargeComponent),
    typeof(FalconComponent)
)]
public readonly partial struct PlayerComponentBundle : INamedEntity
{
    public Arch.Core.Entity Handle => Entity;
    public long CharacterId => PlayerCharacterId;
    public bool InExchange => Exchange != null;
    public bool InExchangeOrShop => InShop || InExchange;
    public double StatOption => this.GenerateStatOption();

    public RegionType AccountLanguage => Account.Language;

    // IVisualEntity - VisualId and VisualType are generated
    public short VNum => 0;

    public short Race => (short)Class;

    // INamedEntity - LevelXp is generated

    public byte Slot => CharacterDto.Slot;

    public Guid? CurrentScriptId => Script?.Id;

    public string? Prefix => null;

    public ReputationType ReputIcon => Ladders.FromReputation(Reputation);
    public DignityType DignityIcon => Ladders.FromDignity(Dignity);

    public int ReputIconValue => (int)ReputIcon;

    public StaticBonusDto? BazaarMedal => StaticBonusList.FirstOrDefault(s =>
        s.StaticBonusType is StaticBonusType.BazaarMedalGold or StaticBonusType.BazaarMedalSilver);

    public bool HasBankVip => StaticBonusList.Any(s => s.StaticBonusType == StaticBonusType.CuarryBankVip);

    public bool HasFamilyExperienceBooster =>
        StaticBonusList.Any(s => s.StaticBonusType == StaticBonusType.FamilyExperienceBooster);

    public short VisibleTitle => Titles.FirstOrDefault(s => s.Visible)?.TitleType ?? 0;

    // A mount or a costume scroll's monster replaces the sprite just as a specialist card does.
    public short ShownMorph => UseSp || IsVehicled || InMonsterForm ? Morph : (short)0;

    // A worn wig is drawn in its own colour instead of the hair's.
    public HairColorType ShownHairColor
    {
        get
        {
            var hat = InventoryService.LoadBySlotAndType(EquipmentType.Hat, NoscorePocketType.Wear);
            return hat?.ItemInstance.Item.IsColored == true ? (HairColorType)hat.ItemInstance.Design : HairColor;
        }
    }

    public Task SendPacketAsync(IPacket? packet)
    {
        return Sender?.SendPacketAsync(packet) ?? Task.CompletedTask;
    }

    public Task SendPacketsAsync(IEnumerable<IPacket?> packets)
    {
        return Sender?.SendPacketsAsync(packets) ?? Task.CompletedTask;
    }
}
