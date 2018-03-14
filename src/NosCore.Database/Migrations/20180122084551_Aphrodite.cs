using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Migrations;
using System;
using System.Collections.Generic;

namespace NosCore.Database.Migrations
{
    public partial class Aphrodite : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Account",
                columns: table => new
                {
                    AccountId = table.Column<long>(nullable: false)
                        .Annotation("SqlServer:ValueGenerationStrategy", SqlServerValueGenerationStrategy.IdentityColumn),
                    Authority = table.Column<short>(nullable: false),
                    Email = table.Column<string>(maxLength: 255, nullable: true),
                    Name = table.Column<string>(maxLength: 255, nullable: true),
                    Password = table.Column<string>(unicode: false, maxLength: 255, nullable: true),
                    RegistrationIP = table.Column<string>(maxLength: 45, nullable: true),
                    VerificationToken = table.Column<string>(maxLength: 32, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Account", x => x.AccountId);
                });

            migrationBuilder.CreateTable(
                name: "Card",
                columns: table => new
                {
                    CardId = table.Column<short>(nullable: false),
                    BuffType = table.Column<byte>(nullable: false),
                    Delay = table.Column<int>(nullable: false),
                    Duration = table.Column<int>(nullable: false),
                    EffectId = table.Column<int>(nullable: false),
                    Level = table.Column<byte>(nullable: false),
                    Name = table.Column<string>(maxLength: 255, nullable: true),
                    Propability = table.Column<byte>(nullable: false),
                    TimeoutBuff = table.Column<short>(nullable: false),
                    TimeoutBuffChance = table.Column<byte>(nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Card", x => x.CardId);
                });

            migrationBuilder.CreateTable(
                name: "Family",
                columns: table => new
                {
                    FamilyId = table.Column<long>(nullable: false)
                        .Annotation("SqlServer:ValueGenerationStrategy", SqlServerValueGenerationStrategy.IdentityColumn),
                    FamilyExperience = table.Column<int>(nullable: false),
                    FamilyFaction = table.Column<byte>(nullable: false),
                    FamilyHeadGender = table.Column<byte>(nullable: false),
                    FamilyLevel = table.Column<byte>(nullable: false),
                    FamilyMessage = table.Column<string>(maxLength: 255, nullable: true),
                    ManagerAuthorityType = table.Column<byte>(nullable: false),
                    ManagerCanGetHistory = table.Column<bool>(nullable: false),
                    ManagerCanInvite = table.Column<bool>(nullable: false),
                    ManagerCanNotice = table.Column<bool>(nullable: false),
                    ManagerCanShout = table.Column<bool>(nullable: false),
                    MaxSize = table.Column<byte>(nullable: false),
                    MemberAuthorityType = table.Column<byte>(nullable: false),
                    MemberCanGetHistory = table.Column<bool>(nullable: false),
                    Name = table.Column<string>(maxLength: 255, nullable: true),
                    WarehouseSize = table.Column<byte>(nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Family", x => x.FamilyId);
                });

            migrationBuilder.CreateTable(
                name: "Item",
                columns: table => new
                {
                    VNum = table.Column<short>(nullable: false),
                    BasicUpgrade = table.Column<byte>(nullable: false),
                    CellonLvl = table.Column<byte>(nullable: false),
                    Class = table.Column<byte>(nullable: false),
                    CloseDefence = table.Column<short>(nullable: false),
                    Color = table.Column<byte>(nullable: false),
                    Concentrate = table.Column<short>(nullable: false),
                    CriticalLuckRate = table.Column<byte>(nullable: false),
                    CriticalRate = table.Column<short>(nullable: false),
                    DamageMaximum = table.Column<short>(nullable: false),
                    DamageMinimum = table.Column<short>(nullable: false),
                    DarkElement = table.Column<byte>(nullable: false),
                    DarkResistance = table.Column<short>(nullable: false),
                    DefenceDodge = table.Column<short>(nullable: false),
                    DistanceDefence = table.Column<short>(nullable: false),
                    DistanceDefenceDodge = table.Column<short>(nullable: false),
                    Effect = table.Column<short>(nullable: false),
                    EffectValue = table.Column<int>(nullable: false),
                    Element = table.Column<byte>(nullable: false),
                    ElementRate = table.Column<short>(nullable: false),
                    EquipmentSlot = table.Column<byte>(nullable: false),
                    FireElement = table.Column<byte>(nullable: false),
                    FireResistance = table.Column<short>(nullable: false),
                    Flag1 = table.Column<bool>(nullable: false),
                    Flag2 = table.Column<bool>(nullable: false),
                    Flag3 = table.Column<bool>(nullable: false),
                    Flag4 = table.Column<bool>(nullable: false),
                    Flag5 = table.Column<bool>(nullable: false),
                    Flag6 = table.Column<bool>(nullable: false),
                    Flag7 = table.Column<bool>(nullable: false),
                    Flag8 = table.Column<bool>(nullable: false),
                    Flag9 = table.Column<bool>(nullable: false),
                    Height = table.Column<byte>(nullable: false),
                    HitRate = table.Column<short>(nullable: false),
                    Hp = table.Column<short>(nullable: false),
                    HpRegeneration = table.Column<short>(nullable: false),
                    IsColored = table.Column<bool>(nullable: false),
                    IsConsumable = table.Column<bool>(nullable: false),
                    IsDroppable = table.Column<bool>(nullable: false),
                    IsHeroic = table.Column<bool>(nullable: false),
                    IsMinilandActionable = table.Column<bool>(nullable: false),
                    IsSoldable = table.Column<bool>(nullable: false),
                    IsTradable = table.Column<bool>(nullable: false),
                    IsWarehouse = table.Column<bool>(nullable: false),
                    ItemSubType = table.Column<byte>(nullable: false),
                    ItemType = table.Column<byte>(nullable: false),
                    ItemValidTime = table.Column<long>(nullable: false),
                    LevelJobMinimum = table.Column<byte>(nullable: false),
                    LevelMinimum = table.Column<byte>(nullable: false),
                    LightElement = table.Column<byte>(nullable: false),
                    LightResistance = table.Column<short>(nullable: false),
                    MagicDefence = table.Column<short>(nullable: false),
                    MaxCellon = table.Column<byte>(nullable: false),
                    MaxCellonLvl = table.Column<byte>(nullable: false),
                    MaxElementRate = table.Column<short>(nullable: false),
                    MaximumAmmo = table.Column<byte>(nullable: false),
                    MinilandObjectPoint = table.Column<int>(nullable: false),
                    MoreHp = table.Column<short>(nullable: false),
                    MoreMp = table.Column<short>(nullable: false),
                    Morph = table.Column<short>(nullable: false),
                    Mp = table.Column<short>(nullable: false),
                    MpRegeneration = table.Column<short>(nullable: false),
                    Name = table.Column<string>(maxLength: 255, nullable: true),
                    Price = table.Column<long>(nullable: false),
                    PvpDefence = table.Column<short>(nullable: false),
                    PvpStrength = table.Column<byte>(nullable: false),
                    ReduceOposantResistance = table.Column<short>(nullable: false),
                    ReputPrice = table.Column<long>(nullable: false),
                    ReputationMinimum = table.Column<byte>(nullable: false),
                    SecondaryElement = table.Column<byte>(nullable: false),
                    Sex = table.Column<byte>(nullable: false),
                    SpType = table.Column<byte>(nullable: false),
                    Speed = table.Column<byte>(nullable: false),
                    Type = table.Column<byte>(nullable: false),
                    WaitDelay = table.Column<short>(nullable: false),
                    WaterElement = table.Column<byte>(nullable: false),
                    WaterResistance = table.Column<short>(nullable: false),
                    Width = table.Column<byte>(nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Item", x => x.VNum);
                });

            migrationBuilder.CreateTable(
                name: "Map",
                columns: table => new
                {
                    MapId = table.Column<short>(nullable: false),
                    Data = table.Column<byte[]>(nullable: true),
                    Music = table.Column<int>(nullable: false),
                    Name = table.Column<string>(maxLength: 255, nullable: true),
                    ShopAllowed = table.Column<bool>(nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Map", x => x.MapId);
                });

            migrationBuilder.CreateTable(
                name: "NpcMonster",
                columns: table => new
                {
                    NpcMonsterVNum = table.Column<short>(nullable: false),
                    AmountRequired = table.Column<byte>(nullable: false),
                    AttackClass = table.Column<byte>(nullable: false),
                    AttackUpgrade = table.Column<byte>(nullable: false),
                    BasicArea = table.Column<byte>(nullable: false),
                    BasicCooldown = table.Column<short>(nullable: false),
                    BasicRange = table.Column<byte>(nullable: false),
                    BasicSkill = table.Column<short>(nullable: false),
                    CloseDefence = table.Column<short>(nullable: false),
                    Concentrate = table.Column<short>(nullable: false),
                    CriticalChance = table.Column<byte>(nullable: false),
                    CriticalRate = table.Column<short>(nullable: false),
                    DamageMaximum = table.Column<short>(nullable: false),
                    DamageMinimum = table.Column<short>(nullable: false),
                    DarkResistance = table.Column<short>(nullable: false),
                    DefenceDodge = table.Column<short>(nullable: false),
                    DefenceUpgrade = table.Column<byte>(nullable: false),
                    DistanceDefence = table.Column<short>(nullable: false),
                    DistanceDefenceDodge = table.Column<short>(nullable: false),
                    Element = table.Column<byte>(nullable: false),
                    ElementRate = table.Column<short>(nullable: false),
                    FireResistance = table.Column<short>(nullable: false),
                    GiveDamagePercentage = table.Column<int>(nullable: false),
                    HeroLevel = table.Column<byte>(nullable: false),
                    HeroXP = table.Column<int>(nullable: false),
                    IsHostile = table.Column<bool>(nullable: false),
                    IsPercent = table.Column<bool>(nullable: false),
                    JobXP = table.Column<int>(nullable: false),
                    Level = table.Column<byte>(nullable: false),
                    LightResistance = table.Column<short>(nullable: false),
                    MagicDefence = table.Column<short>(nullable: false),
                    MaxHP = table.Column<int>(nullable: false),
                    MaxMP = table.Column<int>(nullable: false),
                    MonsterType = table.Column<byte>(nullable: false),
                    Name = table.Column<string>(maxLength: 255, nullable: true),
                    NoAggresiveIcon = table.Column<bool>(nullable: false),
                    NoticeRange = table.Column<byte>(nullable: false),
                    Race = table.Column<byte>(nullable: false),
                    RaceType = table.Column<byte>(nullable: false),
                    RespawnTime = table.Column<int>(nullable: false),
                    Speed = table.Column<byte>(nullable: false),
                    TakeDamages = table.Column<int>(nullable: false),
                    VNumRequired = table.Column<short>(nullable: false),
                    WaterResistance = table.Column<short>(nullable: false),
                    XP = table.Column<int>(nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_NpcMonster", x => x.NpcMonsterVNum);
                });

            migrationBuilder.CreateTable(
                name: "Quest",
                columns: table => new
                {
                    QuestId = table.Column<short>(nullable: false),
                    EndDialogId = table.Column<int>(nullable: true),
                    InfoId = table.Column<int>(nullable: false),
                    IsDaily = table.Column<bool>(nullable: false),
                    LevelMax = table.Column<byte>(nullable: false),
                    LevelMin = table.Column<byte>(nullable: false),
                    NextQuestId = table.Column<long>(nullable: true),
                    QuestType = table.Column<int>(nullable: false),
                    SpecialData = table.Column<int>(nullable: true),
                    StartDialogId = table.Column<int>(nullable: true),
                    TargetMap = table.Column<short>(nullable: true),
                    TargetX = table.Column<short>(nullable: true),
                    TargetY = table.Column<short>(nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Quest", x => x.QuestId);
                });

            migrationBuilder.CreateTable(
                name: "Skill",
                columns: table => new
                {
                    SkillVNum = table.Column<short>(nullable: false),
                    AttackAnimation = table.Column<short>(nullable: false),
                    CPCost = table.Column<byte>(nullable: false),
                    CastAnimation = table.Column<short>(nullable: false),
                    CastEffect = table.Column<short>(nullable: false),
                    CastId = table.Column<short>(nullable: false),
                    CastTime = table.Column<short>(nullable: false),
                    Class = table.Column<byte>(nullable: false),
                    Cooldown = table.Column<short>(nullable: false),
                    Duration = table.Column<short>(nullable: false),
                    Effect = table.Column<short>(nullable: false),
                    Element = table.Column<byte>(nullable: false),
                    HitType = table.Column<byte>(nullable: false),
                    ItemVNum = table.Column<short>(nullable: false),
                    Level = table.Column<byte>(nullable: false),
                    LevelMinimum = table.Column<byte>(nullable: false),
                    MinimumAdventurerLevel = table.Column<byte>(nullable: false),
                    MinimumArcherLevel = table.Column<byte>(nullable: false),
                    MinimumMagicianLevel = table.Column<byte>(nullable: false),
                    MinimumSwordmanLevel = table.Column<byte>(nullable: false),
                    MpCost = table.Column<short>(nullable: false),
                    Name = table.Column<string>(maxLength: 255, nullable: true),
                    Price = table.Column<int>(nullable: false),
                    Range = table.Column<byte>(nullable: false),
                    SkillType = table.Column<byte>(nullable: false),
                    TargetRange = table.Column<byte>(nullable: false),
                    TargetType = table.Column<byte>(nullable: false),
                    Type = table.Column<byte>(nullable: false),
                    UpgradeSkill = table.Column<short>(nullable: false),
                    UpgradeType = table.Column<short>(nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Skill", x => x.SkillVNum);
                });

            migrationBuilder.CreateTable(
                name: "PenaltyLog",
                columns: table => new
                {
                    PenaltyLogId = table.Column<int>(nullable: false)
                        .Annotation("SqlServer:ValueGenerationStrategy", SqlServerValueGenerationStrategy.IdentityColumn),
                    AccountId = table.Column<long>(nullable: false),
                    AdminName = table.Column<string>(nullable: true),
                    DateEnd = table.Column<DateTime>(nullable: false),
                    DateStart = table.Column<DateTime>(nullable: false),
                    Penalty = table.Column<byte>(nullable: false),
                    Reason = table.Column<string>(maxLength: 255, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PenaltyLog", x => x.PenaltyLogId);
                    table.ForeignKey(
                        name: "FK_PenaltyLog_Account_AccountId",
                        column: x => x.AccountId,
                        principalTable: "Account",
                        principalColumn: "AccountId",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "FamilyLog",
                columns: table => new
                {
                    FamilyLogId = table.Column<long>(nullable: false)
                        .Annotation("SqlServer:ValueGenerationStrategy", SqlServerValueGenerationStrategy.IdentityColumn),
                    FamilyId = table.Column<long>(nullable: false),
                    FamilyLogData = table.Column<string>(maxLength: 255, nullable: true),
                    FamilyLogType = table.Column<byte>(nullable: false),
                    Timestamp = table.Column<DateTime>(nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_FamilyLog", x => x.FamilyLogId);
                    table.ForeignKey(
                        name: "FK_FamilyLog_Family_FamilyId",
                        column: x => x.FamilyId,
                        principalTable: "Family",
                        principalColumn: "FamilyId",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "RollGeneratedItem",
                columns: table => new
                {
                    RollGeneratedItemId = table.Column<short>(nullable: false)
                        .Annotation("SqlServer:ValueGenerationStrategy", SqlServerValueGenerationStrategy.IdentityColumn),
                    IsRareRandom = table.Column<bool>(nullable: false),
                    IsSuperReward = table.Column<bool>(nullable: false),
                    ItemGeneratedAmount = table.Column<byte>(nullable: false),
                    ItemGeneratedUpgrade = table.Column<byte>(nullable: false),
                    ItemGeneratedVNum = table.Column<short>(nullable: false),
                    MaximumOriginalItemRare = table.Column<short>(nullable: false),
                    MinimumOriginalItemRare = table.Column<short>(nullable: false),
                    OriginalItemDesign = table.Column<short>(nullable: false),
                    OriginalItemVNum = table.Column<short>(nullable: false),
                    Probability = table.Column<short>(nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RollGeneratedItem", x => x.RollGeneratedItemId);
                    table.ForeignKey(
                        name: "FK_RollGeneratedItem_Item_ItemGeneratedVNum",
                        column: x => x.ItemGeneratedVNum,
                        principalTable: "Item",
                        principalColumn: "VNum",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_RollGeneratedItem_Item_OriginalItemVNum",
                        column: x => x.OriginalItemVNum,
                        principalTable: "Item",
                        principalColumn: "VNum",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "Character",
                columns: table => new
                {
                    CharacterId = table.Column<long>(nullable: false)
                        .Annotation("SqlServer:ValueGenerationStrategy", SqlServerValueGenerationStrategy.IdentityColumn),
                    AccountId = table.Column<long>(nullable: false),
                    Act4Dead = table.Column<int>(nullable: false),
                    Act4Kill = table.Column<int>(nullable: false),
                    Act4Points = table.Column<int>(nullable: false),
                    ArenaWinner = table.Column<int>(nullable: false),
                    Biography = table.Column<string>(maxLength: 255, nullable: true),
                    BuffBlocked = table.Column<bool>(nullable: false),
                    Class = table.Column<byte>(nullable: false),
                    Compliment = table.Column<short>(nullable: false),
                    Dignity = table.Column<float>(nullable: false),
                    Elo = table.Column<int>(nullable: false),
                    EmoticonsBlocked = table.Column<bool>(nullable: false),
                    ExchangeBlocked = table.Column<bool>(nullable: false),
                    Faction = table.Column<byte>(nullable: false),
                    FamilyRequestBlocked = table.Column<bool>(nullable: false),
                    FriendRequestBlocked = table.Column<bool>(nullable: false),
                    Gender = table.Column<byte>(nullable: false),
                    Gold = table.Column<long>(nullable: false),
                    GroupRequestBlocked = table.Column<bool>(nullable: false),
                    HairColor = table.Column<byte>(nullable: false),
                    HairStyle = table.Column<byte>(nullable: false),
                    HeroChatBlocked = table.Column<bool>(nullable: false),
                    HeroLevel = table.Column<byte>(nullable: false),
                    HeroXp = table.Column<long>(nullable: false),
                    Hp = table.Column<int>(nullable: false),
                    HpBlocked = table.Column<bool>(nullable: false),
                    JobLevel = table.Column<byte>(nullable: false),
                    JobLevelXp = table.Column<long>(nullable: false),
                    Level = table.Column<byte>(nullable: false),
                    LevelXp = table.Column<long>(nullable: false),
                    MapId = table.Column<short>(nullable: false),
                    MapX = table.Column<short>(nullable: false),
                    MapY = table.Column<short>(nullable: false),
                    MasterPoints = table.Column<int>(nullable: false),
                    MasterTicket = table.Column<int>(nullable: false),
                    MaxMateCount = table.Column<byte>(nullable: false),
                    MinilandInviteBlocked = table.Column<bool>(nullable: false),
                    MinilandMessage = table.Column<string>(maxLength: 255, nullable: true),
                    MinilandPoint = table.Column<short>(nullable: false),
                    MinilandState = table.Column<byte>(nullable: false),
                    MouseAimLock = table.Column<bool>(nullable: false),
                    Mp = table.Column<int>(nullable: false),
                    Name = table.Column<string>(unicode: false, maxLength: 255, nullable: true),
                    Prefix = table.Column<string>(maxLength: 25, nullable: true),
                    QuickGetUp = table.Column<bool>(nullable: false),
                    RagePoint = table.Column<long>(nullable: false),
                    Reput = table.Column<long>(nullable: false),
                    Slot = table.Column<byte>(nullable: false),
                    SpAdditionPoint = table.Column<int>(nullable: false),
                    SpPoint = table.Column<int>(nullable: false),
                    State = table.Column<byte>(nullable: false),
                    TalentLose = table.Column<int>(nullable: false),
                    TalentSurrender = table.Column<int>(nullable: false),
                    TalentWin = table.Column<int>(nullable: false),
                    WhisperBlocked = table.Column<bool>(nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Character", x => x.CharacterId);
                    table.ForeignKey(
                        name: "FK_Character_Account_AccountId",
                        column: x => x.AccountId,
                        principalTable: "Account",
                        principalColumn: "AccountId",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Character_Map_MapId",
                        column: x => x.MapId,
                        principalTable: "Map",
                        principalColumn: "MapId",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "Portal",
                columns: table => new
                {
                    PortalId = table.Column<int>(nullable: false)
                        .Annotation("SqlServer:ValueGenerationStrategy", SqlServerValueGenerationStrategy.IdentityColumn),
                    DestinationMapId = table.Column<short>(nullable: false),
                    DestinationX = table.Column<short>(nullable: false),
                    DestinationY = table.Column<short>(nullable: false),
                    IsDisabled = table.Column<bool>(nullable: false),
                    SourceMapId = table.Column<short>(nullable: false),
                    SourceX = table.Column<short>(nullable: false),
                    SourceY = table.Column<short>(nullable: false),
                    Type = table.Column<short>(nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Portal", x => x.PortalId);
                    table.ForeignKey(
                        name: "FK_Portal_Map_DestinationMapId",
                        column: x => x.DestinationMapId,
                        principalTable: "Map",
                        principalColumn: "MapId",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Portal_Map_SourceMapId",
                        column: x => x.SourceMapId,
                        principalTable: "Map",
                        principalColumn: "MapId",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "RespawnMapType",
                columns: table => new
                {
                    RespawnMapTypeId = table.Column<long>(nullable: false),
                    DefaultMapId = table.Column<short>(nullable: false),
                    DefaultX = table.Column<short>(nullable: false),
                    DefaultY = table.Column<short>(nullable: false),
                    Name = table.Column<string>(maxLength: 255, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RespawnMapType", x => x.RespawnMapTypeId);
                    table.ForeignKey(
                        name: "FK_RespawnMapType_Map_DefaultMapId",
                        column: x => x.DefaultMapId,
                        principalTable: "Map",
                        principalColumn: "MapId",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "ScriptedInstance",
                columns: table => new
                {
                    ScriptedInstanceId = table.Column<short>(nullable: false)
                        .Annotation("SqlServer:ValueGenerationStrategy", SqlServerValueGenerationStrategy.IdentityColumn),
                    Label = table.Column<string>(nullable: true),
                    MapId = table.Column<short>(nullable: false),
                    PositionX = table.Column<short>(nullable: false),
                    PositionY = table.Column<short>(nullable: false),
                    Script = table.Column<string>(maxLength: 2147483647, nullable: true),
                    Type = table.Column<byte>(nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ScriptedInstance", x => x.ScriptedInstanceId);
                    table.ForeignKey(
                        name: "FK_ScriptedInstance_Map_MapId",
                        column: x => x.MapId,
                        principalTable: "Map",
                        principalColumn: "MapId",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "MapMonster",
                columns: table => new
                {
                    MapMonsterId = table.Column<int>(nullable: false),
                    Direction = table.Column<byte>(nullable: false),
                    IsDisabled = table.Column<bool>(nullable: false),
                    IsMoving = table.Column<bool>(nullable: false),
                    MapId = table.Column<short>(nullable: false),
                    MapX = table.Column<short>(nullable: false),
                    MapY = table.Column<short>(nullable: false),
                    VNum = table.Column<short>(nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MapMonster", x => x.MapMonsterId);
                    table.ForeignKey(
                        name: "FK_MapMonster_Map_MapId",
                        column: x => x.MapId,
                        principalTable: "Map",
                        principalColumn: "MapId",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_MapMonster_NpcMonster_VNum",
                        column: x => x.VNum,
                        principalTable: "NpcMonster",
                        principalColumn: "NpcMonsterVNum",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "MapNpc",
                columns: table => new
                {
                    MapNpcId = table.Column<int>(nullable: false),
                    Dialog = table.Column<short>(nullable: false),
                    Direction = table.Column<byte>(nullable: false),
                    Effect = table.Column<short>(nullable: false),
                    EffectDelay = table.Column<short>(nullable: false),
                    IsDisabled = table.Column<bool>(nullable: false),
                    IsMoving = table.Column<bool>(nullable: false),
                    IsSitting = table.Column<bool>(nullable: false),
                    MapId = table.Column<short>(nullable: false),
                    MapX = table.Column<short>(nullable: false),
                    MapY = table.Column<short>(nullable: false),
                    VNum = table.Column<short>(nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MapNpc", x => x.MapNpcId);
                    table.ForeignKey(
                        name: "FK_MapNpc_Map_MapId",
                        column: x => x.MapId,
                        principalTable: "Map",
                        principalColumn: "MapId",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_MapNpc_NpcMonster_VNum",
                        column: x => x.VNum,
                        principalTable: "NpcMonster",
                        principalColumn: "NpcMonsterVNum",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "QuestObjective",
                columns: table => new
                {
                    QuestObjectiveId = table.Column<short>(nullable: false)
                        .Annotation("SqlServer:ValueGenerationStrategy", SqlServerValueGenerationStrategy.IdentityColumn),
                    Data = table.Column<int>(nullable: false),
                    Objective = table.Column<int>(nullable: false),
                    QuestId = table.Column<short>(nullable: false),
                    SpecialData = table.Column<int>(nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_QuestObjective", x => x.QuestObjectiveId);
                    table.ForeignKey(
                        name: "FK_QuestObjective_Quest_QuestId",
                        column: x => x.QuestId,
                        principalTable: "Quest",
                        principalColumn: "QuestId",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "QuestReward",
                columns: table => new
                {
                    QuestRewardId = table.Column<long>(nullable: false)
                        .Annotation("SqlServer:ValueGenerationStrategy", SqlServerValueGenerationStrategy.IdentityColumn),
                    Amount = table.Column<int>(nullable: false),
                    Data = table.Column<int>(nullable: false),
                    Design = table.Column<byte>(nullable: false),
                    QuestId = table.Column<short>(nullable: false),
                    Rarity = table.Column<byte>(nullable: false),
                    RewardType = table.Column<byte>(nullable: false),
                    Upgrade = table.Column<byte>(nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_QuestReward", x => x.QuestRewardId);
                    table.ForeignKey(
                        name: "FK_QuestReward_Quest_QuestId",
                        column: x => x.QuestId,
                        principalTable: "Quest",
                        principalColumn: "QuestId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "BCard",
                columns: table => new
                {
                    BCardId = table.Column<short>(nullable: false)
                        .Annotation("SqlServer:ValueGenerationStrategy", SqlServerValueGenerationStrategy.IdentityColumn),
                    CardId = table.Column<short>(nullable: true),
                    CastType = table.Column<byte>(nullable: false),
                    FirstData = table.Column<int>(nullable: false),
                    IsLevelDivided = table.Column<bool>(nullable: false),
                    IsLevelScaled = table.Column<bool>(nullable: false),
                    ItemVNum = table.Column<short>(nullable: true),
                    NpcMonsterVNum = table.Column<short>(nullable: true),
                    SecondData = table.Column<int>(nullable: false),
                    SkillVNum = table.Column<short>(nullable: true),
                    SubType = table.Column<byte>(nullable: false),
                    ThirdData = table.Column<int>(nullable: false),
                    Type = table.Column<byte>(nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_BCard", x => x.BCardId);
                    table.ForeignKey(
                        name: "FK_BCard_Card_CardId",
                        column: x => x.CardId,
                        principalTable: "Card",
                        principalColumn: "CardId",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_BCard_Item_ItemVNum",
                        column: x => x.ItemVNum,
                        principalTable: "Item",
                        principalColumn: "VNum",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_BCard_NpcMonster_NpcMonsterVNum",
                        column: x => x.NpcMonsterVNum,
                        principalTable: "NpcMonster",
                        principalColumn: "NpcMonsterVNum",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_BCard_Skill_SkillVNum",
                        column: x => x.SkillVNum,
                        principalTable: "Skill",
                        principalColumn: "SkillVNum",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "Combo",
                columns: table => new
                {
                    ComboId = table.Column<int>(nullable: false)
                        .Annotation("SqlServer:ValueGenerationStrategy", SqlServerValueGenerationStrategy.IdentityColumn),
                    Animation = table.Column<short>(nullable: false),
                    Effect = table.Column<short>(nullable: false),
                    Hit = table.Column<short>(nullable: false),
                    SkillVNum = table.Column<short>(nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Combo", x => x.ComboId);
                    table.ForeignKey(
                        name: "FK_Combo_Skill_SkillVNum",
                        column: x => x.SkillVNum,
                        principalTable: "Skill",
                        principalColumn: "SkillVNum",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "NpcMonsterSkill",
                columns: table => new
                {
                    NpcMonsterSkillId = table.Column<long>(nullable: false)
                        .Annotation("SqlServer:ValueGenerationStrategy", SqlServerValueGenerationStrategy.IdentityColumn),
                    NpcMonsterVNum = table.Column<short>(nullable: false),
                    Rate = table.Column<short>(nullable: false),
                    SkillVNum = table.Column<short>(nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_NpcMonsterSkill", x => x.NpcMonsterSkillId);
                    table.ForeignKey(
                        name: "FK_NpcMonsterSkill_NpcMonster_NpcMonsterVNum",
                        column: x => x.NpcMonsterVNum,
                        principalTable: "NpcMonster",
                        principalColumn: "NpcMonsterVNum",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_NpcMonsterSkill_Skill_SkillVNum",
                        column: x => x.SkillVNum,
                        principalTable: "Skill",
                        principalColumn: "SkillVNum",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "CharacterQuest",
                columns: table => new
                {
                    Id = table.Column<Guid>(nullable: false),
                    CharacterId = table.Column<long>(nullable: false),
                    FifthObjective = table.Column<int>(nullable: false),
                    FirstObjective = table.Column<int>(nullable: false),
                    FourthObjective = table.Column<int>(nullable: false),
                    IsMainQuest = table.Column<bool>(nullable: false),
                    QuestId = table.Column<short>(nullable: false),
                    SecondObjective = table.Column<int>(nullable: false),
                    ThirdObjective = table.Column<int>(nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CharacterQuest", x => x.Id);
                    table.ForeignKey(
                        name: "FK_CharacterQuest_Character_CharacterId",
                        column: x => x.CharacterId,
                        principalTable: "Character",
                        principalColumn: "CharacterId",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_CharacterQuest_Quest_QuestId",
                        column: x => x.QuestId,
                        principalTable: "Quest",
                        principalColumn: "QuestId",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "CharacterRelation",
                columns: table => new
                {
                    CharacterRelationId = table.Column<long>(nullable: false)
                        .Annotation("SqlServer:ValueGenerationStrategy", SqlServerValueGenerationStrategy.IdentityColumn),
                    CharacterId = table.Column<long>(nullable: false),
                    RelatedCharacterId = table.Column<long>(nullable: false),
                    RelationType = table.Column<short>(nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CharacterRelation", x => x.CharacterRelationId);
                    table.ForeignKey(
                        name: "FK_CharacterRelation_Character_CharacterId",
                        column: x => x.CharacterId,
                        principalTable: "Character",
                        principalColumn: "CharacterId",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_CharacterRelation_Character_RelatedCharacterId",
                        column: x => x.RelatedCharacterId,
                        principalTable: "Character",
                        principalColumn: "CharacterId",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "CharacterSkill",
                columns: table => new
                {
                    Id = table.Column<Guid>(nullable: false),
                    CharacterId = table.Column<long>(nullable: false),
                    SkillVNum = table.Column<short>(nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CharacterSkill", x => x.Id);
                    table.ForeignKey(
                        name: "FK_CharacterSkill_Character_CharacterId",
                        column: x => x.CharacterId,
                        principalTable: "Character",
                        principalColumn: "CharacterId",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_CharacterSkill_Skill_SkillVNum",
                        column: x => x.SkillVNum,
                        principalTable: "Skill",
                        principalColumn: "SkillVNum",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "FamilyCharacter",
                columns: table => new
                {
                    FamilyCharacterId = table.Column<long>(nullable: false)
                        .Annotation("SqlServer:ValueGenerationStrategy", SqlServerValueGenerationStrategy.IdentityColumn),
                    Authority = table.Column<byte>(nullable: false),
                    CharacterId = table.Column<long>(nullable: false),
                    DailyMessage = table.Column<string>(maxLength: 255, nullable: true),
                    Experience = table.Column<int>(nullable: false),
                    FamilyId = table.Column<long>(nullable: false),
                    Rank = table.Column<byte>(nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_FamilyCharacter", x => x.FamilyCharacterId);
                    table.ForeignKey(
                        name: "FK_FamilyCharacter_Character_CharacterId",
                        column: x => x.CharacterId,
                        principalTable: "Character",
                        principalColumn: "CharacterId",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_FamilyCharacter_Family_FamilyId",
                        column: x => x.FamilyId,
                        principalTable: "Family",
                        principalColumn: "FamilyId",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "ItemInstance",
                columns: table => new
                {
                    HoldingVNum = table.Column<short>(nullable: true),
                    Id = table.Column<Guid>(nullable: false),
                    Amount = table.Column<int>(nullable: false),
                    BazaarItemId = table.Column<long>(nullable: true),
                    BoundCharacterId = table.Column<long>(nullable: true),
                    CharacterId = table.Column<long>(nullable: false),
                    Design = table.Column<short>(nullable: false),
                    Discriminator = table.Column<string>(nullable: false),
                    DurabilityPoint = table.Column<int>(nullable: false),
                    ItemDeleteTime = table.Column<DateTime>(nullable: true),
                    ItemVNum = table.Column<short>(nullable: false),
                    Rare = table.Column<short>(nullable: false),
                    Slot = table.Column<short>(nullable: false),
                    Type = table.Column<byte>(nullable: false),
                    Upgrade = table.Column<byte>(nullable: false),
                    SlDamage = table.Column<short>(nullable: true),
                    SlDefence = table.Column<short>(nullable: true),
                    SlElement = table.Column<short>(nullable: true),
                    SlHP = table.Column<short>(nullable: true),
                    SpDamage = table.Column<byte>(nullable: true),
                    SpDark = table.Column<byte>(nullable: true),
                    SpDefence = table.Column<byte>(nullable: true),
                    SpElement = table.Column<byte>(nullable: true),
                    SpFire = table.Column<byte>(nullable: true),
                    SpHP = table.Column<byte>(nullable: true),
                    SpLevel = table.Column<byte>(nullable: true),
                    SpLight = table.Column<byte>(nullable: true),
                    SpStoneUpgrade = table.Column<byte>(nullable: true),
                    SpWater = table.Column<byte>(nullable: true),
                    HP = table.Column<short>(nullable: true),
                    MP = table.Column<short>(nullable: true),
                    Ammo = table.Column<byte>(nullable: true),
                    Cellon = table.Column<byte>(nullable: true),
                    CloseDefence = table.Column<short>(nullable: true),
                    Concentrate = table.Column<short>(nullable: true),
                    CriticalDodge = table.Column<short>(nullable: true),
                    CriticalLuckRate = table.Column<byte>(nullable: true),
                    CriticalRate = table.Column<short>(nullable: true),
                    DamageMaximum = table.Column<short>(nullable: true),
                    DamageMinimum = table.Column<short>(nullable: true),
                    DarkElement = table.Column<byte>(nullable: true),
                    DarkResistance = table.Column<short>(nullable: true),
                    DefenceDodge = table.Column<short>(nullable: true),
                    DistanceDefence = table.Column<short>(nullable: true),
                    DistanceDefenceDodge = table.Column<short>(nullable: true),
                    ElementRate = table.Column<short>(nullable: true),
                    FireElement = table.Column<byte>(nullable: true),
                    FireResistance = table.Column<short>(nullable: true),
                    WearableInstance_HP = table.Column<short>(nullable: true),
                    HitRate = table.Column<short>(nullable: true),
                    IsEmpty = table.Column<bool>(nullable: true),
                    IsFixed = table.Column<bool>(nullable: true),
                    LightElement = table.Column<byte>(nullable: true),
                    LightResistance = table.Column<short>(nullable: true),
                    WearableInstance_MP = table.Column<short>(nullable: true),
                    MagicDefence = table.Column<short>(nullable: true),
                    MaxElementRate = table.Column<short>(nullable: true),
                    ShellRarity = table.Column<byte>(nullable: true),
                    WaterElement = table.Column<byte>(nullable: true),
                    WaterResistance = table.Column<short>(nullable: true),
                    XP = table.Column<long>(nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ItemInstance", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ItemInstance_Character_BoundCharacterId",
                        column: x => x.BoundCharacterId,
                        principalTable: "Character",
                        principalColumn: "CharacterId",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ItemInstance_Character_CharacterId",
                        column: x => x.CharacterId,
                        principalTable: "Character",
                        principalColumn: "CharacterId",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ItemInstance_Item_ItemVNum",
                        column: x => x.ItemVNum,
                        principalTable: "Item",
                        principalColumn: "VNum",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "Mail",
                columns: table => new
                {
                    MailId = table.Column<long>(nullable: false)
                        .Annotation("SqlServer:ValueGenerationStrategy", SqlServerValueGenerationStrategy.IdentityColumn),
                    AttachmentAmount = table.Column<byte>(nullable: false),
                    AttachmentRarity = table.Column<byte>(nullable: false),
                    AttachmentUpgrade = table.Column<byte>(nullable: false),
                    AttachmentVNum = table.Column<short>(nullable: true),
                    Date = table.Column<DateTime>(nullable: false),
                    EqPacket = table.Column<string>(maxLength: 255, nullable: true),
                    IsOpened = table.Column<bool>(nullable: false),
                    IsSenderCopy = table.Column<bool>(nullable: false),
                    Message = table.Column<string>(maxLength: 255, nullable: true),
                    ReceiverId = table.Column<long>(nullable: false),
                    SenderCharacterClass = table.Column<byte>(nullable: false),
                    SenderGender = table.Column<byte>(nullable: false),
                    SenderHairColor = table.Column<byte>(nullable: false),
                    SenderHairStyle = table.Column<byte>(nullable: false),
                    SenderId = table.Column<long>(nullable: false),
                    SenderMorphId = table.Column<short>(nullable: false),
                    Title = table.Column<string>(maxLength: 255, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Mail", x => x.MailId);
                    table.ForeignKey(
                        name: "FK_Mail_Item_AttachmentVNum",
                        column: x => x.AttachmentVNum,
                        principalTable: "Item",
                        principalColumn: "VNum",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Mail_Character_ReceiverId",
                        column: x => x.ReceiverId,
                        principalTable: "Character",
                        principalColumn: "CharacterId",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Mail_Character_SenderId",
                        column: x => x.SenderId,
                        principalTable: "Character",
                        principalColumn: "CharacterId",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "Mate",
                columns: table => new
                {
                    MateId = table.Column<long>(nullable: false)
                        .Annotation("SqlServer:ValueGenerationStrategy", SqlServerValueGenerationStrategy.IdentityColumn),
                    Attack = table.Column<byte>(nullable: false),
                    CanPickUp = table.Column<bool>(nullable: false),
                    CharacterId = table.Column<long>(nullable: false),
                    Defence = table.Column<byte>(nullable: false),
                    Direction = table.Column<byte>(nullable: false),
                    Experience = table.Column<long>(nullable: false),
                    Hp = table.Column<int>(nullable: false),
                    IsSummonable = table.Column<bool>(nullable: false),
                    IsTeamMember = table.Column<bool>(nullable: false),
                    Level = table.Column<byte>(nullable: false),
                    Loyalty = table.Column<short>(nullable: false),
                    MapX = table.Column<short>(nullable: false),
                    MapY = table.Column<short>(nullable: false),
                    MateType = table.Column<byte>(nullable: false),
                    Mp = table.Column<int>(nullable: false),
                    Name = table.Column<string>(maxLength: 255, nullable: true),
                    Skin = table.Column<short>(nullable: false),
                    VNum = table.Column<short>(nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Mate", x => x.MateId);
                    table.ForeignKey(
                        name: "FK_Mate_Character_CharacterId",
                        column: x => x.CharacterId,
                        principalTable: "Character",
                        principalColumn: "CharacterId",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Mate_NpcMonster_VNum",
                        column: x => x.VNum,
                        principalTable: "NpcMonster",
                        principalColumn: "NpcMonsterVNum",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "QuicklistEntry",
                columns: table => new
                {
                    Id = table.Column<Guid>(nullable: false),
                    CharacterId = table.Column<long>(nullable: false),
                    Morph = table.Column<short>(nullable: false),
                    Pos = table.Column<short>(nullable: false),
                    Q1 = table.Column<short>(nullable: false),
                    Q2 = table.Column<short>(nullable: false),
                    Slot = table.Column<short>(nullable: false),
                    Type = table.Column<short>(nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_QuicklistEntry", x => x.Id);
                    table.ForeignKey(
                        name: "FK_QuicklistEntry_Character_CharacterId",
                        column: x => x.CharacterId,
                        principalTable: "Character",
                        principalColumn: "CharacterId",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "StaticBonus",
                columns: table => new
                {
                    StaticBonusId = table.Column<long>(nullable: false)
                        .Annotation("SqlServer:ValueGenerationStrategy", SqlServerValueGenerationStrategy.IdentityColumn),
                    CharacterId = table.Column<long>(nullable: false),
                    DateEnd = table.Column<DateTime>(nullable: false),
                    StaticBonusType = table.Column<byte>(nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_StaticBonus", x => x.StaticBonusId);
                    table.ForeignKey(
                        name: "FK_StaticBonus_Character_CharacterId",
                        column: x => x.CharacterId,
                        principalTable: "Character",
                        principalColumn: "CharacterId",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "StaticBuff",
                columns: table => new
                {
                    StaticBuffId = table.Column<long>(nullable: false)
                        .Annotation("SqlServer:ValueGenerationStrategy", SqlServerValueGenerationStrategy.IdentityColumn),
                    CardId = table.Column<short>(nullable: false),
                    CharacterId = table.Column<long>(nullable: false),
                    RemainingTime = table.Column<int>(nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_StaticBuff", x => x.StaticBuffId);
                    table.ForeignKey(
                        name: "FK_StaticBuff_Card_CardId",
                        column: x => x.CardId,
                        principalTable: "Card",
                        principalColumn: "CardId",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_StaticBuff_Character_CharacterId",
                        column: x => x.CharacterId,
                        principalTable: "Character",
                        principalColumn: "CharacterId",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "MapType",
                columns: table => new
                {
                    MapTypeId = table.Column<short>(nullable: false),
                    MapTypeName = table.Column<string>(nullable: true),
                    PotionDelay = table.Column<short>(nullable: false),
                    RespawnMapTypeId = table.Column<long>(nullable: true),
                    ReturnMapTypeId = table.Column<long>(nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MapType", x => x.MapTypeId);
                    table.ForeignKey(
                        name: "FK_MapType_RespawnMapType_RespawnMapTypeId",
                        column: x => x.RespawnMapTypeId,
                        principalTable: "RespawnMapType",
                        principalColumn: "RespawnMapTypeId",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_MapType_RespawnMapType_ReturnMapTypeId",
                        column: x => x.ReturnMapTypeId,
                        principalTable: "RespawnMapType",
                        principalColumn: "RespawnMapTypeId",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "Respawn",
                columns: table => new
                {
                    RespawnId = table.Column<long>(nullable: false)
                        .Annotation("SqlServer:ValueGenerationStrategy", SqlServerValueGenerationStrategy.IdentityColumn),
                    CharacterId = table.Column<long>(nullable: false),
                    MapId = table.Column<short>(nullable: false),
                    RespawnMapTypeId = table.Column<long>(nullable: false),
                    X = table.Column<short>(nullable: false),
                    Y = table.Column<short>(nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Respawn", x => x.RespawnId);
                    table.ForeignKey(
                        name: "FK_Respawn_Character_CharacterId",
                        column: x => x.CharacterId,
                        principalTable: "Character",
                        principalColumn: "CharacterId",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Respawn_Map_MapId",
                        column: x => x.MapId,
                        principalTable: "Map",
                        principalColumn: "MapId",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Respawn_RespawnMapType_RespawnMapTypeId",
                        column: x => x.RespawnMapTypeId,
                        principalTable: "RespawnMapType",
                        principalColumn: "RespawnMapTypeId",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "Recipe",
                columns: table => new
                {
                    RecipeId = table.Column<short>(nullable: false)
                        .Annotation("SqlServer:ValueGenerationStrategy", SqlServerValueGenerationStrategy.IdentityColumn),
                    Amount = table.Column<byte>(nullable: false),
                    ItemVNum = table.Column<short>(nullable: false),
                    MapNpcId = table.Column<int>(nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Recipe", x => x.RecipeId);
                    table.ForeignKey(
                        name: "FK_Recipe_Item_ItemVNum",
                        column: x => x.ItemVNum,
                        principalTable: "Item",
                        principalColumn: "VNum",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Recipe_MapNpc_MapNpcId",
                        column: x => x.MapNpcId,
                        principalTable: "MapNpc",
                        principalColumn: "MapNpcId",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "Shop",
                columns: table => new
                {
                    ShopId = table.Column<int>(nullable: false)
                        .Annotation("SqlServer:ValueGenerationStrategy", SqlServerValueGenerationStrategy.IdentityColumn),
                    MapNpcId = table.Column<int>(nullable: false),
                    MenuType = table.Column<byte>(nullable: false),
                    Name = table.Column<string>(maxLength: 255, nullable: true),
                    ShopType = table.Column<byte>(nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Shop", x => x.ShopId);
                    table.ForeignKey(
                        name: "FK_Shop_MapNpc_MapNpcId",
                        column: x => x.MapNpcId,
                        principalTable: "MapNpc",
                        principalColumn: "MapNpcId",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "Teleporter",
                columns: table => new
                {
                    TeleporterId = table.Column<short>(nullable: false)
                        .Annotation("SqlServer:ValueGenerationStrategy", SqlServerValueGenerationStrategy.IdentityColumn),
                    Index = table.Column<short>(nullable: false),
                    MapId = table.Column<short>(nullable: false),
                    MapNpcId = table.Column<int>(nullable: false),
                    MapX = table.Column<short>(nullable: false),
                    MapY = table.Column<short>(nullable: false),
                    Type = table.Column<byte>(nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Teleporter", x => x.TeleporterId);
                    table.ForeignKey(
                        name: "FK_Teleporter_Map_MapId",
                        column: x => x.MapId,
                        principalTable: "Map",
                        principalColumn: "MapId",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Teleporter_MapNpc_MapNpcId",
                        column: x => x.MapNpcId,
                        principalTable: "MapNpc",
                        principalColumn: "MapNpcId",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "BazaarItem",
                columns: table => new
                {
                    BazaarItemId = table.Column<long>(nullable: false)
                        .Annotation("SqlServer:ValueGenerationStrategy", SqlServerValueGenerationStrategy.IdentityColumn),
                    Amount = table.Column<byte>(nullable: false),
                    DateStart = table.Column<DateTime>(nullable: false),
                    Duration = table.Column<short>(nullable: false),
                    IsPackage = table.Column<bool>(nullable: false),
                    ItemInstanceId = table.Column<Guid>(nullable: false),
                    MedalUsed = table.Column<bool>(nullable: false),
                    Price = table.Column<long>(nullable: false),
                    SellerId = table.Column<long>(nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_BazaarItem", x => x.BazaarItemId);
                    table.ForeignKey(
                        name: "FK_BazaarItem_ItemInstance_ItemInstanceId",
                        column: x => x.ItemInstanceId,
                        principalTable: "ItemInstance",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_BazaarItem_Character_SellerId",
                        column: x => x.SellerId,
                        principalTable: "Character",
                        principalColumn: "CharacterId",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "EquipmentOption",
                columns: table => new
                {
                    Id = table.Column<Guid>(nullable: false),
                    Level = table.Column<byte>(nullable: false),
                    Type = table.Column<byte>(nullable: false),
                    Value = table.Column<int>(nullable: false),
                    WearableInstanceId = table.Column<Guid>(nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_EquipmentOption", x => x.Id);
                    table.ForeignKey(
                        name: "FK_EquipmentOption_ItemInstance_WearableInstanceId",
                        column: x => x.WearableInstanceId,
                        principalTable: "ItemInstance",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "MinilandObject",
                columns: table => new
                {
                    MinilandObjectId = table.Column<long>(nullable: false)
                        .Annotation("SqlServer:ValueGenerationStrategy", SqlServerValueGenerationStrategy.IdentityColumn),
                    CharacterId = table.Column<long>(nullable: false),
                    ItemInstanceId = table.Column<Guid>(nullable: true),
                    Level1BoxAmount = table.Column<byte>(nullable: false),
                    Level2BoxAmount = table.Column<byte>(nullable: false),
                    Level3BoxAmount = table.Column<byte>(nullable: false),
                    Level4BoxAmount = table.Column<byte>(nullable: false),
                    Level5BoxAmount = table.Column<byte>(nullable: false),
                    MapX = table.Column<short>(nullable: false),
                    MapY = table.Column<short>(nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MinilandObject", x => x.MinilandObjectId);
                    table.ForeignKey(
                        name: "FK_MinilandObject_Character_CharacterId",
                        column: x => x.CharacterId,
                        principalTable: "Character",
                        principalColumn: "CharacterId",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_MinilandObject_ItemInstance_ItemInstanceId",
                        column: x => x.ItemInstanceId,
                        principalTable: "ItemInstance",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "Drop",
                columns: table => new
                {
                    DropId = table.Column<short>(nullable: false)
                        .Annotation("SqlServer:ValueGenerationStrategy", SqlServerValueGenerationStrategy.IdentityColumn),
                    Amount = table.Column<int>(nullable: false),
                    DropChance = table.Column<int>(nullable: false),
                    ItemVNum = table.Column<short>(nullable: false),
                    MapTypeId = table.Column<short>(nullable: true),
                    MonsterVNum = table.Column<short>(nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Drop", x => x.DropId);
                    table.ForeignKey(
                        name: "FK_Drop_Item_ItemVNum",
                        column: x => x.ItemVNum,
                        principalTable: "Item",
                        principalColumn: "VNum",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Drop_MapType_MapTypeId",
                        column: x => x.MapTypeId,
                        principalTable: "MapType",
                        principalColumn: "MapTypeId",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Drop_NpcMonster_MonsterVNum",
                        column: x => x.MonsterVNum,
                        principalTable: "NpcMonster",
                        principalColumn: "NpcMonsterVNum",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "MapTypeMap",
                columns: table => new
                {
                    MapTypeMapId = table.Column<short>(nullable: false)
                        .Annotation("SqlServer:ValueGenerationStrategy", SqlServerValueGenerationStrategy.IdentityColumn),
                    MapId = table.Column<short>(nullable: false),
                    MapTypeId = table.Column<short>(nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MapTypeMap", x => x.MapTypeMapId);
                    table.ForeignKey(
                        name: "FK_MapTypeMap_Map_MapId",
                        column: x => x.MapId,
                        principalTable: "Map",
                        principalColumn: "MapId",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_MapTypeMap_MapType_MapTypeId",
                        column: x => x.MapTypeId,
                        principalTable: "MapType",
                        principalColumn: "MapTypeId",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "RecipeItem",
                columns: table => new
                {
                    RecipeItemId = table.Column<short>(nullable: false)
                        .Annotation("SqlServer:ValueGenerationStrategy", SqlServerValueGenerationStrategy.IdentityColumn),
                    Amount = table.Column<short>(nullable: false),
                    ItemVNum = table.Column<short>(nullable: false),
                    RecipeId = table.Column<short>(nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RecipeItem", x => x.RecipeItemId);
                    table.ForeignKey(
                        name: "FK_RecipeItem_Item_ItemVNum",
                        column: x => x.ItemVNum,
                        principalTable: "Item",
                        principalColumn: "VNum",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_RecipeItem_Recipe_RecipeId",
                        column: x => x.RecipeId,
                        principalTable: "Recipe",
                        principalColumn: "RecipeId",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "ShopItem",
                columns: table => new
                {
                    ShopItemId = table.Column<int>(nullable: false)
                        .Annotation("SqlServer:ValueGenerationStrategy", SqlServerValueGenerationStrategy.IdentityColumn),
                    Color = table.Column<byte>(nullable: false),
                    ItemVNum = table.Column<short>(nullable: false),
                    Rare = table.Column<short>(nullable: false),
                    ShopId = table.Column<int>(nullable: false),
                    Slot = table.Column<byte>(nullable: false),
                    Type = table.Column<byte>(nullable: false),
                    Upgrade = table.Column<byte>(nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ShopItem", x => x.ShopItemId);
                    table.ForeignKey(
                        name: "FK_ShopItem_Item_ItemVNum",
                        column: x => x.ItemVNum,
                        principalTable: "Item",
                        principalColumn: "VNum",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ShopItem_Shop_ShopId",
                        column: x => x.ShopId,
                        principalTable: "Shop",
                        principalColumn: "ShopId",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "ShopSkill",
                columns: table => new
                {
                    ShopSkillId = table.Column<int>(nullable: false)
                        .Annotation("SqlServer:ValueGenerationStrategy", SqlServerValueGenerationStrategy.IdentityColumn),
                    ShopId = table.Column<int>(nullable: false),
                    SkillVNum = table.Column<short>(nullable: false),
                    Slot = table.Column<byte>(nullable: false),
                    Type = table.Column<byte>(nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ShopSkill", x => x.ShopSkillId);
                    table.ForeignKey(
                        name: "FK_ShopSkill_Shop_ShopId",
                        column: x => x.ShopId,
                        principalTable: "Shop",
                        principalColumn: "ShopId",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ShopSkill_Skill_SkillVNum",
                        column: x => x.SkillVNum,
                        principalTable: "Skill",
                        principalColumn: "SkillVNum",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_BazaarItem_ItemInstanceId",
                table: "BazaarItem",
                column: "ItemInstanceId");

            migrationBuilder.CreateIndex(
                name: "IX_BazaarItem_SellerId",
                table: "BazaarItem",
                column: "SellerId");

            migrationBuilder.CreateIndex(
                name: "IX_BCard_CardId",
                table: "BCard",
                column: "CardId");

            migrationBuilder.CreateIndex(
                name: "IX_BCard_ItemVNum",
                table: "BCard",
                column: "ItemVNum");

            migrationBuilder.CreateIndex(
                name: "IX_BCard_NpcMonsterVNum",
                table: "BCard",
                column: "NpcMonsterVNum");

            migrationBuilder.CreateIndex(
                name: "IX_BCard_SkillVNum",
                table: "BCard",
                column: "SkillVNum");

            migrationBuilder.CreateIndex(
                name: "IX_Character_AccountId",
                table: "Character",
                column: "AccountId");

            migrationBuilder.CreateIndex(
                name: "IX_Character_MapId",
                table: "Character",
                column: "MapId");

            migrationBuilder.CreateIndex(
                name: "IX_CharacterQuest_CharacterId",
                table: "CharacterQuest",
                column: "CharacterId");

            migrationBuilder.CreateIndex(
                name: "IX_CharacterQuest_QuestId",
                table: "CharacterQuest",
                column: "QuestId");

            migrationBuilder.CreateIndex(
                name: "IX_CharacterRelation_CharacterId",
                table: "CharacterRelation",
                column: "CharacterId");

            migrationBuilder.CreateIndex(
                name: "IX_CharacterRelation_RelatedCharacterId",
                table: "CharacterRelation",
                column: "RelatedCharacterId");

            migrationBuilder.CreateIndex(
                name: "IX_CharacterSkill_CharacterId",
                table: "CharacterSkill",
                column: "CharacterId");

            migrationBuilder.CreateIndex(
                name: "IX_CharacterSkill_SkillVNum",
                table: "CharacterSkill",
                column: "SkillVNum");

            migrationBuilder.CreateIndex(
                name: "IX_Combo_SkillVNum",
                table: "Combo",
                column: "SkillVNum");

            migrationBuilder.CreateIndex(
                name: "IX_Drop_ItemVNum",
                table: "Drop",
                column: "ItemVNum");

            migrationBuilder.CreateIndex(
                name: "IX_Drop_MapTypeId",
                table: "Drop",
                column: "MapTypeId");

            migrationBuilder.CreateIndex(
                name: "IX_Drop_MonsterVNum",
                table: "Drop",
                column: "MonsterVNum");

            migrationBuilder.CreateIndex(
                name: "IX_EquipmentOption_WearableInstanceId",
                table: "EquipmentOption",
                column: "WearableInstanceId");

            migrationBuilder.CreateIndex(
                name: "IX_FamilyCharacter_CharacterId",
                table: "FamilyCharacter",
                column: "CharacterId");

            migrationBuilder.CreateIndex(
                name: "IX_FamilyCharacter_FamilyId",
                table: "FamilyCharacter",
                column: "FamilyId");

            migrationBuilder.CreateIndex(
                name: "IX_FamilyLog_FamilyId",
                table: "FamilyLog",
                column: "FamilyId");

            migrationBuilder.CreateIndex(
                name: "IX_ItemInstance_BoundCharacterId",
                table: "ItemInstance",
                column: "BoundCharacterId");

            migrationBuilder.CreateIndex(
                name: "IX_ItemInstance_ItemVNum",
                table: "ItemInstance",
                column: "ItemVNum");

            migrationBuilder.CreateIndex(
                name: "IX_ItemInstance_CharacterId_Slot_Type",
                table: "ItemInstance",
                columns: new[] { "CharacterId", "Slot", "Type" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Mail_AttachmentVNum",
                table: "Mail",
                column: "AttachmentVNum");

            migrationBuilder.CreateIndex(
                name: "IX_Mail_ReceiverId",
                table: "Mail",
                column: "ReceiverId");

            migrationBuilder.CreateIndex(
                name: "IX_Mail_SenderId",
                table: "Mail",
                column: "SenderId");

            migrationBuilder.CreateIndex(
                name: "IX_MapMonster_MapId",
                table: "MapMonster",
                column: "MapId");

            migrationBuilder.CreateIndex(
                name: "IX_MapMonster_VNum",
                table: "MapMonster",
                column: "VNum");

            migrationBuilder.CreateIndex(
                name: "IX_MapNpc_MapId",
                table: "MapNpc",
                column: "MapId");

            migrationBuilder.CreateIndex(
                name: "IX_MapNpc_VNum",
                table: "MapNpc",
                column: "VNum");

            migrationBuilder.CreateIndex(
                name: "IX_MapType_RespawnMapTypeId",
                table: "MapType",
                column: "RespawnMapTypeId");

            migrationBuilder.CreateIndex(
                name: "IX_MapType_ReturnMapTypeId",
                table: "MapType",
                column: "ReturnMapTypeId");

            migrationBuilder.CreateIndex(
                name: "IX_MapTypeMap_MapTypeId",
                table: "MapTypeMap",
                column: "MapTypeId");

            migrationBuilder.CreateIndex(
                name: "IX_MapTypeMap_MapId_MapTypeId",
                table: "MapTypeMap",
                columns: new[] { "MapId", "MapTypeId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Mate_CharacterId",
                table: "Mate",
                column: "CharacterId");

            migrationBuilder.CreateIndex(
                name: "IX_Mate_VNum",
                table: "Mate",
                column: "VNum");

            migrationBuilder.CreateIndex(
                name: "IX_MinilandObject_CharacterId",
                table: "MinilandObject",
                column: "CharacterId");

            migrationBuilder.CreateIndex(
                name: "IX_MinilandObject_ItemInstanceId",
                table: "MinilandObject",
                column: "ItemInstanceId");

            migrationBuilder.CreateIndex(
                name: "IX_NpcMonsterSkill_NpcMonsterVNum",
                table: "NpcMonsterSkill",
                column: "NpcMonsterVNum");

            migrationBuilder.CreateIndex(
                name: "IX_NpcMonsterSkill_SkillVNum",
                table: "NpcMonsterSkill",
                column: "SkillVNum");

            migrationBuilder.CreateIndex(
                name: "IX_PenaltyLog_AccountId",
                table: "PenaltyLog",
                column: "AccountId");

            migrationBuilder.CreateIndex(
                name: "IX_Portal_DestinationMapId",
                table: "Portal",
                column: "DestinationMapId");

            migrationBuilder.CreateIndex(
                name: "IX_Portal_SourceMapId",
                table: "Portal",
                column: "SourceMapId");

            migrationBuilder.CreateIndex(
                name: "IX_QuestObjective_QuestId",
                table: "QuestObjective",
                column: "QuestId");

            migrationBuilder.CreateIndex(
                name: "IX_QuestReward_QuestId",
                table: "QuestReward",
                column: "QuestId");

            migrationBuilder.CreateIndex(
                name: "IX_QuicklistEntry_CharacterId",
                table: "QuicklistEntry",
                column: "CharacterId");

            migrationBuilder.CreateIndex(
                name: "IX_Recipe_ItemVNum",
                table: "Recipe",
                column: "ItemVNum");

            migrationBuilder.CreateIndex(
                name: "IX_Recipe_MapNpcId",
                table: "Recipe",
                column: "MapNpcId");

            migrationBuilder.CreateIndex(
                name: "IX_RecipeItem_ItemVNum",
                table: "RecipeItem",
                column: "ItemVNum");

            migrationBuilder.CreateIndex(
                name: "IX_RecipeItem_RecipeId",
                table: "RecipeItem",
                column: "RecipeId");

            migrationBuilder.CreateIndex(
                name: "IX_Respawn_CharacterId",
                table: "Respawn",
                column: "CharacterId");

            migrationBuilder.CreateIndex(
                name: "IX_Respawn_MapId",
                table: "Respawn",
                column: "MapId");

            migrationBuilder.CreateIndex(
                name: "IX_Respawn_RespawnMapTypeId",
                table: "Respawn",
                column: "RespawnMapTypeId");

            migrationBuilder.CreateIndex(
                name: "IX_RespawnMapType_DefaultMapId",
                table: "RespawnMapType",
                column: "DefaultMapId");

            migrationBuilder.CreateIndex(
                name: "IX_RollGeneratedItem_ItemGeneratedVNum",
                table: "RollGeneratedItem",
                column: "ItemGeneratedVNum");

            migrationBuilder.CreateIndex(
                name: "IX_RollGeneratedItem_OriginalItemVNum",
                table: "RollGeneratedItem",
                column: "OriginalItemVNum");

            migrationBuilder.CreateIndex(
                name: "IX_ScriptedInstance_MapId",
                table: "ScriptedInstance",
                column: "MapId");

            migrationBuilder.CreateIndex(
                name: "IX_Shop_MapNpcId",
                table: "Shop",
                column: "MapNpcId");

            migrationBuilder.CreateIndex(
                name: "IX_ShopItem_ItemVNum",
                table: "ShopItem",
                column: "ItemVNum");

            migrationBuilder.CreateIndex(
                name: "IX_ShopItem_ShopId",
                table: "ShopItem",
                column: "ShopId");

            migrationBuilder.CreateIndex(
                name: "IX_ShopSkill_ShopId",
                table: "ShopSkill",
                column: "ShopId");

            migrationBuilder.CreateIndex(
                name: "IX_ShopSkill_SkillVNum",
                table: "ShopSkill",
                column: "SkillVNum");

            migrationBuilder.CreateIndex(
                name: "IX_StaticBonus_CharacterId",
                table: "StaticBonus",
                column: "CharacterId");

            migrationBuilder.CreateIndex(
                name: "IX_StaticBuff_CardId",
                table: "StaticBuff",
                column: "CardId");

            migrationBuilder.CreateIndex(
                name: "IX_StaticBuff_CharacterId",
                table: "StaticBuff",
                column: "CharacterId");

            migrationBuilder.CreateIndex(
                name: "IX_Teleporter_MapId",
                table: "Teleporter",
                column: "MapId");

            migrationBuilder.CreateIndex(
                name: "IX_Teleporter_MapNpcId",
                table: "Teleporter",
                column: "MapNpcId");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "BazaarItem");

            migrationBuilder.DropTable(
                name: "BCard");

            migrationBuilder.DropTable(
                name: "CharacterQuest");

            migrationBuilder.DropTable(
                name: "CharacterRelation");

            migrationBuilder.DropTable(
                name: "CharacterSkill");

            migrationBuilder.DropTable(
                name: "Combo");

            migrationBuilder.DropTable(
                name: "Drop");

            migrationBuilder.DropTable(
                name: "EquipmentOption");

            migrationBuilder.DropTable(
                name: "FamilyCharacter");

            migrationBuilder.DropTable(
                name: "FamilyLog");

            migrationBuilder.DropTable(
                name: "Mail");

            migrationBuilder.DropTable(
                name: "MapMonster");

            migrationBuilder.DropTable(
                name: "MapTypeMap");

            migrationBuilder.DropTable(
                name: "Mate");

            migrationBuilder.DropTable(
                name: "MinilandObject");

            migrationBuilder.DropTable(
                name: "NpcMonsterSkill");

            migrationBuilder.DropTable(
                name: "PenaltyLog");

            migrationBuilder.DropTable(
                name: "Portal");

            migrationBuilder.DropTable(
                name: "QuestObjective");

            migrationBuilder.DropTable(
                name: "QuestReward");

            migrationBuilder.DropTable(
                name: "QuicklistEntry");

            migrationBuilder.DropTable(
                name: "RecipeItem");

            migrationBuilder.DropTable(
                name: "Respawn");

            migrationBuilder.DropTable(
                name: "RollGeneratedItem");

            migrationBuilder.DropTable(
                name: "ScriptedInstance");

            migrationBuilder.DropTable(
                name: "ShopItem");

            migrationBuilder.DropTable(
                name: "ShopSkill");

            migrationBuilder.DropTable(
                name: "StaticBonus");

            migrationBuilder.DropTable(
                name: "StaticBuff");

            migrationBuilder.DropTable(
                name: "Teleporter");

            migrationBuilder.DropTable(
                name: "Family");

            migrationBuilder.DropTable(
                name: "MapType");

            migrationBuilder.DropTable(
                name: "ItemInstance");

            migrationBuilder.DropTable(
                name: "Quest");

            migrationBuilder.DropTable(
                name: "Recipe");

            migrationBuilder.DropTable(
                name: "Shop");

            migrationBuilder.DropTable(
                name: "Skill");

            migrationBuilder.DropTable(
                name: "Card");

            migrationBuilder.DropTable(
                name: "RespawnMapType");

            migrationBuilder.DropTable(
                name: "Character");

            migrationBuilder.DropTable(
                name: "Item");

            migrationBuilder.DropTable(
                name: "MapNpc");

            migrationBuilder.DropTable(
                name: "Account");

            migrationBuilder.DropTable(
                name: "Map");

            migrationBuilder.DropTable(
                name: "NpcMonster");
        }
    }
}
