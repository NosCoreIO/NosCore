using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

namespace NosCore.Database.Migrations
{
    public partial class Aphrodite1 : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Account",
                columns: table => new
                {
                    AccountId = table.Column<long>(nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Authority = table.Column<short>(nullable: false),
                    Email = table.Column<string>(maxLength: 255, nullable: true),
                    Name = table.Column<string>(maxLength: 255, nullable: false),
                    Password = table.Column<string>(unicode: false, maxLength: 255, nullable: true),
                    NewAuthPassword = table.Column<string>(maxLength: 255, nullable: true),
                    NewAuthSalt = table.Column<string>(maxLength: 255, nullable: true),
                    RegistrationIp = table.Column<string>(maxLength: 45, nullable: true),
                    VerificationToken = table.Column<string>(maxLength: 32, nullable: true),
                    Language = table.Column<int>(nullable: false),
                    BankMoney = table.Column<long>(nullable: false),
                    ItemShopMoney = table.Column<long>(nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Account", x => x.AccountId);
                });

            migrationBuilder.CreateTable(
                name: "Act",
                columns: table => new
                {
                    ActId = table.Column<byte>(nullable: false),
                    Title = table.Column<string>(maxLength: 255, nullable: false),
                    Scene = table.Column<byte>(nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Act", x => x.ActId);
                });

            migrationBuilder.CreateTable(
                name: "AuditLog",
                columns: table => new
                {
                    AuditId = table.Column<Guid>(nullable: false),
                    TargetId = table.Column<string>(maxLength: 80, nullable: false),
                    TargetType = table.Column<string>(maxLength: 32, nullable: false),
                    Time = table.Column<DateTime>(nullable: false),
                    AuditLogType = table.Column<int>(nullable: false),
                    Value = table.Column<string>(maxLength: 255, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AuditLog", x => x.AuditId);
                });

            migrationBuilder.CreateTable(
                name: "Card",
                columns: table => new
                {
                    CardId = table.Column<short>(nullable: false),
                    Duration = table.Column<int>(nullable: false),
                    EffectId = table.Column<int>(nullable: false),
                    Level = table.Column<byte>(nullable: false),
                    Name = table.Column<string>(maxLength: 255, nullable: false),
                    Delay = table.Column<int>(nullable: false),
                    TimeoutBuff = table.Column<short>(nullable: false),
                    TimeoutBuffChance = table.Column<byte>(nullable: false),
                    BuffType = table.Column<byte>(nullable: false),
                    Propability = table.Column<byte>(nullable: false)
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
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    FamilyExperience = table.Column<int>(nullable: false),
                    FamilyHeadGender = table.Column<byte>(nullable: false),
                    FamilyLevel = table.Column<byte>(nullable: false),
                    FamilyFaction = table.Column<byte>(nullable: false),
                    FamilyMessage = table.Column<string>(maxLength: 255, nullable: true),
                    ManagerAuthorityType = table.Column<int>(nullable: false),
                    ManagerCanGetHistory = table.Column<bool>(nullable: false),
                    ManagerCanInvite = table.Column<bool>(nullable: false),
                    ManagerCanNotice = table.Column<bool>(nullable: false),
                    ManagerCanShout = table.Column<bool>(nullable: false),
                    MaxSize = table.Column<byte>(nullable: false),
                    MemberAuthorityType = table.Column<int>(nullable: false),
                    MemberCanGetHistory = table.Column<bool>(nullable: false),
                    Name = table.Column<string>(maxLength: 255, nullable: false),
                    WarehouseSize = table.Column<byte>(nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Family", x => x.FamilyId);
                });

            migrationBuilder.CreateTable(
                name: "I18NActDesc",
                columns: table => new
                {
                    I18NActDescId = table.Column<int>(nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Key = table.Column<string>(nullable: false),
                    RegionType = table.Column<int>(nullable: false),
                    Text = table.Column<string>(nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_I18NActDesc", x => x.I18NActDescId);
                });

            migrationBuilder.CreateTable(
                name: "I18NBCard",
                columns: table => new
                {
                    I18NbCardId = table.Column<int>(nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Key = table.Column<string>(nullable: false),
                    RegionType = table.Column<int>(nullable: false),
                    Text = table.Column<string>(nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_I18NBCard", x => x.I18NbCardId);
                });

            migrationBuilder.CreateTable(
                name: "I18NCard",
                columns: table => new
                {
                    I18NCardId = table.Column<int>(nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Key = table.Column<string>(nullable: false),
                    RegionType = table.Column<int>(nullable: false),
                    Text = table.Column<string>(nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_I18NCard", x => x.I18NCardId);
                });

            migrationBuilder.CreateTable(
                name: "I18NItem",
                columns: table => new
                {
                    I18NItemId = table.Column<int>(nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Key = table.Column<string>(nullable: false),
                    RegionType = table.Column<int>(nullable: false),
                    Text = table.Column<string>(nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_I18NItem", x => x.I18NItemId);
                });

            migrationBuilder.CreateTable(
                name: "I18NMapIdData",
                columns: table => new
                {
                    I18NMapIdDataId = table.Column<int>(nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Key = table.Column<string>(nullable: false),
                    RegionType = table.Column<int>(nullable: false),
                    Text = table.Column<string>(nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_I18NMapIdData", x => x.I18NMapIdDataId);
                });

            migrationBuilder.CreateTable(
                name: "I18NMapPointData",
                columns: table => new
                {
                    I18NMapPointDataId = table.Column<int>(nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Key = table.Column<string>(nullable: false),
                    RegionType = table.Column<int>(nullable: false),
                    Text = table.Column<string>(nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_I18NMapPointData", x => x.I18NMapPointDataId);
                });

            migrationBuilder.CreateTable(
                name: "I18NNpcMonster",
                columns: table => new
                {
                    I18NNpcMonsterId = table.Column<int>(nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Key = table.Column<string>(nullable: false),
                    RegionType = table.Column<int>(nullable: false),
                    Text = table.Column<string>(nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_I18NNpcMonster", x => x.I18NNpcMonsterId);
                });

            migrationBuilder.CreateTable(
                name: "I18NNpcMonsterTalk",
                columns: table => new
                {
                    I18NNpcMonsterTalkId = table.Column<int>(nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Key = table.Column<string>(nullable: false),
                    RegionType = table.Column<int>(nullable: false),
                    Text = table.Column<string>(nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_I18NNpcMonsterTalk", x => x.I18NNpcMonsterTalkId);
                });

            migrationBuilder.CreateTable(
                name: "I18NQuest",
                columns: table => new
                {
                    I18NQuestId = table.Column<int>(nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Key = table.Column<string>(nullable: false),
                    RegionType = table.Column<int>(nullable: false),
                    Text = table.Column<string>(nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_I18NQuest", x => x.I18NQuestId);
                });

            migrationBuilder.CreateTable(
                name: "I18NSkill",
                columns: table => new
                {
                    I18NSkillId = table.Column<int>(nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Key = table.Column<string>(nullable: false),
                    RegionType = table.Column<int>(nullable: false),
                    Text = table.Column<string>(nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_I18NSkill", x => x.I18NSkillId);
                });

            migrationBuilder.CreateTable(
                name: "Item",
                columns: table => new
                {
                    VNum = table.Column<short>(nullable: false),
                    BasicUpgrade = table.Column<byte>(nullable: false),
                    Flag1 = table.Column<bool>(nullable: false),
                    Flag2 = table.Column<bool>(nullable: false),
                    Flag3 = table.Column<bool>(nullable: false),
                    Flag4 = table.Column<bool>(nullable: false),
                    RequireBinding = table.Column<bool>(nullable: false),
                    Flag6 = table.Column<bool>(nullable: false),
                    Flag7 = table.Column<bool>(nullable: false),
                    Flag8 = table.Column<bool>(nullable: false),
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
                    Effect = table.Column<int>(nullable: false),
                    EffectValue = table.Column<int>(nullable: false),
                    Element = table.Column<int>(nullable: false),
                    ElementRate = table.Column<short>(nullable: false),
                    EquipmentSlot = table.Column<byte>(nullable: false),
                    FireElement = table.Column<byte>(nullable: false),
                    FireResistance = table.Column<short>(nullable: false),
                    Height = table.Column<byte>(nullable: false),
                    HitRate = table.Column<short>(nullable: false),
                    Hp = table.Column<short>(nullable: false),
                    HpRegeneration = table.Column<short>(nullable: false),
                    IsMinilandActionable = table.Column<bool>(nullable: false),
                    IsColored = table.Column<bool>(nullable: false),
                    IsConsumable = table.Column<bool>(nullable: false),
                    IsDroppable = table.Column<bool>(nullable: false),
                    IsHeroic = table.Column<bool>(nullable: false),
                    Flag9 = table.Column<bool>(nullable: false),
                    IsWarehouse = table.Column<bool>(nullable: false),
                    IsSoldable = table.Column<bool>(nullable: false),
                    IsTradable = table.Column<bool>(nullable: false),
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
                    SecondMorph = table.Column<short>(nullable: false),
                    Mp = table.Column<short>(nullable: false),
                    MpRegeneration = table.Column<short>(nullable: false),
                    Name = table.Column<string>(maxLength: 255, nullable: false),
                    Price = table.Column<long>(nullable: false),
                    PvpDefence = table.Column<short>(nullable: false),
                    PvpStrength = table.Column<byte>(nullable: false),
                    ReduceOposantResistance = table.Column<short>(nullable: false),
                    ReputationMinimum = table.Column<byte>(nullable: false),
                    ReputPrice = table.Column<long>(nullable: false),
                    SecondaryElement = table.Column<int>(nullable: false),
                    Sex = table.Column<byte>(nullable: false),
                    Speed = table.Column<byte>(nullable: false),
                    SpType = table.Column<byte>(nullable: false),
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
                    Data = table.Column<byte[]>(nullable: false),
                    Music = table.Column<int>(nullable: false),
                    Name = table.Column<string>(maxLength: 255, nullable: false),
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
                    HeroLevel = table.Column<byte>(nullable: false),
                    HeroXp = table.Column<int>(nullable: false),
                    IsHostile = table.Column<bool>(nullable: false),
                    JobXp = table.Column<int>(nullable: false),
                    Level = table.Column<byte>(nullable: false),
                    LightResistance = table.Column<short>(nullable: false),
                    MagicDefence = table.Column<short>(nullable: false),
                    MaxHp = table.Column<int>(nullable: false),
                    MaxMp = table.Column<int>(nullable: false),
                    MonsterType = table.Column<byte>(nullable: false),
                    Name = table.Column<string>(maxLength: 255, nullable: false),
                    NoAggresiveIcon = table.Column<bool>(nullable: false),
                    NoticeRange = table.Column<byte>(nullable: false),
                    Race = table.Column<byte>(nullable: false),
                    RaceType = table.Column<byte>(nullable: false),
                    RespawnTime = table.Column<int>(nullable: false),
                    Speed = table.Column<byte>(nullable: false),
                    VNumRequired = table.Column<short>(nullable: false),
                    WaterResistance = table.Column<short>(nullable: false),
                    Xp = table.Column<int>(nullable: false),
                    IsPercent = table.Column<bool>(nullable: false),
                    TakeDamages = table.Column<int>(nullable: false),
                    GiveDamagePercentage = table.Column<int>(nullable: false)
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
                    QuestType = table.Column<int>(nullable: false),
                    LevelMin = table.Column<byte>(nullable: false),
                    LevelMax = table.Column<byte>(nullable: false),
                    StartDialogId = table.Column<int>(nullable: true),
                    EndDialogId = table.Column<int>(nullable: true),
                    TargetMap = table.Column<short>(nullable: true),
                    TargetX = table.Column<short>(nullable: true),
                    TargetY = table.Column<short>(nullable: true),
                    NextQuestId = table.Column<short>(nullable: true),
                    IsDaily = table.Column<bool>(nullable: false),
                    AutoFinish = table.Column<bool>(nullable: false),
                    IsSecondary = table.Column<bool>(nullable: false),
                    SpecialData = table.Column<int>(nullable: true),
                    RequiredQuestId = table.Column<short>(nullable: true),
                    Title = table.Column<string>(maxLength: 255, nullable: false),
                    Desc = table.Column<string>(maxLength: 255, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Quest", x => x.QuestId);
                });

            migrationBuilder.CreateTable(
                name: "QuestReward",
                columns: table => new
                {
                    QuestRewardId = table.Column<short>(nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    RewardType = table.Column<byte>(nullable: false),
                    Data = table.Column<int>(nullable: false),
                    Design = table.Column<byte>(nullable: false),
                    Rarity = table.Column<byte>(nullable: false),
                    Upgrade = table.Column<byte>(nullable: false),
                    Amount = table.Column<int>(nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_QuestReward", x => x.QuestRewardId);
                });

            migrationBuilder.CreateTable(
                name: "Skill",
                columns: table => new
                {
                    SkillVNum = table.Column<short>(nullable: false),
                    AttackAnimation = table.Column<short>(nullable: false),
                    CastAnimation = table.Column<short>(nullable: false),
                    CastEffect = table.Column<short>(nullable: false),
                    CastId = table.Column<short>(nullable: false),
                    CastTime = table.Column<short>(nullable: false),
                    Class = table.Column<byte>(nullable: false),
                    Cooldown = table.Column<short>(nullable: false),
                    CpCost = table.Column<byte>(nullable: false),
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
                    Name = table.Column<string>(maxLength: 255, nullable: false),
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
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    AccountId = table.Column<long>(nullable: false),
                    AdminName = table.Column<string>(nullable: false),
                    DateEnd = table.Column<DateTime>(nullable: false),
                    DateStart = table.Column<DateTime>(nullable: false),
                    Penalty = table.Column<byte>(nullable: false),
                    Reason = table.Column<string>(maxLength: 255, nullable: false)
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
                name: "ActPart",
                columns: table => new
                {
                    ActPartId = table.Column<byte>(nullable: false),
                    ActPartNumber = table.Column<byte>(nullable: false),
                    ActId = table.Column<byte>(nullable: false),
                    MaxTs = table.Column<byte>(nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ActPart", x => x.ActPartId);
                    table.ForeignKey(
                        name: "FK_ActPart_Act_ActId",
                        column: x => x.ActId,
                        principalTable: "Act",
                        principalColumn: "ActId",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "FamilyLog",
                columns: table => new
                {
                    FamilyLogId = table.Column<long>(nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
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
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    OriginalItemDesign = table.Column<short>(nullable: false),
                    OriginalItemVNum = table.Column<short>(nullable: false),
                    Probability = table.Column<short>(nullable: false),
                    ItemGeneratedAmount = table.Column<byte>(nullable: false),
                    ItemGeneratedVNum = table.Column<short>(nullable: false),
                    ItemGeneratedUpgrade = table.Column<byte>(nullable: false),
                    IsRareRandom = table.Column<bool>(nullable: false),
                    MinimumOriginalItemRare = table.Column<short>(nullable: false),
                    MaximumOriginalItemRare = table.Column<short>(nullable: false),
                    IsSuperReward = table.Column<bool>(nullable: false)
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
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
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
                    MouseAimLock = table.Column<bool>(nullable: false),
                    Mp = table.Column<int>(nullable: false),
                    Prefix = table.Column<string>(maxLength: 25, nullable: true),
                    Name = table.Column<string>(unicode: false, maxLength: 255, nullable: false),
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
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
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
                    MapId = table.Column<short>(nullable: false),
                    DefaultX = table.Column<short>(nullable: false),
                    DefaultY = table.Column<short>(nullable: false),
                    Name = table.Column<string>(maxLength: 255, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RespawnMapType", x => x.RespawnMapTypeId);
                    table.ForeignKey(
                        name: "FK_RespawnMapType_Map_MapId",
                        column: x => x.MapId,
                        principalTable: "Map",
                        principalColumn: "MapId",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "ScriptedInstance",
                columns: table => new
                {
                    ScriptedInstanceId = table.Column<short>(nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    MapId = table.Column<short>(nullable: false),
                    PositionX = table.Column<short>(nullable: false),
                    PositionY = table.Column<short>(nullable: false),
                    Label = table.Column<string>(nullable: true),
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
                    IsDisabled = table.Column<bool>(nullable: false),
                    IsMoving = table.Column<bool>(nullable: false),
                    MapId = table.Column<short>(nullable: false),
                    MapX = table.Column<short>(nullable: false),
                    MapY = table.Column<short>(nullable: false),
                    VNum = table.Column<short>(nullable: false),
                    Direction = table.Column<byte>(nullable: false)
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
                    Effect = table.Column<short>(nullable: false),
                    EffectDelay = table.Column<short>(nullable: false),
                    IsDisabled = table.Column<bool>(nullable: false),
                    IsMoving = table.Column<bool>(nullable: false),
                    IsSitting = table.Column<bool>(nullable: false),
                    MapId = table.Column<short>(nullable: false),
                    MapX = table.Column<short>(nullable: false),
                    MapY = table.Column<short>(nullable: false),
                    VNum = table.Column<short>(nullable: false),
                    Direction = table.Column<byte>(nullable: false)
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
                    QuestObjectiveId = table.Column<Guid>(nullable: false),
                    FirstData = table.Column<int>(nullable: false),
                    SecondData = table.Column<int>(nullable: true),
                    ThirdData = table.Column<int>(nullable: true),
                    FourthData = table.Column<int>(nullable: true),
                    QuestId = table.Column<short>(nullable: false)
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
                name: "QuestQuestReward",
                columns: table => new
                {
                    Id = table.Column<Guid>(nullable: false),
                    QuestRewardId = table.Column<short>(nullable: false),
                    QuestId = table.Column<short>(nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_QuestQuestReward", x => x.Id);
                    table.ForeignKey(
                        name: "FK_QuestQuestReward_Quest_QuestId",
                        column: x => x.QuestId,
                        principalTable: "Quest",
                        principalColumn: "QuestId",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_QuestQuestReward_QuestReward_QuestRewardId",
                        column: x => x.QuestRewardId,
                        principalTable: "QuestReward",
                        principalColumn: "QuestRewardId",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "BCard",
                columns: table => new
                {
                    BCardId = table.Column<short>(nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    SubType = table.Column<byte>(nullable: false),
                    Type = table.Column<byte>(nullable: false),
                    FirstData = table.Column<int>(nullable: false),
                    SecondData = table.Column<int>(nullable: false),
                    CardId = table.Column<short>(nullable: true),
                    ItemVNum = table.Column<short>(nullable: true),
                    SkillVNum = table.Column<short>(nullable: true),
                    NpcMonsterVNum = table.Column<short>(nullable: true),
                    CastType = table.Column<byte>(nullable: false),
                    ThirdData = table.Column<int>(nullable: false),
                    IsLevelScaled = table.Column<bool>(nullable: false),
                    IsLevelDivided = table.Column<bool>(nullable: false)
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
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
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
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
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
                name: "CharacterActPart",
                columns: table => new
                {
                    Id = table.Column<Guid>(nullable: false),
                    CharacterId = table.Column<long>(nullable: false),
                    ActPartId = table.Column<byte>(nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CharacterActPart", x => x.Id);
                    table.ForeignKey(
                        name: "FK_CharacterActPart_ActPart_ActPartId",
                        column: x => x.ActPartId,
                        principalTable: "ActPart",
                        principalColumn: "ActPartId",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_CharacterActPart_Character_CharacterId",
                        column: x => x.CharacterId,
                        principalTable: "Character",
                        principalColumn: "CharacterId",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "CharacterQuest",
                columns: table => new
                {
                    Id = table.Column<Guid>(nullable: false),
                    CharacterId = table.Column<long>(nullable: false),
                    QuestId = table.Column<short>(nullable: false),
                    FirstObjective = table.Column<int>(nullable: false),
                    SecondObjective = table.Column<int>(nullable: false),
                    ThirdObjective = table.Column<int>(nullable: false),
                    FourthObjective = table.Column<int>(nullable: false),
                    FifthObjective = table.Column<int>(nullable: false),
                    IsMainQuest = table.Column<bool>(nullable: false)
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
                    CharacterRelationId = table.Column<Guid>(nullable: false),
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
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
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
                    Id = table.Column<Guid>(nullable: false),
                    Amount = table.Column<short>(nullable: false),
                    BoundCharacterId = table.Column<long>(nullable: true),
                    CharacterId = table.Column<long>(nullable: false),
                    Design = table.Column<short>(nullable: false),
                    DurabilityPoint = table.Column<int>(nullable: false),
                    ItemDeleteTime = table.Column<DateTime>(nullable: true),
                    ItemVNum = table.Column<short>(nullable: false),
                    Upgrade = table.Column<byte>(nullable: false),
                    Rare = table.Column<short>(nullable: false),
                    Discriminator = table.Column<string>(nullable: false),
                    Hp = table.Column<short>(nullable: true),
                    Mp = table.Column<short>(nullable: true),
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
                    HitRate = table.Column<short>(nullable: true),
                    WearableInstance_Hp = table.Column<short>(nullable: true),
                    IsEmpty = table.Column<bool>(nullable: true),
                    IsFixed = table.Column<bool>(nullable: true),
                    LightElement = table.Column<byte>(nullable: true),
                    LightResistance = table.Column<short>(nullable: true),
                    MagicDefence = table.Column<short>(nullable: true),
                    MaxElementRate = table.Column<short>(nullable: true),
                    WearableInstance_Mp = table.Column<short>(nullable: true),
                    ShellRarity = table.Column<byte>(nullable: true),
                    WaterElement = table.Column<byte>(nullable: true),
                    WaterResistance = table.Column<short>(nullable: true),
                    Xp = table.Column<long>(nullable: true),
                    SlDamage = table.Column<short>(nullable: true),
                    SlDefence = table.Column<short>(nullable: true),
                    SlElement = table.Column<short>(nullable: true),
                    SlHp = table.Column<short>(nullable: true),
                    SpDamage = table.Column<byte>(nullable: true),
                    SpDark = table.Column<byte>(nullable: true),
                    SpDefence = table.Column<byte>(nullable: true),
                    SpElement = table.Column<byte>(nullable: true),
                    SpFire = table.Column<byte>(nullable: true),
                    SpHp = table.Column<byte>(nullable: true),
                    SpLevel = table.Column<byte>(nullable: true),
                    SpLight = table.Column<byte>(nullable: true),
                    SpStoneUpgrade = table.Column<byte>(nullable: true),
                    SpWater = table.Column<byte>(nullable: true),
                    HoldingVNum = table.Column<short>(nullable: true)
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
                        name: "FK_ItemInstance_Item_ItemVNum",
                        column: x => x.ItemVNum,
                        principalTable: "Item",
                        principalColumn: "VNum",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "Mate",
                columns: table => new
                {
                    MateId = table.Column<long>(nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
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
                    VNum = table.Column<short>(nullable: false),
                    Skin = table.Column<short>(nullable: false)
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
                name: "Miniland",
                columns: table => new
                {
                    MinilandId = table.Column<Guid>(nullable: false),
                    MinilandMessage = table.Column<string>(maxLength: 255, nullable: true),
                    MinilandPoint = table.Column<long>(nullable: false),
                    State = table.Column<byte>(nullable: false),
                    OwnerId = table.Column<long>(nullable: false),
                    DailyVisitCount = table.Column<int>(nullable: false),
                    VisitCount = table.Column<int>(nullable: false),
                    WelcomeMusicInfo = table.Column<string>(nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Miniland", x => x.MinilandId);
                    table.ForeignKey(
                        name: "FK_Miniland_Character_OwnerId",
                        column: x => x.OwnerId,
                        principalTable: "Character",
                        principalColumn: "CharacterId",
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
                    Type = table.Column<byte>(nullable: false)
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
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    CharacterId = table.Column<long>(nullable: false),
                    DateEnd = table.Column<DateTime>(nullable: true),
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
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    CharacterId = table.Column<long>(nullable: false),
                    CardId = table.Column<short>(nullable: false),
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
                name: "Title",
                columns: table => new
                {
                    Id = table.Column<Guid>(nullable: false),
                    CharacterId = table.Column<long>(nullable: false),
                    Active = table.Column<bool>(nullable: false),
                    Visible = table.Column<bool>(nullable: false),
                    TitleType = table.Column<short>(nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Title", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Title_Character_CharacterId",
                        column: x => x.CharacterId,
                        principalTable: "Character",
                        principalColumn: "CharacterId",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "Warehouse",
                columns: table => new
                {
                    Id = table.Column<Guid>(nullable: false),
                    CharacterId = table.Column<long>(nullable: true),
                    FamilyId = table.Column<long>(nullable: true),
                    Type = table.Column<byte>(nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Warehouse", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Warehouse_Character_CharacterId",
                        column: x => x.CharacterId,
                        principalTable: "Character",
                        principalColumn: "CharacterId",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Warehouse_Family_FamilyId",
                        column: x => x.FamilyId,
                        principalTable: "Family",
                        principalColumn: "FamilyId",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "MapType",
                columns: table => new
                {
                    MapTypeId = table.Column<short>(nullable: false),
                    MapTypeName = table.Column<string>(nullable: false),
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
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
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
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
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
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    MapNpcId = table.Column<int>(nullable: false),
                    MenuType = table.Column<byte>(nullable: false),
                    Name = table.Column<string>(maxLength: 255, nullable: false),
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
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Index = table.Column<short>(nullable: false),
                    Type = table.Column<byte>(nullable: false),
                    MapId = table.Column<short>(nullable: false),
                    MapNpcId = table.Column<int>(nullable: false),
                    MapX = table.Column<short>(nullable: false),
                    MapY = table.Column<short>(nullable: false)
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
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Amount = table.Column<short>(nullable: false),
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
                name: "InventoryItemInstance",
                columns: table => new
                {
                    Id = table.Column<Guid>(nullable: false),
                    CharacterId = table.Column<long>(nullable: false),
                    ItemInstanceId = table.Column<Guid>(nullable: false),
                    Slot = table.Column<short>(nullable: false),
                    Type = table.Column<byte>(nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_InventoryItemInstance", x => x.Id);
                    table.ForeignKey(
                        name: "FK_InventoryItemInstance_Character_CharacterId",
                        column: x => x.CharacterId,
                        principalTable: "Character",
                        principalColumn: "CharacterId",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_InventoryItemInstance_ItemInstance_ItemInstanceId",
                        column: x => x.ItemInstanceId,
                        principalTable: "ItemInstance",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "Mail",
                columns: table => new
                {
                    MailId = table.Column<long>(nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Hat = table.Column<short>(nullable: true),
                    Armor = table.Column<short>(nullable: true),
                    MainWeapon = table.Column<short>(nullable: true),
                    SecondaryWeapon = table.Column<short>(nullable: true),
                    Mask = table.Column<short>(nullable: true),
                    Fairy = table.Column<short>(nullable: true),
                    CostumeSuit = table.Column<short>(nullable: true),
                    CostumeHat = table.Column<short>(nullable: true),
                    WeaponSkin = table.Column<short>(nullable: true),
                    WingSkin = table.Column<short>(nullable: true),
                    Date = table.Column<DateTime>(nullable: false),
                    IsOpened = table.Column<bool>(nullable: false),
                    IsSenderCopy = table.Column<bool>(nullable: false),
                    ItemInstanceId = table.Column<Guid>(nullable: true),
                    Message = table.Column<string>(maxLength: 255, nullable: false),
                    ReceiverId = table.Column<long>(nullable: false),
                    SenderId = table.Column<long>(nullable: true),
                    SenderCharacterClass = table.Column<byte>(nullable: true),
                    SenderGender = table.Column<byte>(nullable: true),
                    SenderHairColor = table.Column<byte>(nullable: true),
                    SenderHairStyle = table.Column<byte>(nullable: true),
                    SenderMorphId = table.Column<short>(nullable: true),
                    Title = table.Column<string>(maxLength: 255, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Mail", x => x.MailId);
                    table.ForeignKey(
                        name: "FK_Mail_ItemInstance_ItemInstanceId",
                        column: x => x.ItemInstanceId,
                        principalTable: "ItemInstance",
                        principalColumn: "Id",
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
                name: "WarehouseItem",
                columns: table => new
                {
                    Id = table.Column<Guid>(nullable: false),
                    WarehouseId = table.Column<Guid>(nullable: false),
                    ItemInstanceId = table.Column<Guid>(nullable: false),
                    Slot = table.Column<short>(nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_WarehouseItem", x => x.Id);
                    table.ForeignKey(
                        name: "FK_WarehouseItem_ItemInstance_ItemInstanceId",
                        column: x => x.ItemInstanceId,
                        principalTable: "ItemInstance",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_WarehouseItem_Warehouse_WarehouseId",
                        column: x => x.WarehouseId,
                        principalTable: "Warehouse",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "Drop",
                columns: table => new
                {
                    DropId = table.Column<short>(nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Amount = table.Column<int>(nullable: false),
                    DropChance = table.Column<int>(nullable: false),
                    VNum = table.Column<short>(nullable: false),
                    MapTypeId = table.Column<short>(nullable: true),
                    MonsterVNum = table.Column<short>(nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Drop", x => x.DropId);
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
                    table.ForeignKey(
                        name: "FK_Drop_Item_VNum",
                        column: x => x.VNum,
                        principalTable: "Item",
                        principalColumn: "VNum",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "MapTypeMap",
                columns: table => new
                {
                    MapTypeMapId = table.Column<short>(nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
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
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
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
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
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
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
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

            migrationBuilder.CreateTable(
                name: "MinilandObject",
                columns: table => new
                {
                    MinilandObjectId = table.Column<Guid>(nullable: false),
                    InventoryItemInstanceId = table.Column<Guid>(nullable: true),
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
                        name: "FK_MinilandObject_InventoryItemInstance_InventoryItemInstanceId",
                        column: x => x.InventoryItemInstanceId,
                        principalTable: "InventoryItemInstance",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Account_Name",
                table: "Account",
                column: "Name",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ActPart_ActId",
                table: "ActPart",
                column: "ActId");

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
                name: "IX_CharacterActPart_ActPartId",
                table: "CharacterActPart",
                column: "ActPartId");

            migrationBuilder.CreateIndex(
                name: "IX_CharacterActPart_CharacterId",
                table: "CharacterActPart",
                column: "CharacterId");

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
                name: "IX_Drop_MapTypeId",
                table: "Drop",
                column: "MapTypeId");

            migrationBuilder.CreateIndex(
                name: "IX_Drop_MonsterVNum",
                table: "Drop",
                column: "MonsterVNum");

            migrationBuilder.CreateIndex(
                name: "IX_Drop_VNum",
                table: "Drop",
                column: "VNum");

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
                name: "IX_I18NActDesc_Key_RegionType",
                table: "I18NActDesc",
                columns: new[] { "Key", "RegionType" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_I18NBCard_Key_RegionType",
                table: "I18NBCard",
                columns: new[] { "Key", "RegionType" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_I18NCard_Key_RegionType",
                table: "I18NCard",
                columns: new[] { "Key", "RegionType" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_I18NItem_Key_RegionType",
                table: "I18NItem",
                columns: new[] { "Key", "RegionType" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_I18NMapIdData_Key_RegionType",
                table: "I18NMapIdData",
                columns: new[] { "Key", "RegionType" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_I18NMapPointData_Key_RegionType",
                table: "I18NMapPointData",
                columns: new[] { "Key", "RegionType" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_I18NNpcMonster_Key_RegionType",
                table: "I18NNpcMonster",
                columns: new[] { "Key", "RegionType" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_I18NNpcMonsterTalk_Key_RegionType",
                table: "I18NNpcMonsterTalk",
                columns: new[] { "Key", "RegionType" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_I18NQuest_Key_RegionType",
                table: "I18NQuest",
                columns: new[] { "Key", "RegionType" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_I18NSkill_Key_RegionType",
                table: "I18NSkill",
                columns: new[] { "Key", "RegionType" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_InventoryItemInstance_ItemInstanceId",
                table: "InventoryItemInstance",
                column: "ItemInstanceId");

            migrationBuilder.CreateIndex(
                name: "IX_InventoryItemInstance_CharacterId_Slot_Type",
                table: "InventoryItemInstance",
                columns: new[] { "CharacterId", "Slot", "Type" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ItemInstance_BoundCharacterId",
                table: "ItemInstance",
                column: "BoundCharacterId");

            migrationBuilder.CreateIndex(
                name: "IX_ItemInstance_ItemVNum",
                table: "ItemInstance",
                column: "ItemVNum");

            migrationBuilder.CreateIndex(
                name: "IX_Mail_ItemInstanceId",
                table: "Mail",
                column: "ItemInstanceId");

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
                name: "IX_Miniland_OwnerId",
                table: "Miniland",
                column: "OwnerId");

            migrationBuilder.CreateIndex(
                name: "IX_MinilandObject_InventoryItemInstanceId",
                table: "MinilandObject",
                column: "InventoryItemInstanceId");

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
                name: "IX_QuestQuestReward_QuestId",
                table: "QuestQuestReward",
                column: "QuestId");

            migrationBuilder.CreateIndex(
                name: "IX_QuestQuestReward_QuestRewardId",
                table: "QuestQuestReward",
                column: "QuestRewardId");

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
                name: "IX_RespawnMapType_MapId",
                table: "RespawnMapType",
                column: "MapId");

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

            migrationBuilder.CreateIndex(
                name: "IX_Title_CharacterId_TitleType",
                table: "Title",
                columns: new[] { "CharacterId", "TitleType" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Warehouse_CharacterId",
                table: "Warehouse",
                column: "CharacterId");

            migrationBuilder.CreateIndex(
                name: "IX_Warehouse_FamilyId",
                table: "Warehouse",
                column: "FamilyId");

            migrationBuilder.CreateIndex(
                name: "IX_WarehouseItem_ItemInstanceId",
                table: "WarehouseItem",
                column: "ItemInstanceId");

            migrationBuilder.CreateIndex(
                name: "IX_WarehouseItem_WarehouseId",
                table: "WarehouseItem",
                column: "WarehouseId");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "AuditLog");

            migrationBuilder.DropTable(
                name: "BazaarItem");

            migrationBuilder.DropTable(
                name: "BCard");

            migrationBuilder.DropTable(
                name: "CharacterActPart");

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
                name: "I18NActDesc");

            migrationBuilder.DropTable(
                name: "I18NBCard");

            migrationBuilder.DropTable(
                name: "I18NCard");

            migrationBuilder.DropTable(
                name: "I18NItem");

            migrationBuilder.DropTable(
                name: "I18NMapIdData");

            migrationBuilder.DropTable(
                name: "I18NMapPointData");

            migrationBuilder.DropTable(
                name: "I18NNpcMonster");

            migrationBuilder.DropTable(
                name: "I18NNpcMonsterTalk");

            migrationBuilder.DropTable(
                name: "I18NQuest");

            migrationBuilder.DropTable(
                name: "I18NSkill");

            migrationBuilder.DropTable(
                name: "Mail");

            migrationBuilder.DropTable(
                name: "MapMonster");

            migrationBuilder.DropTable(
                name: "MapTypeMap");

            migrationBuilder.DropTable(
                name: "Mate");

            migrationBuilder.DropTable(
                name: "Miniland");

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
                name: "QuestQuestReward");

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
                name: "Title");

            migrationBuilder.DropTable(
                name: "WarehouseItem");

            migrationBuilder.DropTable(
                name: "ActPart");

            migrationBuilder.DropTable(
                name: "MapType");

            migrationBuilder.DropTable(
                name: "InventoryItemInstance");

            migrationBuilder.DropTable(
                name: "Quest");

            migrationBuilder.DropTable(
                name: "QuestReward");

            migrationBuilder.DropTable(
                name: "Recipe");

            migrationBuilder.DropTable(
                name: "Shop");

            migrationBuilder.DropTable(
                name: "Skill");

            migrationBuilder.DropTable(
                name: "Card");

            migrationBuilder.DropTable(
                name: "Warehouse");

            migrationBuilder.DropTable(
                name: "Act");

            migrationBuilder.DropTable(
                name: "RespawnMapType");

            migrationBuilder.DropTable(
                name: "ItemInstance");

            migrationBuilder.DropTable(
                name: "MapNpc");

            migrationBuilder.DropTable(
                name: "Family");

            migrationBuilder.DropTable(
                name: "Character");

            migrationBuilder.DropTable(
                name: "Item");

            migrationBuilder.DropTable(
                name: "NpcMonster");

            migrationBuilder.DropTable(
                name: "Account");

            migrationBuilder.DropTable(
                name: "Map");
        }
    }
}
