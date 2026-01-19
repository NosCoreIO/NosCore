using Microsoft.EntityFrameworkCore.Migrations;
using NodaTime;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;
using System;

#nullable disable

namespace NosCore.Database.Migrations
{
    public partial class FirstMigration : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterDatabase()
                .Annotation("Npgsql:Enum:audit_log_type", "account_creation,character_creation,email_update")
                .Annotation("Npgsql:Enum:authority_type", "user,moderator,game_master,administrator,root,closed,banned,unconfirmed")
                .Annotation("Npgsql:Enum:character_class_type", "adventurer,swordsman,archer,mage,martial_artist")
                .Annotation("Npgsql:Enum:character_relation_type", "friend,hidden_spouse,spouse,blocked")
                .Annotation("Npgsql:Enum:character_state", "active,inactive")
                .Annotation("Npgsql:Enum:element_type", "neutral,fire,water,light,dark")
                .Annotation("Npgsql:Enum:equipment_type", "main_weapon,armor,hat,gloves,boots,secondary_weapon,necklace,ring,bracelet,mask,fairy,amulet,sp,costume_suit,costume_hat,weapon_skin,wing_skin")
                .Annotation("Npgsql:Enum:family_authority", "head,assistant,manager,member")
                .Annotation("Npgsql:Enum:family_authority_type", "none,put,all")
                .Annotation("Npgsql:Enum:family_log_type", "daily_message,raid_won,rainbow_battle,family_xp,family_level_up,level_up,item_upgraded,right_changed,authority_changed,family_managed,user_managed,ware_house_added,ware_house_removed")
                .Annotation("Npgsql:Enum:family_member_rank", "nothing,old_uncle,old_aunt,father,mother,uncle,aunt,brother,sister,spouse,brother2,sister2,old_son,old_daugter,middle_son,middle_daughter,young_son,young_daugter,old_little_son,old_little_daughter,little_son,little_daughter,middle_little_son,middle_little_daugter")
                .Annotation("Npgsql:Enum:gender_type", "male,female")
                .Annotation("Npgsql:Enum:hair_color_type", "dark_purple,yellow,blue,purple,orange,brown,green,dark_grey,light_blue,pink_red,light_yellow,light_pink,light_green,light_grey,sky_blue,black,dark_orange,dark_orange_variant2,dark_orange_variant3,dark_orange_variant4,dark_orange_variant5,dark_orange_variant6,light_orange,light_light_orange,light_light_light_orange,light_light_light_light_orange,super_light_orange,dark_yellow,light_light_yellow,kaki_yellow,super_light_yellow,super_light_yellow2,super_light_yellow3,little_dark_yellow,yellow_variant,yellow_variant1,yellow_variant2,yellow_variant3,yellow_variant4,yellow_variant5,yellow_variant6,yellow_variant7,yellow_variant8,yellow_variant9,green_variant,green_variant1,dark_green_variant,green_more_dark_variant,green_variant2,green_variant3,green_variant4,green_variant5,green_variant6,green_variant7,green_variant8,green_variant9,green_variant10,green_variant11,green_variant12,green_variant13,green_variant14,green_variant15,green_variant16,green_variant17,green_variant18,green_variant19,green_variant20,light_blue_variant1,light_blue_variant2,light_blue_variant3,light_blue_variant4,light_blue_variant5,light_blue_variant6,light_blue_variant7,light_blue_variant8,light_blue_variant9,light_blue_variant10,light_blue_variant11,light_blue_variant12,light_blue_variant13,dark_black,light_blue_variant14,light_blue_variant15,light_blue_variant16,light_blue_variant17,blue_variant,blue_variant_dark,blue_variant_dark_dark,blue_variant_dark_dark2,flash_blue,flash_blue_dark,flash_blue_dark2,flash_blue_dark3,flash_blue_dark4,flash_blue_dark5,flash_blue_dark6,flash_blue_dark7,flash_blue_dark8,flash_blue_dark9,white,flash_blue_dark10,flash_blue1,flash_blue2,flash_blue3,flash_blue4,flash_blue5,flash_purple,flash_light_purple,flash_light_purple2,flash_light_purple3,flash_light_purple4,flash_light_purple5,light_purple,purple_variant1,purple_variant2,purple_variant3,purple_variant4,purple_variant5,purple_variant6,purple_variant7,purple_variant8,purple_variant9,purple_variant10,purple_variant11,purple_variant12,purple_variant13,purple_variant14,purple_variant15")
                .Annotation("Npgsql:Enum:hair_style_type", "hair_style_a,hair_style_b,hair_style_c,hair_style_d,no_hair")
                .Annotation("Npgsql:Enum:item_effect_type", "no_effect,teleport,apply_hair_die,speaker,marriage_proposal,undefined,sp_charger,dropped_sp_recharger,premium_sp_recharger,crafted_sp_recharger,specialist_medal,apply_skin_partner,change_gender,point_initialisation,sealed_tarot_card,tarot_card,red_amulet,blue_amulet,reinforcement_amulet,heroic,random_heroic,attack_amulet,defense_amulet,speed_booster,box_effect,vehicle,gold_nos_merchant_upgrade,silver_nos_merchant_upgrade,inventory_upgrade,pet_space_upgrade,pet_basket_upgrade,pet_backpack_upgrade,inventory_ticket_upgrade,buff_potions,marriage_separation")
                .Annotation("Npgsql:Enum:item_type", "weapon,armor,fashion,jewelery,specialist,box,shell,main,upgrade,production,map,special,potion,event,title,quest1,sell,food,snack,magical,part,teacher,ammo,quest2,house,garden,minigame,terrace,miniland_theme")
                .Annotation("Npgsql:Enum:mate_type", "partner,pet")
                .Annotation("Npgsql:Enum:miniland_state", "open,private,lock")
                .Annotation("Npgsql:Enum:monster_type", "unknown,partner,npc,well,portal,boss,elite,peapod,special,gem_space_time")
                .Annotation("Npgsql:Enum:noscore_pocket_type", "equipment,main,etc,miniland,specialist,costume,wear")
                .Annotation("Npgsql:Enum:penalty_type", "muted,banned,block_exp,block_f_exp,block_rep,warning")
                .Annotation("Npgsql:Enum:portal_type", "ts_normal,closed,open,miniland,ts_end,ts_end_closed,exit,exit_closed,raid,effect,blue_raid,dark_raid,time_space,shop_teleport,map_portal")
                .Annotation("Npgsql:Enum:quest_type", "hunt,special_collect,collect_in_raid,brings,capture_without_getting_the_monster,capture,times_space,product,number_of_kill,target_reput,ts_point,dialog1,collect_in_ts,required,wear,needed,collect,transmit_gold,go_to,collect_map_entity,use,dialog2,un_know,inspect,win_raid,flower_quest")
                .Annotation("Npgsql:Enum:region_type", "en,de,fr,it,pl,es,ru,cs,tr")
                .Annotation("Npgsql:Enum:scripted_instance_type", "time_space,raid,raid_act4")
                .Annotation("Npgsql:Enum:static_bonus_type", "bazaar_medal_gold,bazaar_medal_silver,back_pack,pet_basket,pet_back_pack,inventory_ticket_upgrade")
                .Annotation("Npgsql:Enum:teleporter_type", "teleporter,teleporter_on_map")
                .Annotation("Npgsql:Enum:warehouse_type", "warehouse,family_ware_house,pet_warehouse");

            migrationBuilder.CreateTable(
                name: "Account",
                columns: table => new
                {
                    AccountId = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Authority = table.Column<short>(type: "smallint", nullable: false),
                    Email = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: true),
                    Name = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
                    Password = table.Column<string>(type: "character varying(255)", unicode: false, maxLength: 255, nullable: true),
                    NewAuthPassword = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: true),
                    NewAuthSalt = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: true),
                    RegistrationIp = table.Column<string>(type: "character varying(45)", maxLength: 45, nullable: true),
                    VerificationToken = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: true),
                    Language = table.Column<int>(type: "integer", nullable: false),
                    BankMoney = table.Column<long>(type: "bigint", nullable: false),
                    ItemShopMoney = table.Column<long>(type: "bigint", nullable: false),
                    MfaSecret = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Account", x => x.AccountId);
                });

            migrationBuilder.CreateTable(
                name: "Act",
                columns: table => new
                {
                    ActId = table.Column<byte>(type: "smallint", nullable: false),
                    Title = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
                    Scene = table.Column<byte>(type: "smallint", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Act", x => x.ActId);
                });

            migrationBuilder.CreateTable(
                name: "AuditLog",
                columns: table => new
                {
                    AuditId = table.Column<Guid>(type: "uuid", nullable: false),
                    TargetId = table.Column<string>(type: "character varying(80)", maxLength: 80, nullable: false),
                    TargetType = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    Time = table.Column<Instant>(type: "timestamp with time zone", nullable: false),
                    AuditLogType = table.Column<int>(type: "integer", nullable: false),
                    Value = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AuditLog", x => x.AuditId);
                });

            migrationBuilder.CreateTable(
                name: "Card",
                columns: table => new
                {
                    CardId = table.Column<short>(type: "smallint", nullable: false),
                    Duration = table.Column<int>(type: "integer", nullable: false),
                    EffectId = table.Column<int>(type: "integer", nullable: false),
                    Level = table.Column<byte>(type: "smallint", nullable: false),
                    Name = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
                    Delay = table.Column<int>(type: "integer", nullable: false),
                    TimeoutBuff = table.Column<short>(type: "smallint", nullable: false),
                    TimeoutBuffChance = table.Column<byte>(type: "smallint", nullable: false),
                    BuffType = table.Column<byte>(type: "smallint", nullable: false),
                    Propability = table.Column<byte>(type: "smallint", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Card", x => x.CardId);
                });

            migrationBuilder.CreateTable(
                name: "Family",
                columns: table => new
                {
                    FamilyId = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    FamilyExperience = table.Column<int>(type: "integer", nullable: false),
                    FamilyHeadGender = table.Column<byte>(type: "smallint", nullable: false),
                    FamilyLevel = table.Column<byte>(type: "smallint", nullable: false),
                    FamilyFaction = table.Column<byte>(type: "smallint", nullable: false),
                    FamilyMessage = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: true),
                    ManagerAuthorityType = table.Column<int>(type: "integer", nullable: false),
                    ManagerCanGetHistory = table.Column<bool>(type: "boolean", nullable: false),
                    ManagerCanInvite = table.Column<bool>(type: "boolean", nullable: false),
                    ManagerCanNotice = table.Column<bool>(type: "boolean", nullable: false),
                    ManagerCanShout = table.Column<bool>(type: "boolean", nullable: false),
                    MaxSize = table.Column<byte>(type: "smallint", nullable: false),
                    MemberAuthorityType = table.Column<int>(type: "integer", nullable: false),
                    MemberCanGetHistory = table.Column<bool>(type: "boolean", nullable: false),
                    Name = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
                    WarehouseSize = table.Column<byte>(type: "smallint", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Family", x => x.FamilyId);
                });

            migrationBuilder.CreateTable(
                name: "I18NActDesc",
                columns: table => new
                {
                    I18NActDescId = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Key = table.Column<string>(type: "text", nullable: false),
                    RegionType = table.Column<int>(type: "integer", nullable: false),
                    Text = table.Column<string>(type: "text", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_I18NActDesc", x => x.I18NActDescId);
                });

            migrationBuilder.CreateTable(
                name: "I18NBCard",
                columns: table => new
                {
                    I18NbCardId = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Key = table.Column<string>(type: "text", nullable: false),
                    RegionType = table.Column<int>(type: "integer", nullable: false),
                    Text = table.Column<string>(type: "text", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_I18NBCard", x => x.I18NbCardId);
                });

            migrationBuilder.CreateTable(
                name: "I18NCard",
                columns: table => new
                {
                    I18NCardId = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Key = table.Column<string>(type: "text", nullable: false),
                    RegionType = table.Column<int>(type: "integer", nullable: false),
                    Text = table.Column<string>(type: "text", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_I18NCard", x => x.I18NCardId);
                });

            migrationBuilder.CreateTable(
                name: "I18NItem",
                columns: table => new
                {
                    I18NItemId = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Key = table.Column<string>(type: "text", nullable: false),
                    RegionType = table.Column<int>(type: "integer", nullable: false),
                    Text = table.Column<string>(type: "text", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_I18NItem", x => x.I18NItemId);
                });

            migrationBuilder.CreateTable(
                name: "I18NMapIdData",
                columns: table => new
                {
                    I18NMapIdDataId = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Key = table.Column<string>(type: "text", nullable: false),
                    RegionType = table.Column<int>(type: "integer", nullable: false),
                    Text = table.Column<string>(type: "text", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_I18NMapIdData", x => x.I18NMapIdDataId);
                });

            migrationBuilder.CreateTable(
                name: "I18NMapPointData",
                columns: table => new
                {
                    I18NMapPointDataId = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Key = table.Column<string>(type: "text", nullable: false),
                    RegionType = table.Column<int>(type: "integer", nullable: false),
                    Text = table.Column<string>(type: "text", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_I18NMapPointData", x => x.I18NMapPointDataId);
                });

            migrationBuilder.CreateTable(
                name: "I18NNpcMonster",
                columns: table => new
                {
                    I18NNpcMonsterId = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Key = table.Column<string>(type: "text", nullable: false),
                    RegionType = table.Column<int>(type: "integer", nullable: false),
                    Text = table.Column<string>(type: "text", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_I18NNpcMonster", x => x.I18NNpcMonsterId);
                });

            migrationBuilder.CreateTable(
                name: "I18NNpcMonsterTalk",
                columns: table => new
                {
                    I18NNpcMonsterTalkId = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Key = table.Column<string>(type: "text", nullable: false),
                    RegionType = table.Column<int>(type: "integer", nullable: false),
                    Text = table.Column<string>(type: "text", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_I18NNpcMonsterTalk", x => x.I18NNpcMonsterTalkId);
                });

            migrationBuilder.CreateTable(
                name: "I18NQuest",
                columns: table => new
                {
                    I18NQuestId = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Key = table.Column<string>(type: "text", nullable: false),
                    RegionType = table.Column<int>(type: "integer", nullable: false),
                    Text = table.Column<string>(type: "text", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_I18NQuest", x => x.I18NQuestId);
                });

            migrationBuilder.CreateTable(
                name: "I18NSkill",
                columns: table => new
                {
                    I18NSkillId = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Key = table.Column<string>(type: "text", nullable: false),
                    RegionType = table.Column<int>(type: "integer", nullable: false),
                    Text = table.Column<string>(type: "text", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_I18NSkill", x => x.I18NSkillId);
                });

            migrationBuilder.CreateTable(
                name: "Item",
                columns: table => new
                {
                    VNum = table.Column<short>(type: "smallint", nullable: false),
                    BasicUpgrade = table.Column<byte>(type: "smallint", nullable: false),
                    Flag1 = table.Column<bool>(type: "boolean", nullable: false),
                    Flag2 = table.Column<bool>(type: "boolean", nullable: false),
                    Flag3 = table.Column<bool>(type: "boolean", nullable: false),
                    Flag4 = table.Column<bool>(type: "boolean", nullable: false),
                    RequireBinding = table.Column<bool>(type: "boolean", nullable: false),
                    Flag6 = table.Column<bool>(type: "boolean", nullable: false),
                    Flag7 = table.Column<bool>(type: "boolean", nullable: false),
                    Flag8 = table.Column<bool>(type: "boolean", nullable: false),
                    CellonLvl = table.Column<byte>(type: "smallint", nullable: false),
                    Class = table.Column<byte>(type: "smallint", nullable: false),
                    CloseDefence = table.Column<short>(type: "smallint", nullable: false),
                    Color = table.Column<byte>(type: "smallint", nullable: false),
                    Concentrate = table.Column<short>(type: "smallint", nullable: false),
                    CriticalLuckRate = table.Column<byte>(type: "smallint", nullable: false),
                    CriticalRate = table.Column<short>(type: "smallint", nullable: false),
                    DamageMaximum = table.Column<short>(type: "smallint", nullable: false),
                    DamageMinimum = table.Column<short>(type: "smallint", nullable: false),
                    DarkElement = table.Column<byte>(type: "smallint", nullable: false),
                    DarkResistance = table.Column<short>(type: "smallint", nullable: false),
                    DefenceDodge = table.Column<short>(type: "smallint", nullable: false),
                    DistanceDefence = table.Column<short>(type: "smallint", nullable: false),
                    DistanceDefenceDodge = table.Column<short>(type: "smallint", nullable: false),
                    Effect = table.Column<int>(type: "integer", nullable: false),
                    EffectValue = table.Column<int>(type: "integer", nullable: false),
                    Element = table.Column<int>(type: "integer", nullable: false),
                    ElementRate = table.Column<short>(type: "smallint", nullable: false),
                    EquipmentSlot = table.Column<byte>(type: "smallint", nullable: false),
                    FireElement = table.Column<byte>(type: "smallint", nullable: false),
                    FireResistance = table.Column<short>(type: "smallint", nullable: false),
                    Height = table.Column<byte>(type: "smallint", nullable: false),
                    HitRate = table.Column<short>(type: "smallint", nullable: false),
                    Hp = table.Column<short>(type: "smallint", nullable: false),
                    HpRegeneration = table.Column<short>(type: "smallint", nullable: false),
                    IsMinilandActionable = table.Column<bool>(type: "boolean", nullable: false),
                    IsColored = table.Column<bool>(type: "boolean", nullable: false),
                    IsConsumable = table.Column<bool>(type: "boolean", nullable: false),
                    IsDroppable = table.Column<bool>(type: "boolean", nullable: false),
                    IsHeroic = table.Column<bool>(type: "boolean", nullable: false),
                    Flag9 = table.Column<bool>(type: "boolean", nullable: false),
                    IsWarehouse = table.Column<bool>(type: "boolean", nullable: false),
                    IsSoldable = table.Column<bool>(type: "boolean", nullable: false),
                    IsTradable = table.Column<bool>(type: "boolean", nullable: false),
                    ItemSubType = table.Column<byte>(type: "smallint", nullable: false),
                    ItemType = table.Column<byte>(type: "smallint", nullable: false),
                    ItemValidTime = table.Column<long>(type: "bigint", nullable: false),
                    LevelJobMinimum = table.Column<byte>(type: "smallint", nullable: false),
                    LevelMinimum = table.Column<byte>(type: "smallint", nullable: false),
                    LightElement = table.Column<byte>(type: "smallint", nullable: false),
                    LightResistance = table.Column<short>(type: "smallint", nullable: false),
                    MagicDefence = table.Column<short>(type: "smallint", nullable: false),
                    MaxCellon = table.Column<byte>(type: "smallint", nullable: false),
                    MaxCellonLvl = table.Column<byte>(type: "smallint", nullable: false),
                    MaxElementRate = table.Column<short>(type: "smallint", nullable: false),
                    MaximumAmmo = table.Column<byte>(type: "smallint", nullable: false),
                    MinilandObjectPoint = table.Column<int>(type: "integer", nullable: false),
                    MoreHp = table.Column<short>(type: "smallint", nullable: false),
                    MoreMp = table.Column<short>(type: "smallint", nullable: false),
                    Morph = table.Column<short>(type: "smallint", nullable: false),
                    SecondMorph = table.Column<short>(type: "smallint", nullable: false),
                    Mp = table.Column<short>(type: "smallint", nullable: false),
                    MpRegeneration = table.Column<short>(type: "smallint", nullable: false),
                    Name = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
                    Price = table.Column<long>(type: "bigint", nullable: false),
                    PvpDefence = table.Column<short>(type: "smallint", nullable: false),
                    PvpStrength = table.Column<byte>(type: "smallint", nullable: false),
                    ReduceOposantResistance = table.Column<short>(type: "smallint", nullable: false),
                    ReputationMinimum = table.Column<byte>(type: "smallint", nullable: false),
                    ReputPrice = table.Column<long>(type: "bigint", nullable: false),
                    SecondaryElement = table.Column<int>(type: "integer", nullable: false),
                    Sex = table.Column<byte>(type: "smallint", nullable: false),
                    Speed = table.Column<byte>(type: "smallint", nullable: false),
                    SpType = table.Column<byte>(type: "smallint", nullable: false),
                    Type = table.Column<byte>(type: "smallint", nullable: false),
                    WaitDelay = table.Column<short>(type: "smallint", nullable: false),
                    WaterElement = table.Column<byte>(type: "smallint", nullable: false),
                    WaterResistance = table.Column<short>(type: "smallint", nullable: false),
                    Width = table.Column<byte>(type: "smallint", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Item", x => x.VNum);
                });

            migrationBuilder.CreateTable(
                name: "Map",
                columns: table => new
                {
                    MapId = table.Column<short>(type: "smallint", nullable: false),
                    Data = table.Column<byte[]>(type: "bytea", nullable: false),
                    Music = table.Column<int>(type: "integer", nullable: false),
                    Name = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
                    ShopAllowed = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Map", x => x.MapId);
                });

            migrationBuilder.CreateTable(
                name: "NpcMonster",
                columns: table => new
                {
                    NpcMonsterVNum = table.Column<short>(type: "smallint", nullable: false),
                    AmountRequired = table.Column<byte>(type: "smallint", nullable: false),
                    AttackClass = table.Column<byte>(type: "smallint", nullable: false),
                    AttackUpgrade = table.Column<byte>(type: "smallint", nullable: false),
                    BasicArea = table.Column<byte>(type: "smallint", nullable: false),
                    BasicCooldown = table.Column<short>(type: "smallint", nullable: false),
                    BasicRange = table.Column<byte>(type: "smallint", nullable: false),
                    BasicSkill = table.Column<short>(type: "smallint", nullable: false),
                    CloseDefence = table.Column<short>(type: "smallint", nullable: false),
                    Concentrate = table.Column<short>(type: "smallint", nullable: false),
                    CriticalChance = table.Column<byte>(type: "smallint", nullable: false),
                    CriticalRate = table.Column<short>(type: "smallint", nullable: false),
                    DamageMaximum = table.Column<short>(type: "smallint", nullable: false),
                    DamageMinimum = table.Column<short>(type: "smallint", nullable: false),
                    DarkResistance = table.Column<short>(type: "smallint", nullable: false),
                    DefenceDodge = table.Column<short>(type: "smallint", nullable: false),
                    DefenceUpgrade = table.Column<byte>(type: "smallint", nullable: false),
                    DistanceDefence = table.Column<short>(type: "smallint", nullable: false),
                    DistanceDefenceDodge = table.Column<short>(type: "smallint", nullable: false),
                    Element = table.Column<byte>(type: "smallint", nullable: false),
                    ElementRate = table.Column<short>(type: "smallint", nullable: false),
                    FireResistance = table.Column<short>(type: "smallint", nullable: false),
                    HeroLevel = table.Column<byte>(type: "smallint", nullable: false),
                    HeroXp = table.Column<int>(type: "integer", nullable: false),
                    IsHostile = table.Column<bool>(type: "boolean", nullable: false),
                    JobXp = table.Column<int>(type: "integer", nullable: false),
                    Level = table.Column<byte>(type: "smallint", nullable: false),
                    LightResistance = table.Column<short>(type: "smallint", nullable: false),
                    MagicDefence = table.Column<short>(type: "smallint", nullable: false),
                    MaxHp = table.Column<int>(type: "integer", nullable: false),
                    MaxMp = table.Column<int>(type: "integer", nullable: false),
                    MonsterType = table.Column<byte>(type: "smallint", nullable: false),
                    Name = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
                    NoAggresiveIcon = table.Column<bool>(type: "boolean", nullable: false),
                    NoticeRange = table.Column<byte>(type: "smallint", nullable: false),
                    Race = table.Column<byte>(type: "smallint", nullable: false),
                    RaceType = table.Column<byte>(type: "smallint", nullable: false),
                    RespawnTime = table.Column<int>(type: "integer", nullable: false),
                    Speed = table.Column<byte>(type: "smallint", nullable: false),
                    VNumRequired = table.Column<short>(type: "smallint", nullable: false),
                    WaterResistance = table.Column<short>(type: "smallint", nullable: false),
                    Xp = table.Column<int>(type: "integer", nullable: false),
                    IsPercent = table.Column<bool>(type: "boolean", nullable: false),
                    TakeDamages = table.Column<int>(type: "integer", nullable: false),
                    GiveDamagePercentage = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_NpcMonster", x => x.NpcMonsterVNum);
                });

            migrationBuilder.CreateTable(
                name: "NpcTalk",
                columns: table => new
                {
                    DialogId = table.Column<short>(type: "smallint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Name = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_NpcTalk", x => x.DialogId);
                });

            migrationBuilder.CreateTable(
                name: "Quest",
                columns: table => new
                {
                    QuestId = table.Column<short>(type: "smallint", nullable: false),
                    QuestType = table.Column<short>(type: "smallint", nullable: false),
                    LevelMin = table.Column<byte>(type: "smallint", nullable: false),
                    LevelMax = table.Column<byte>(type: "smallint", nullable: false),
                    StartDialogId = table.Column<int>(type: "integer", nullable: true),
                    EndDialogId = table.Column<int>(type: "integer", nullable: true),
                    TargetMap = table.Column<short>(type: "smallint", nullable: true),
                    TargetX = table.Column<short>(type: "smallint", nullable: true),
                    TargetY = table.Column<short>(type: "smallint", nullable: true),
                    NextQuestId = table.Column<short>(type: "smallint", nullable: true),
                    IsDaily = table.Column<bool>(type: "boolean", nullable: false),
                    AutoFinish = table.Column<bool>(type: "boolean", nullable: false),
                    IsSecondary = table.Column<bool>(type: "boolean", nullable: false),
                    SpecialData = table.Column<int>(type: "integer", nullable: true),
                    RequiredQuestId = table.Column<short>(type: "smallint", nullable: true),
                    Title = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
                    Desc = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Quest", x => x.QuestId);
                });

            migrationBuilder.CreateTable(
                name: "QuestReward",
                columns: table => new
                {
                    QuestRewardId = table.Column<short>(type: "smallint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    RewardType = table.Column<byte>(type: "smallint", nullable: false),
                    Data = table.Column<int>(type: "integer", nullable: false),
                    Design = table.Column<byte>(type: "smallint", nullable: false),
                    Rarity = table.Column<byte>(type: "smallint", nullable: false),
                    Upgrade = table.Column<byte>(type: "smallint", nullable: false),
                    Amount = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_QuestReward", x => x.QuestRewardId);
                });

            migrationBuilder.CreateTable(
                name: "Script",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    ScriptId = table.Column<byte>(type: "smallint", nullable: false),
                    ScriptStepId = table.Column<short>(type: "smallint", nullable: false),
                    StepType = table.Column<string>(type: "text", nullable: false),
                    StringArgument = table.Column<string>(type: "text", nullable: true),
                    Argument1 = table.Column<short>(type: "smallint", nullable: true),
                    Argument2 = table.Column<short>(type: "smallint", nullable: true),
                    Argument3 = table.Column<short>(type: "smallint", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Script", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Skill",
                columns: table => new
                {
                    SkillVNum = table.Column<short>(type: "smallint", nullable: false),
                    AttackAnimation = table.Column<short>(type: "smallint", nullable: false),
                    CastAnimation = table.Column<short>(type: "smallint", nullable: false),
                    CastEffect = table.Column<short>(type: "smallint", nullable: false),
                    CastId = table.Column<short>(type: "smallint", nullable: false),
                    CastTime = table.Column<short>(type: "smallint", nullable: false),
                    Class = table.Column<byte>(type: "smallint", nullable: false),
                    Cooldown = table.Column<short>(type: "smallint", nullable: false),
                    CpCost = table.Column<byte>(type: "smallint", nullable: false),
                    Duration = table.Column<short>(type: "smallint", nullable: false),
                    Effect = table.Column<short>(type: "smallint", nullable: false),
                    Element = table.Column<byte>(type: "smallint", nullable: false),
                    HitType = table.Column<byte>(type: "smallint", nullable: false),
                    ItemVNum = table.Column<short>(type: "smallint", nullable: false),
                    Level = table.Column<byte>(type: "smallint", nullable: false),
                    LevelMinimum = table.Column<byte>(type: "smallint", nullable: false),
                    MinimumAdventurerLevel = table.Column<byte>(type: "smallint", nullable: false),
                    MinimumArcherLevel = table.Column<byte>(type: "smallint", nullable: false),
                    MinimumMagicianLevel = table.Column<byte>(type: "smallint", nullable: false),
                    MinimumSwordmanLevel = table.Column<byte>(type: "smallint", nullable: false),
                    MpCost = table.Column<short>(type: "smallint", nullable: false),
                    Name = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
                    Price = table.Column<int>(type: "integer", nullable: false),
                    Range = table.Column<byte>(type: "smallint", nullable: false),
                    SkillType = table.Column<byte>(type: "smallint", nullable: false),
                    TargetRange = table.Column<byte>(type: "smallint", nullable: false),
                    TargetType = table.Column<byte>(type: "smallint", nullable: false),
                    Type = table.Column<byte>(type: "smallint", nullable: false),
                    UpgradeSkill = table.Column<short>(type: "smallint", nullable: false),
                    UpgradeType = table.Column<short>(type: "smallint", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Skill", x => x.SkillVNum);
                });

            migrationBuilder.CreateTable(
                name: "PenaltyLog",
                columns: table => new
                {
                    PenaltyLogId = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    AccountId = table.Column<long>(type: "bigint", nullable: false),
                    AdminName = table.Column<string>(type: "text", nullable: false),
                    DateEnd = table.Column<Instant>(type: "timestamp with time zone", nullable: false),
                    DateStart = table.Column<Instant>(type: "timestamp with time zone", nullable: false),
                    Penalty = table.Column<byte>(type: "smallint", nullable: false),
                    Reason = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false)
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
                    ActPartId = table.Column<byte>(type: "smallint", nullable: false),
                    ActPartNumber = table.Column<byte>(type: "smallint", nullable: false),
                    ActId = table.Column<byte>(type: "smallint", nullable: false),
                    MaxTs = table.Column<byte>(type: "smallint", nullable: false)
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
                    FamilyLogId = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    FamilyId = table.Column<long>(type: "bigint", nullable: false),
                    FamilyLogData = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: true),
                    FamilyLogType = table.Column<byte>(type: "smallint", nullable: false),
                    Timestamp = table.Column<Instant>(type: "timestamp with time zone", nullable: false)
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
                    RollGeneratedItemId = table.Column<short>(type: "smallint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    OriginalItemDesign = table.Column<short>(type: "smallint", nullable: false),
                    OriginalItemVNum = table.Column<short>(type: "smallint", nullable: false),
                    Probability = table.Column<short>(type: "smallint", nullable: false),
                    ItemGeneratedAmount = table.Column<byte>(type: "smallint", nullable: false),
                    ItemGeneratedVNum = table.Column<short>(type: "smallint", nullable: false),
                    ItemGeneratedUpgrade = table.Column<byte>(type: "smallint", nullable: false),
                    IsRareRandom = table.Column<bool>(type: "boolean", nullable: false),
                    MinimumOriginalItemRare = table.Column<short>(type: "smallint", nullable: false),
                    MaximumOriginalItemRare = table.Column<short>(type: "smallint", nullable: false),
                    IsSuperReward = table.Column<bool>(type: "boolean", nullable: false)
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
                name: "Portal",
                columns: table => new
                {
                    PortalId = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    DestinationMapId = table.Column<short>(type: "smallint", nullable: false),
                    DestinationX = table.Column<short>(type: "smallint", nullable: false),
                    DestinationY = table.Column<short>(type: "smallint", nullable: false),
                    IsDisabled = table.Column<bool>(type: "boolean", nullable: false),
                    SourceMapId = table.Column<short>(type: "smallint", nullable: false),
                    SourceX = table.Column<short>(type: "smallint", nullable: false),
                    SourceY = table.Column<short>(type: "smallint", nullable: false),
                    Type = table.Column<short>(type: "smallint", nullable: false)
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
                    RespawnMapTypeId = table.Column<long>(type: "bigint", nullable: false),
                    MapId = table.Column<short>(type: "smallint", nullable: false),
                    DefaultX = table.Column<short>(type: "smallint", nullable: false),
                    DefaultY = table.Column<short>(type: "smallint", nullable: false),
                    Name = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: true)
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
                    ScriptedInstanceId = table.Column<short>(type: "smallint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    MapId = table.Column<short>(type: "smallint", nullable: false),
                    PositionX = table.Column<short>(type: "smallint", nullable: false),
                    PositionY = table.Column<short>(type: "smallint", nullable: false),
                    Label = table.Column<string>(type: "text", nullable: true),
                    Script = table.Column<string>(type: "text", maxLength: 2147483647, nullable: true),
                    Type = table.Column<byte>(type: "smallint", nullable: false)
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
                    MapMonsterId = table.Column<int>(type: "integer", nullable: false),
                    IsDisabled = table.Column<bool>(type: "boolean", nullable: false),
                    IsMoving = table.Column<bool>(type: "boolean", nullable: false),
                    MapId = table.Column<short>(type: "smallint", nullable: false),
                    MapX = table.Column<short>(type: "smallint", nullable: false),
                    MapY = table.Column<short>(type: "smallint", nullable: false),
                    VNum = table.Column<short>(type: "smallint", nullable: false),
                    Direction = table.Column<byte>(type: "smallint", nullable: false)
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
                    MapNpcId = table.Column<int>(type: "integer", nullable: false),
                    Dialog = table.Column<short>(type: "smallint", nullable: true),
                    Effect = table.Column<short>(type: "smallint", nullable: false),
                    EffectDelay = table.Column<short>(type: "smallint", nullable: false),
                    IsDisabled = table.Column<bool>(type: "boolean", nullable: false),
                    IsMoving = table.Column<bool>(type: "boolean", nullable: false),
                    IsSitting = table.Column<bool>(type: "boolean", nullable: false),
                    MapId = table.Column<short>(type: "smallint", nullable: false),
                    MapX = table.Column<short>(type: "smallint", nullable: false),
                    MapY = table.Column<short>(type: "smallint", nullable: false),
                    VNum = table.Column<short>(type: "smallint", nullable: false),
                    Direction = table.Column<byte>(type: "smallint", nullable: false)
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
                    table.ForeignKey(
                        name: "FK_MapNpc_NpcTalk_Dialog",
                        column: x => x.Dialog,
                        principalTable: "NpcTalk",
                        principalColumn: "DialogId",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "QuestObjective",
                columns: table => new
                {
                    QuestObjectiveId = table.Column<Guid>(type: "uuid", nullable: false),
                    FirstData = table.Column<int>(type: "integer", nullable: false),
                    SecondData = table.Column<int>(type: "integer", nullable: true),
                    ThirdData = table.Column<int>(type: "integer", nullable: true),
                    FourthData = table.Column<int>(type: "integer", nullable: true),
                    QuestId = table.Column<short>(type: "smallint", nullable: false)
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
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    QuestRewardId = table.Column<short>(type: "smallint", nullable: false),
                    QuestId = table.Column<short>(type: "smallint", nullable: false)
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
                name: "Character",
                columns: table => new
                {
                    CharacterId = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    ServerId = table.Column<byte>(type: "smallint", nullable: false),
                    AccountId = table.Column<long>(type: "bigint", nullable: false),
                    Act4Dead = table.Column<int>(type: "integer", nullable: false),
                    Act4Kill = table.Column<int>(type: "integer", nullable: false),
                    Act4Points = table.Column<int>(type: "integer", nullable: false),
                    ArenaWinner = table.Column<int>(type: "integer", nullable: false),
                    CurrentScriptId = table.Column<Guid>(type: "uuid", nullable: true),
                    Biography = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: true),
                    BuffBlocked = table.Column<bool>(type: "boolean", nullable: false),
                    Class = table.Column<byte>(type: "smallint", nullable: false),
                    Compliment = table.Column<short>(type: "smallint", nullable: false),
                    Dignity = table.Column<short>(type: "smallint", nullable: false),
                    Elo = table.Column<int>(type: "integer", nullable: false),
                    EmoticonsBlocked = table.Column<bool>(type: "boolean", nullable: false),
                    ExchangeBlocked = table.Column<bool>(type: "boolean", nullable: false),
                    ShouldRename = table.Column<bool>(type: "boolean", nullable: false),
                    Faction = table.Column<byte>(type: "smallint", nullable: false),
                    FamilyRequestBlocked = table.Column<bool>(type: "boolean", nullable: false),
                    FriendRequestBlocked = table.Column<bool>(type: "boolean", nullable: false),
                    Gender = table.Column<byte>(type: "smallint", nullable: false),
                    Gold = table.Column<long>(type: "bigint", nullable: false),
                    GroupRequestBlocked = table.Column<bool>(type: "boolean", nullable: false),
                    HairColor = table.Column<byte>(type: "smallint", nullable: false),
                    HairStyle = table.Column<byte>(type: "smallint", nullable: false),
                    HeroChatBlocked = table.Column<bool>(type: "boolean", nullable: false),
                    HeroLevel = table.Column<byte>(type: "smallint", nullable: false),
                    HeroXp = table.Column<long>(type: "bigint", nullable: false),
                    Hp = table.Column<int>(type: "integer", nullable: false),
                    HpBlocked = table.Column<bool>(type: "boolean", nullable: false),
                    JobLevel = table.Column<byte>(type: "smallint", nullable: false),
                    JobLevelXp = table.Column<long>(type: "bigint", nullable: false),
                    Level = table.Column<byte>(type: "smallint", nullable: false),
                    LevelXp = table.Column<long>(type: "bigint", nullable: false),
                    MapId = table.Column<short>(type: "smallint", nullable: false),
                    MapX = table.Column<short>(type: "smallint", nullable: false),
                    MapY = table.Column<short>(type: "smallint", nullable: false),
                    MasterPoints = table.Column<int>(type: "integer", nullable: false),
                    MasterTicket = table.Column<int>(type: "integer", nullable: false),
                    MaxMateCount = table.Column<byte>(type: "smallint", nullable: false),
                    MinilandInviteBlocked = table.Column<bool>(type: "boolean", nullable: false),
                    MouseAimLock = table.Column<bool>(type: "boolean", nullable: false),
                    Mp = table.Column<int>(type: "integer", nullable: false),
                    Prefix = table.Column<string>(type: "character varying(25)", maxLength: 25, nullable: true),
                    Name = table.Column<string>(type: "character varying(255)", unicode: false, maxLength: 255, nullable: false),
                    QuickGetUp = table.Column<bool>(type: "boolean", nullable: false),
                    RagePoint = table.Column<long>(type: "bigint", nullable: false),
                    Reput = table.Column<long>(type: "bigint", nullable: false),
                    Slot = table.Column<byte>(type: "smallint", nullable: false),
                    SpAdditionPoint = table.Column<int>(type: "integer", nullable: false),
                    SpPoint = table.Column<int>(type: "integer", nullable: false),
                    State = table.Column<byte>(type: "smallint", nullable: false),
                    TalentLose = table.Column<int>(type: "integer", nullable: false),
                    TalentSurrender = table.Column<int>(type: "integer", nullable: false),
                    TalentWin = table.Column<int>(type: "integer", nullable: false),
                    WhisperBlocked = table.Column<bool>(type: "boolean", nullable: false)
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
                    table.ForeignKey(
                        name: "FK_Character_Script_CurrentScriptId",
                        column: x => x.CurrentScriptId,
                        principalTable: "Script",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "BCard",
                columns: table => new
                {
                    BCardId = table.Column<short>(type: "smallint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    SubType = table.Column<byte>(type: "smallint", nullable: false),
                    Type = table.Column<byte>(type: "smallint", nullable: false),
                    FirstData = table.Column<int>(type: "integer", nullable: false),
                    SecondData = table.Column<int>(type: "integer", nullable: false),
                    CardId = table.Column<short>(type: "smallint", nullable: true),
                    ItemVNum = table.Column<short>(type: "smallint", nullable: true),
                    SkillVNum = table.Column<short>(type: "smallint", nullable: true),
                    NpcMonsterVNum = table.Column<short>(type: "smallint", nullable: true),
                    CastType = table.Column<byte>(type: "smallint", nullable: false),
                    ThirdData = table.Column<int>(type: "integer", nullable: false),
                    IsLevelScaled = table.Column<bool>(type: "boolean", nullable: false),
                    IsLevelDivided = table.Column<bool>(type: "boolean", nullable: false)
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
                    ComboId = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Animation = table.Column<short>(type: "smallint", nullable: false),
                    Effect = table.Column<short>(type: "smallint", nullable: false),
                    Hit = table.Column<short>(type: "smallint", nullable: false),
                    SkillVNum = table.Column<short>(type: "smallint", nullable: false)
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
                    NpcMonsterSkillId = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    NpcMonsterVNum = table.Column<short>(type: "smallint", nullable: false),
                    Rate = table.Column<short>(type: "smallint", nullable: false),
                    SkillVNum = table.Column<short>(type: "smallint", nullable: false)
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
                name: "MapType",
                columns: table => new
                {
                    MapTypeId = table.Column<short>(type: "smallint", nullable: false),
                    MapTypeName = table.Column<string>(type: "text", nullable: false),
                    PotionDelay = table.Column<short>(type: "smallint", nullable: false),
                    RespawnMapTypeId = table.Column<long>(type: "bigint", nullable: true),
                    ReturnMapTypeId = table.Column<long>(type: "bigint", nullable: true)
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
                name: "Recipe",
                columns: table => new
                {
                    RecipeId = table.Column<short>(type: "smallint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Amount = table.Column<byte>(type: "smallint", nullable: false),
                    ItemVNum = table.Column<short>(type: "smallint", nullable: false),
                    MapNpcId = table.Column<int>(type: "integer", nullable: false)
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
                    ShopId = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    MapNpcId = table.Column<int>(type: "integer", nullable: false),
                    MenuType = table.Column<byte>(type: "smallint", nullable: false),
                    ShopType = table.Column<byte>(type: "smallint", nullable: false)
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
                    TeleporterId = table.Column<short>(type: "smallint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Index = table.Column<short>(type: "smallint", nullable: false),
                    Type = table.Column<byte>(type: "smallint", nullable: false),
                    MapId = table.Column<short>(type: "smallint", nullable: false),
                    MapNpcId = table.Column<int>(type: "integer", nullable: false),
                    MapX = table.Column<short>(type: "smallint", nullable: false),
                    MapY = table.Column<short>(type: "smallint", nullable: false)
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
                name: "CharacterActPart",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    CharacterId = table.Column<long>(type: "bigint", nullable: false),
                    ActPartId = table.Column<byte>(type: "smallint", nullable: false)
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
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    CharacterId = table.Column<long>(type: "bigint", nullable: false),
                    QuestId = table.Column<short>(type: "smallint", nullable: false),
                    CompletedOn = table.Column<Instant>(type: "timestamp with time zone", nullable: true)
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
                    CharacterRelationId = table.Column<Guid>(type: "uuid", nullable: false),
                    CharacterId = table.Column<long>(type: "bigint", nullable: false),
                    RelatedCharacterId = table.Column<long>(type: "bigint", nullable: false),
                    RelationType = table.Column<short>(type: "smallint", nullable: false)
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
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    CharacterId = table.Column<long>(type: "bigint", nullable: false),
                    SkillVNum = table.Column<short>(type: "smallint", nullable: false)
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
                    FamilyCharacterId = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Authority = table.Column<byte>(type: "smallint", nullable: false),
                    CharacterId = table.Column<long>(type: "bigint", nullable: false),
                    DailyMessage = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: true),
                    Experience = table.Column<int>(type: "integer", nullable: false),
                    FamilyId = table.Column<long>(type: "bigint", nullable: false),
                    Rank = table.Column<byte>(type: "smallint", nullable: false)
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
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Amount = table.Column<short>(type: "smallint", nullable: false),
                    BoundCharacterId = table.Column<long>(type: "bigint", nullable: true),
                    CharacterId = table.Column<long>(type: "bigint", nullable: false),
                    Design = table.Column<short>(type: "smallint", nullable: false),
                    DurabilityPoint = table.Column<int>(type: "integer", nullable: false),
                    ItemDeleteTime = table.Column<Instant>(type: "timestamp with time zone", nullable: true),
                    ItemVNum = table.Column<short>(type: "smallint", nullable: false),
                    Upgrade = table.Column<byte>(type: "smallint", nullable: false),
                    Rare = table.Column<short>(type: "smallint", nullable: false),
                    Discriminator = table.Column<string>(type: "text", nullable: false),
                    Hp = table.Column<short>(type: "smallint", nullable: true),
                    Mp = table.Column<short>(type: "smallint", nullable: true),
                    Ammo = table.Column<byte>(type: "smallint", nullable: true),
                    Cellon = table.Column<byte>(type: "smallint", nullable: true),
                    CloseDefence = table.Column<short>(type: "smallint", nullable: true),
                    Concentrate = table.Column<short>(type: "smallint", nullable: true),
                    CriticalDodge = table.Column<short>(type: "smallint", nullable: true),
                    CriticalLuckRate = table.Column<byte>(type: "smallint", nullable: true),
                    CriticalRate = table.Column<short>(type: "smallint", nullable: true),
                    DamageMaximum = table.Column<short>(type: "smallint", nullable: true),
                    DamageMinimum = table.Column<short>(type: "smallint", nullable: true),
                    DarkElement = table.Column<byte>(type: "smallint", nullable: true),
                    DarkResistance = table.Column<short>(type: "smallint", nullable: true),
                    DefenceDodge = table.Column<short>(type: "smallint", nullable: true),
                    DistanceDefence = table.Column<short>(type: "smallint", nullable: true),
                    DistanceDefenceDodge = table.Column<short>(type: "smallint", nullable: true),
                    ElementRate = table.Column<short>(type: "smallint", nullable: true),
                    FireElement = table.Column<byte>(type: "smallint", nullable: true),
                    FireResistance = table.Column<short>(type: "smallint", nullable: true),
                    HitRate = table.Column<short>(type: "smallint", nullable: true),
                    WearableInstance_Hp = table.Column<short>(type: "smallint", nullable: true),
                    IsEmpty = table.Column<bool>(type: "boolean", nullable: true),
                    IsFixed = table.Column<bool>(type: "boolean", nullable: true),
                    LightElement = table.Column<byte>(type: "smallint", nullable: true),
                    LightResistance = table.Column<short>(type: "smallint", nullable: true),
                    MagicDefence = table.Column<short>(type: "smallint", nullable: true),
                    MaxElementRate = table.Column<short>(type: "smallint", nullable: true),
                    WearableInstance_Mp = table.Column<short>(type: "smallint", nullable: true),
                    ShellRarity = table.Column<byte>(type: "smallint", nullable: true),
                    WaterElement = table.Column<byte>(type: "smallint", nullable: true),
                    WaterResistance = table.Column<short>(type: "smallint", nullable: true),
                    Xp = table.Column<long>(type: "bigint", nullable: true),
                    SlDamage = table.Column<short>(type: "smallint", nullable: true),
                    SlDefence = table.Column<short>(type: "smallint", nullable: true),
                    SlElement = table.Column<short>(type: "smallint", nullable: true),
                    SlHp = table.Column<short>(type: "smallint", nullable: true),
                    SpDamage = table.Column<byte>(type: "smallint", nullable: true),
                    SpDark = table.Column<byte>(type: "smallint", nullable: true),
                    SpDefence = table.Column<byte>(type: "smallint", nullable: true),
                    SpElement = table.Column<byte>(type: "smallint", nullable: true),
                    SpFire = table.Column<byte>(type: "smallint", nullable: true),
                    SpHp = table.Column<byte>(type: "smallint", nullable: true),
                    SpLevel = table.Column<byte>(type: "smallint", nullable: true),
                    SpLight = table.Column<byte>(type: "smallint", nullable: true),
                    SpStoneUpgrade = table.Column<byte>(type: "smallint", nullable: true),
                    SpWater = table.Column<byte>(type: "smallint", nullable: true),
                    HoldingVNum = table.Column<short>(type: "smallint", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ItemInstance", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ItemInstance_Character_BoundCharacterId",
                        column: x => x.BoundCharacterId,
                        principalTable: "Character",
                        principalColumn: "CharacterId");
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
                    MateId = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Attack = table.Column<byte>(type: "smallint", nullable: false),
                    CanPickUp = table.Column<bool>(type: "boolean", nullable: false),
                    CharacterId = table.Column<long>(type: "bigint", nullable: false),
                    Defence = table.Column<byte>(type: "smallint", nullable: false),
                    Direction = table.Column<byte>(type: "smallint", nullable: false),
                    Experience = table.Column<long>(type: "bigint", nullable: false),
                    Hp = table.Column<int>(type: "integer", nullable: false),
                    IsSummonable = table.Column<bool>(type: "boolean", nullable: false),
                    IsTeamMember = table.Column<bool>(type: "boolean", nullable: false),
                    Level = table.Column<byte>(type: "smallint", nullable: false),
                    Loyalty = table.Column<short>(type: "smallint", nullable: false),
                    MapX = table.Column<short>(type: "smallint", nullable: false),
                    MapY = table.Column<short>(type: "smallint", nullable: false),
                    MateType = table.Column<byte>(type: "smallint", nullable: false),
                    Mp = table.Column<int>(type: "integer", nullable: false),
                    Name = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: true),
                    VNum = table.Column<short>(type: "smallint", nullable: false),
                    Skin = table.Column<short>(type: "smallint", nullable: false)
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
                    MinilandId = table.Column<Guid>(type: "uuid", nullable: false),
                    MinilandMessage = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: true),
                    MinilandPoint = table.Column<long>(type: "bigint", nullable: false),
                    State = table.Column<byte>(type: "smallint", nullable: false),
                    OwnerId = table.Column<long>(type: "bigint", nullable: false),
                    DailyVisitCount = table.Column<int>(type: "integer", nullable: false),
                    VisitCount = table.Column<int>(type: "integer", nullable: false),
                    WelcomeMusicInfo = table.Column<short>(type: "smallint", nullable: false)
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
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    CharacterId = table.Column<long>(type: "bigint", nullable: false),
                    Morph = table.Column<short>(type: "smallint", nullable: false),
                    IconVNum = table.Column<short>(type: "smallint", nullable: false),
                    QuickListIndex = table.Column<short>(type: "smallint", nullable: false),
                    Slot = table.Column<short>(type: "smallint", nullable: false),
                    IconType = table.Column<short>(type: "smallint", nullable: false),
                    Type = table.Column<short>(type: "smallint", nullable: false)
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
                name: "Respawn",
                columns: table => new
                {
                    RespawnId = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    CharacterId = table.Column<long>(type: "bigint", nullable: false),
                    MapId = table.Column<short>(type: "smallint", nullable: false),
                    RespawnMapTypeId = table.Column<long>(type: "bigint", nullable: false),
                    X = table.Column<short>(type: "smallint", nullable: false),
                    Y = table.Column<short>(type: "smallint", nullable: false)
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
                name: "StaticBonus",
                columns: table => new
                {
                    StaticBonusId = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    CharacterId = table.Column<long>(type: "bigint", nullable: false),
                    DateEnd = table.Column<Instant>(type: "timestamp with time zone", nullable: true),
                    StaticBonusType = table.Column<byte>(type: "smallint", nullable: false)
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
                    StaticBuffId = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    CharacterId = table.Column<long>(type: "bigint", nullable: false),
                    CardId = table.Column<short>(type: "smallint", nullable: false),
                    RemainingTime = table.Column<int>(type: "integer", nullable: false)
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
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    CharacterId = table.Column<long>(type: "bigint", nullable: false),
                    Active = table.Column<bool>(type: "boolean", nullable: false),
                    Visible = table.Column<bool>(type: "boolean", nullable: false),
                    TitleType = table.Column<short>(type: "smallint", nullable: false)
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
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    CharacterId = table.Column<long>(type: "bigint", nullable: true),
                    FamilyId = table.Column<long>(type: "bigint", nullable: true),
                    Type = table.Column<byte>(type: "smallint", nullable: false)
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
                name: "Drop",
                columns: table => new
                {
                    DropId = table.Column<short>(type: "smallint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Amount = table.Column<int>(type: "integer", nullable: false),
                    DropChance = table.Column<int>(type: "integer", nullable: false),
                    VNum = table.Column<short>(type: "smallint", nullable: false),
                    MapTypeId = table.Column<short>(type: "smallint", nullable: true),
                    MonsterVNum = table.Column<short>(type: "smallint", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Drop", x => x.DropId);
                    table.ForeignKey(
                        name: "FK_Drop_Item_VNum",
                        column: x => x.VNum,
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
                    MapTypeMapId = table.Column<short>(type: "smallint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    MapId = table.Column<short>(type: "smallint", nullable: false),
                    MapTypeId = table.Column<short>(type: "smallint", nullable: false)
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
                    RecipeItemId = table.Column<short>(type: "smallint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Amount = table.Column<short>(type: "smallint", nullable: false),
                    ItemVNum = table.Column<short>(type: "smallint", nullable: false),
                    RecipeId = table.Column<short>(type: "smallint", nullable: false)
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
                    ShopItemId = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Color = table.Column<byte>(type: "smallint", nullable: false),
                    ItemVNum = table.Column<short>(type: "smallint", nullable: false),
                    Rare = table.Column<short>(type: "smallint", nullable: false),
                    ShopId = table.Column<int>(type: "integer", nullable: false),
                    Slot = table.Column<byte>(type: "smallint", nullable: false),
                    Type = table.Column<byte>(type: "smallint", nullable: false),
                    Upgrade = table.Column<byte>(type: "smallint", nullable: false)
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
                    ShopSkillId = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    ShopId = table.Column<int>(type: "integer", nullable: false),
                    SkillVNum = table.Column<short>(type: "smallint", nullable: false),
                    Slot = table.Column<byte>(type: "smallint", nullable: false),
                    Type = table.Column<byte>(type: "smallint", nullable: false)
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
                name: "BazaarItem",
                columns: table => new
                {
                    BazaarItemId = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Amount = table.Column<short>(type: "smallint", nullable: false),
                    DateStart = table.Column<Instant>(type: "timestamp with time zone", nullable: false),
                    Duration = table.Column<short>(type: "smallint", nullable: false),
                    IsPackage = table.Column<bool>(type: "boolean", nullable: false),
                    ItemInstanceId = table.Column<Guid>(type: "uuid", nullable: false),
                    MedalUsed = table.Column<bool>(type: "boolean", nullable: false),
                    Price = table.Column<long>(type: "bigint", nullable: false),
                    SellerId = table.Column<long>(type: "bigint", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_BazaarItem", x => x.BazaarItemId);
                    table.ForeignKey(
                        name: "FK_BazaarItem_Character_SellerId",
                        column: x => x.SellerId,
                        principalTable: "Character",
                        principalColumn: "CharacterId",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_BazaarItem_ItemInstance_ItemInstanceId",
                        column: x => x.ItemInstanceId,
                        principalTable: "ItemInstance",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "EquipmentOption",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Level = table.Column<byte>(type: "smallint", nullable: false),
                    Type = table.Column<byte>(type: "smallint", nullable: false),
                    Value = table.Column<int>(type: "integer", nullable: false),
                    WearableInstanceId = table.Column<Guid>(type: "uuid", nullable: false)
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
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    CharacterId = table.Column<long>(type: "bigint", nullable: false),
                    ItemInstanceId = table.Column<Guid>(type: "uuid", nullable: false),
                    Slot = table.Column<short>(type: "smallint", nullable: false),
                    Type = table.Column<byte>(type: "smallint", nullable: false)
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
                    MailId = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Hat = table.Column<short>(type: "smallint", nullable: true),
                    Armor = table.Column<short>(type: "smallint", nullable: true),
                    MainWeapon = table.Column<short>(type: "smallint", nullable: true),
                    SecondaryWeapon = table.Column<short>(type: "smallint", nullable: true),
                    Mask = table.Column<short>(type: "smallint", nullable: true),
                    Fairy = table.Column<short>(type: "smallint", nullable: true),
                    CostumeSuit = table.Column<short>(type: "smallint", nullable: true),
                    CostumeHat = table.Column<short>(type: "smallint", nullable: true),
                    WeaponSkin = table.Column<short>(type: "smallint", nullable: true),
                    WingSkin = table.Column<short>(type: "smallint", nullable: true),
                    Date = table.Column<Instant>(type: "timestamp with time zone", nullable: false),
                    IsOpened = table.Column<bool>(type: "boolean", nullable: false),
                    IsSenderCopy = table.Column<bool>(type: "boolean", nullable: false),
                    ItemInstanceId = table.Column<Guid>(type: "uuid", nullable: true),
                    Message = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
                    ReceiverId = table.Column<long>(type: "bigint", nullable: false),
                    SenderId = table.Column<long>(type: "bigint", nullable: true),
                    SenderCharacterClass = table.Column<byte>(type: "smallint", nullable: true),
                    SenderGender = table.Column<byte>(type: "smallint", nullable: true),
                    SenderHairColor = table.Column<byte>(type: "smallint", nullable: true),
                    SenderHairStyle = table.Column<byte>(type: "smallint", nullable: true),
                    SenderMorphId = table.Column<short>(type: "smallint", nullable: true),
                    Title = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Mail", x => x.MailId);
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
                    table.ForeignKey(
                        name: "FK_Mail_ItemInstance_ItemInstanceId",
                        column: x => x.ItemInstanceId,
                        principalTable: "ItemInstance",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "WarehouseItem",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    WarehouseId = table.Column<Guid>(type: "uuid", nullable: false),
                    ItemInstanceId = table.Column<Guid>(type: "uuid", nullable: false),
                    Slot = table.Column<short>(type: "smallint", nullable: false)
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
                name: "MinilandObject",
                columns: table => new
                {
                    MinilandObjectId = table.Column<Guid>(type: "uuid", nullable: false),
                    InventoryItemInstanceId = table.Column<Guid>(type: "uuid", nullable: true),
                    Level1BoxAmount = table.Column<byte>(type: "smallint", nullable: false),
                    Level2BoxAmount = table.Column<byte>(type: "smallint", nullable: false),
                    Level3BoxAmount = table.Column<byte>(type: "smallint", nullable: false),
                    Level4BoxAmount = table.Column<byte>(type: "smallint", nullable: false),
                    Level5BoxAmount = table.Column<byte>(type: "smallint", nullable: false),
                    MapX = table.Column<short>(type: "smallint", nullable: false),
                    MapY = table.Column<short>(type: "smallint", nullable: false)
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
                name: "IX_Character_CurrentScriptId",
                table: "Character",
                column: "CurrentScriptId");

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
                name: "IX_InventoryItemInstance_CharacterId_Slot_Type",
                table: "InventoryItemInstance",
                columns: new[] { "CharacterId", "Slot", "Type" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_InventoryItemInstance_ItemInstanceId",
                table: "InventoryItemInstance",
                column: "ItemInstanceId");

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
                name: "IX_MapNpc_Dialog",
                table: "MapNpc",
                column: "Dialog");

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
                name: "IX_MapTypeMap_MapId_MapTypeId",
                table: "MapTypeMap",
                columns: new[] { "MapId", "MapTypeId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_MapTypeMap_MapTypeId",
                table: "MapTypeMap",
                column: "MapTypeId");

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
                name: "IX_Script_ScriptId_ScriptStepId",
                table: "Script",
                columns: new[] { "ScriptId", "ScriptStepId" },
                unique: true);

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
                name: "NpcTalk");

            migrationBuilder.DropTable(
                name: "Account");

            migrationBuilder.DropTable(
                name: "Map");

            migrationBuilder.DropTable(
                name: "Script");
        }
    }
}
