using System;
using Microsoft.EntityFrameworkCore.Migrations;
using NodaTime;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace NosCore.Database.Migrations
{
    public partial class battlepass : Migration
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
                .Annotation("Npgsql:Enum:frequency_type", "daily,weekly,seasonal")
                .Annotation("Npgsql:Enum:gender_type", "male,female")
                .Annotation("Npgsql:Enum:hair_color_type", "dark_purple,yellow,blue,purple,orange,brown,green,dark_grey,light_blue,pink_red,light_yellow,light_pink,light_green,light_grey,sky_blue,black,dark_orange,dark_orange_variant2,dark_orange_variant3,dark_orange_variant4,dark_orange_variant5,dark_orange_variant6,light_orange,light_light_orange,light_light_light_orange,light_light_light_light_orange,super_light_orange,dark_yellow,light_light_yellow,kaki_yellow,super_light_yellow,super_light_yellow2,super_light_yellow3,little_dark_yellow,yellow_variant,yellow_variant1,yellow_variant2,yellow_variant3,yellow_variant4,yellow_variant5,yellow_variant6,yellow_variant7,yellow_variant8,yellow_variant9,green_variant,green_variant1,dark_green_variant,green_more_dark_variant,green_variant2,green_variant3,green_variant4,green_variant5,green_variant6,green_variant7,green_variant8,green_variant9,green_variant10,green_variant11,green_variant12,green_variant13,green_variant14,green_variant15,green_variant16,green_variant17,green_variant18,green_variant19,green_variant20,light_blue_variant1,light_blue_variant2,light_blue_variant3,light_blue_variant4,light_blue_variant5,light_blue_variant6,light_blue_variant7,light_blue_variant8,light_blue_variant9,light_blue_variant10,light_blue_variant11,light_blue_variant12,light_blue_variant13,dark_black,light_blue_variant14,light_blue_variant15,light_blue_variant16,light_blue_variant17,blue_variant,blue_variant_dark,blue_variant_dark_dark,blue_variant_dark_dark2,flash_blue,flash_blue_dark,flash_blue_dark2,flash_blue_dark3,flash_blue_dark4,flash_blue_dark5,flash_blue_dark6,flash_blue_dark7,flash_blue_dark8,flash_blue_dark9,white,flash_blue_dark10,flash_blue1,flash_blue2,flash_blue3,flash_blue4,flash_blue5,flash_purple,flash_light_purple,flash_light_purple2,flash_light_purple3,flash_light_purple4,flash_light_purple5,light_purple,purple_variant1,purple_variant2,purple_variant3,purple_variant4,purple_variant5,purple_variant6,purple_variant7,purple_variant8,purple_variant9,purple_variant10,purple_variant11,purple_variant12,purple_variant13,purple_variant14,purple_variant15")
                .Annotation("Npgsql:Enum:hair_style_type", "hair_style_a,hair_style_b,hair_style_c,hair_style_d,no_hair")
                .Annotation("Npgsql:Enum:item_effect_type", "no_effect,teleport,apply_hair_die,speaker,marriage_proposal,undefined,sp_charger,dropped_sp_recharger,premium_sp_recharger,crafted_sp_recharger,specialist_medal,apply_skin_partner,change_gender,point_initialisation,sealed_tarot_card,tarot_card,red_amulet,blue_amulet,reinforcement_amulet,heroic,random_heroic,attack_amulet,defense_amulet,speed_booster,box_effect,vehicle,gold_nos_merchant_upgrade,silver_nos_merchant_upgrade,inventory_upgrade,pet_space_upgrade,pet_basket_upgrade,pet_backpack_upgrade,inventory_ticket_upgrade,buff_potions,marriage_separation")
                .Annotation("Npgsql:Enum:item_type", "weapon,armor,fashion,jewelery,specialist,box,shell,main,upgrade,production,map,special,potion,event,title,quest1,sell,food,snack,magical,part,teacher,ammo,quest2,house,garden,minigame,terrace,miniland_theme")
                .Annotation("Npgsql:Enum:mate_type", "partner,pet")
                .Annotation("Npgsql:Enum:miniland_state", "open,private,lock")
                .Annotation("Npgsql:Enum:mission_type", "commend_player,complete_raid,complete_daily_quest_from_npc,defeat_monster_in_level_range,complete_rbb,complete_ic,complete_aot,play_minigames,catch_fish,complete_ts_min_level30,defeat_cursed_monster,complete_aot_master,cook_meal,complete_minigames_raid,complete_caligor_raid,upgrade_sp_or_equipement,craft_items,kill_players_in_glacernon,reach_reputation,stay_logged,defeat_bosses_on_map,reach_level_sky_tower,log_in_days_in_row,spend_gold_in_npc_shop_or_upgrade,complete_normal_ts,complete_hidden_ts,complete_level_sky_tower,earn_points_in_combat_arena")
                .Annotation("Npgsql:Enum:monster_type", "unknown,partner,npc,well,portal,boss,elite,peapod,special,gem_space_time")
                .Annotation("Npgsql:Enum:noscore_pocket_type", "equipment,main,etc,miniland,specialist,costume,wear")
                .Annotation("Npgsql:Enum:penalty_type", "muted,banned,block_exp,block_f_exp,block_rep,warning")
                .Annotation("Npgsql:Enum:portal_type", "ts_normal,closed,open,miniland,ts_end,ts_end_closed,exit,exit_closed,raid,effect,blue_raid,dark_raid,time_space,shop_teleport,map_portal")
                .Annotation("Npgsql:Enum:quest_type", "hunt,special_collect,collect_in_raid,brings,capture_without_getting_the_monster,capture,times_space,product,number_of_kill,target_reput,ts_point,dialog1,collect_in_ts,required,wear,needed,collect,transmit_gold,go_to,collect_map_entity,use,dialog2,un_know,inspect,win_raid,flower_quest")
                .Annotation("Npgsql:Enum:region_type", "en,de,fr,it,pl,es,ru,cs,tr")
                .Annotation("Npgsql:Enum:scripted_instance_type", "time_space,raid,raid_act4")
                .Annotation("Npgsql:Enum:static_bonus_type", "bazaar_medal_gold,bazaar_medal_silver,back_pack,pet_basket,pet_back_pack,inventory_ticket_upgrade")
                .Annotation("Npgsql:Enum:teleporter_type", "teleporter,teleporter_on_map")
                .Annotation("Npgsql:Enum:warehouse_type", "warehouse,family_ware_house,pet_warehouse")
                .OldAnnotation("Npgsql:Enum:audit_log_type", "account_creation,character_creation,email_update")
                .OldAnnotation("Npgsql:Enum:authority_type", "user,moderator,game_master,administrator,root,closed,banned,unconfirmed")
                .OldAnnotation("Npgsql:Enum:character_class_type", "adventurer,swordsman,archer,mage,martial_artist")
                .OldAnnotation("Npgsql:Enum:character_relation_type", "friend,hidden_spouse,spouse,blocked")
                .OldAnnotation("Npgsql:Enum:character_state", "active,inactive")
                .OldAnnotation("Npgsql:Enum:element_type", "neutral,fire,water,light,dark")
                .OldAnnotation("Npgsql:Enum:equipment_type", "main_weapon,armor,hat,gloves,boots,secondary_weapon,necklace,ring,bracelet,mask,fairy,amulet,sp,costume_suit,costume_hat,weapon_skin,wing_skin")
                .OldAnnotation("Npgsql:Enum:family_authority", "head,assistant,manager,member")
                .OldAnnotation("Npgsql:Enum:family_authority_type", "none,put,all")
                .OldAnnotation("Npgsql:Enum:family_log_type", "daily_message,raid_won,rainbow_battle,family_xp,family_level_up,level_up,item_upgraded,right_changed,authority_changed,family_managed,user_managed,ware_house_added,ware_house_removed")
                .OldAnnotation("Npgsql:Enum:family_member_rank", "nothing,old_uncle,old_aunt,father,mother,uncle,aunt,brother,sister,spouse,brother2,sister2,old_son,old_daugter,middle_son,middle_daughter,young_son,young_daugter,old_little_son,old_little_daughter,little_son,little_daughter,middle_little_son,middle_little_daugter")
                .OldAnnotation("Npgsql:Enum:gender_type", "male,female")
                .OldAnnotation("Npgsql:Enum:hair_color_type", "dark_purple,yellow,blue,purple,orange,brown,green,dark_grey,light_blue,pink_red,light_yellow,light_pink,light_green,light_grey,sky_blue,black,dark_orange,dark_orange_variant2,dark_orange_variant3,dark_orange_variant4,dark_orange_variant5,dark_orange_variant6,light_orange,light_light_orange,light_light_light_orange,light_light_light_light_orange,super_light_orange,dark_yellow,light_light_yellow,kaki_yellow,super_light_yellow,super_light_yellow2,super_light_yellow3,little_dark_yellow,yellow_variant,yellow_variant1,yellow_variant2,yellow_variant3,yellow_variant4,yellow_variant5,yellow_variant6,yellow_variant7,yellow_variant8,yellow_variant9,green_variant,green_variant1,dark_green_variant,green_more_dark_variant,green_variant2,green_variant3,green_variant4,green_variant5,green_variant6,green_variant7,green_variant8,green_variant9,green_variant10,green_variant11,green_variant12,green_variant13,green_variant14,green_variant15,green_variant16,green_variant17,green_variant18,green_variant19,green_variant20,light_blue_variant1,light_blue_variant2,light_blue_variant3,light_blue_variant4,light_blue_variant5,light_blue_variant6,light_blue_variant7,light_blue_variant8,light_blue_variant9,light_blue_variant10,light_blue_variant11,light_blue_variant12,light_blue_variant13,dark_black,light_blue_variant14,light_blue_variant15,light_blue_variant16,light_blue_variant17,blue_variant,blue_variant_dark,blue_variant_dark_dark,blue_variant_dark_dark2,flash_blue,flash_blue_dark,flash_blue_dark2,flash_blue_dark3,flash_blue_dark4,flash_blue_dark5,flash_blue_dark6,flash_blue_dark7,flash_blue_dark8,flash_blue_dark9,white,flash_blue_dark10,flash_blue1,flash_blue2,flash_blue3,flash_blue4,flash_blue5,flash_purple,flash_light_purple,flash_light_purple2,flash_light_purple3,flash_light_purple4,flash_light_purple5,light_purple,purple_variant1,purple_variant2,purple_variant3,purple_variant4,purple_variant5,purple_variant6,purple_variant7,purple_variant8,purple_variant9,purple_variant10,purple_variant11,purple_variant12,purple_variant13,purple_variant14,purple_variant15")
                .OldAnnotation("Npgsql:Enum:hair_style_type", "hair_style_a,hair_style_b,hair_style_c,hair_style_d,no_hair")
                .OldAnnotation("Npgsql:Enum:item_effect_type", "no_effect,teleport,apply_hair_die,speaker,marriage_proposal,undefined,sp_charger,dropped_sp_recharger,premium_sp_recharger,crafted_sp_recharger,specialist_medal,apply_skin_partner,change_gender,point_initialisation,sealed_tarot_card,tarot_card,red_amulet,blue_amulet,reinforcement_amulet,heroic,random_heroic,attack_amulet,defense_amulet,speed_booster,box_effect,vehicle,gold_nos_merchant_upgrade,silver_nos_merchant_upgrade,inventory_upgrade,pet_space_upgrade,pet_basket_upgrade,pet_backpack_upgrade,inventory_ticket_upgrade,buff_potions,marriage_separation")
                .OldAnnotation("Npgsql:Enum:item_type", "weapon,armor,fashion,jewelery,specialist,box,shell,main,upgrade,production,map,special,potion,event,title,quest1,sell,food,snack,magical,part,teacher,ammo,quest2,house,garden,minigame,terrace,miniland_theme")
                .OldAnnotation("Npgsql:Enum:mate_type", "partner,pet")
                .OldAnnotation("Npgsql:Enum:miniland_state", "open,private,lock")
                .OldAnnotation("Npgsql:Enum:monster_type", "unknown,partner,npc,well,portal,boss,elite,peapod,special,gem_space_time")
                .OldAnnotation("Npgsql:Enum:noscore_pocket_type", "equipment,main,etc,miniland,specialist,costume,wear")
                .OldAnnotation("Npgsql:Enum:penalty_type", "muted,banned,block_exp,block_f_exp,block_rep,warning")
                .OldAnnotation("Npgsql:Enum:portal_type", "ts_normal,closed,open,miniland,ts_end,ts_end_closed,exit,exit_closed,raid,effect,blue_raid,dark_raid,time_space,shop_teleport,map_portal")
                .OldAnnotation("Npgsql:Enum:quest_type", "hunt,special_collect,collect_in_raid,brings,capture_without_getting_the_monster,capture,times_space,product,number_of_kill,target_reput,ts_point,dialog1,collect_in_ts,required,wear,needed,collect,transmit_gold,go_to,collect_map_entity,use,dialog2,un_know,inspect,win_raid,flower_quest")
                .OldAnnotation("Npgsql:Enum:region_type", "en,de,fr,it,pl,es,ru,cs,tr")
                .OldAnnotation("Npgsql:Enum:scripted_instance_type", "time_space,raid,raid_act4")
                .OldAnnotation("Npgsql:Enum:static_bonus_type", "bazaar_medal_gold,bazaar_medal_silver,back_pack,pet_basket,pet_back_pack,inventory_ticket_upgrade")
                .OldAnnotation("Npgsql:Enum:teleporter_type", "teleporter,teleporter_on_map")
                .OldAnnotation("Npgsql:Enum:warehouse_type", "warehouse,family_ware_house,pet_warehouse");

            migrationBuilder.AddColumn<int>(
                name: "BattlePassPoints",
                table: "Character",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<bool>(
                name: "IsBattelPassPremimum",
                table: "Character",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.CreateTable(
                name: "BattlepassBearing",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    MinimumBattlePassPoint = table.Column<int>(type: "integer", nullable: false),
                    MaximumBattlePassPoint = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_BattlepassBearing", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "BattlepassItem",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    ItemVNum = table.Column<short>(type: "smallint", nullable: false),
                    Amount = table.Column<short>(type: "smallint", nullable: false),
                    IsSuperReward = table.Column<bool>(type: "boolean", nullable: false),
                    IsPremium = table.Column<bool>(type: "boolean", nullable: false),
                    BearingId = table.Column<long>(type: "bigint", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_BattlepassItem", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "BattlepassQuest",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    MissionType = table.Column<byte>(type: "smallint", nullable: false),
                    FrequencyType = table.Column<byte>(type: "smallint", nullable: false),
                    Data = table.Column<short>(type: "smallint", nullable: false),
                    MinObjectiveValue = table.Column<long>(type: "bigint", nullable: false),
                    MaxObjectiveValue = table.Column<long>(type: "bigint", nullable: false),
                    RewardAmount = table.Column<short>(type: "smallint", nullable: false),
                    Start = table.Column<Instant>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_BattlepassQuest", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "CharacterBattlepass",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    CharacterId = table.Column<long>(type: "bigint", nullable: false),
                    Data = table.Column<long>(type: "bigint", nullable: false),
                    Data2 = table.Column<long>(type: "bigint", nullable: true),
                    Data3 = table.Column<bool>(type: "boolean", nullable: true),
                    IsItem = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CharacterBattlepass", x => x.Id);
                });
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "BattlepassBearing");

            migrationBuilder.DropTable(
                name: "BattlepassItem");

            migrationBuilder.DropTable(
                name: "BattlepassQuest");

            migrationBuilder.DropTable(
                name: "CharacterBattlepass");

            migrationBuilder.DropColumn(
                name: "BattlePassPoints",
                table: "Character");

            migrationBuilder.DropColumn(
                name: "IsBattelPassPremimum",
                table: "Character");

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
                .Annotation("Npgsql:Enum:warehouse_type", "warehouse,family_ware_house,pet_warehouse")
                .OldAnnotation("Npgsql:Enum:audit_log_type", "account_creation,character_creation,email_update")
                .OldAnnotation("Npgsql:Enum:authority_type", "user,moderator,game_master,administrator,root,closed,banned,unconfirmed")
                .OldAnnotation("Npgsql:Enum:character_class_type", "adventurer,swordsman,archer,mage,martial_artist")
                .OldAnnotation("Npgsql:Enum:character_relation_type", "friend,hidden_spouse,spouse,blocked")
                .OldAnnotation("Npgsql:Enum:character_state", "active,inactive")
                .OldAnnotation("Npgsql:Enum:element_type", "neutral,fire,water,light,dark")
                .OldAnnotation("Npgsql:Enum:equipment_type", "main_weapon,armor,hat,gloves,boots,secondary_weapon,necklace,ring,bracelet,mask,fairy,amulet,sp,costume_suit,costume_hat,weapon_skin,wing_skin")
                .OldAnnotation("Npgsql:Enum:family_authority", "head,assistant,manager,member")
                .OldAnnotation("Npgsql:Enum:family_authority_type", "none,put,all")
                .OldAnnotation("Npgsql:Enum:family_log_type", "daily_message,raid_won,rainbow_battle,family_xp,family_level_up,level_up,item_upgraded,right_changed,authority_changed,family_managed,user_managed,ware_house_added,ware_house_removed")
                .OldAnnotation("Npgsql:Enum:family_member_rank", "nothing,old_uncle,old_aunt,father,mother,uncle,aunt,brother,sister,spouse,brother2,sister2,old_son,old_daugter,middle_son,middle_daughter,young_son,young_daugter,old_little_son,old_little_daughter,little_son,little_daughter,middle_little_son,middle_little_daugter")
                .OldAnnotation("Npgsql:Enum:frequency_type", "daily,weekly,seasonal")
                .OldAnnotation("Npgsql:Enum:gender_type", "male,female")
                .OldAnnotation("Npgsql:Enum:hair_color_type", "dark_purple,yellow,blue,purple,orange,brown,green,dark_grey,light_blue,pink_red,light_yellow,light_pink,light_green,light_grey,sky_blue,black,dark_orange,dark_orange_variant2,dark_orange_variant3,dark_orange_variant4,dark_orange_variant5,dark_orange_variant6,light_orange,light_light_orange,light_light_light_orange,light_light_light_light_orange,super_light_orange,dark_yellow,light_light_yellow,kaki_yellow,super_light_yellow,super_light_yellow2,super_light_yellow3,little_dark_yellow,yellow_variant,yellow_variant1,yellow_variant2,yellow_variant3,yellow_variant4,yellow_variant5,yellow_variant6,yellow_variant7,yellow_variant8,yellow_variant9,green_variant,green_variant1,dark_green_variant,green_more_dark_variant,green_variant2,green_variant3,green_variant4,green_variant5,green_variant6,green_variant7,green_variant8,green_variant9,green_variant10,green_variant11,green_variant12,green_variant13,green_variant14,green_variant15,green_variant16,green_variant17,green_variant18,green_variant19,green_variant20,light_blue_variant1,light_blue_variant2,light_blue_variant3,light_blue_variant4,light_blue_variant5,light_blue_variant6,light_blue_variant7,light_blue_variant8,light_blue_variant9,light_blue_variant10,light_blue_variant11,light_blue_variant12,light_blue_variant13,dark_black,light_blue_variant14,light_blue_variant15,light_blue_variant16,light_blue_variant17,blue_variant,blue_variant_dark,blue_variant_dark_dark,blue_variant_dark_dark2,flash_blue,flash_blue_dark,flash_blue_dark2,flash_blue_dark3,flash_blue_dark4,flash_blue_dark5,flash_blue_dark6,flash_blue_dark7,flash_blue_dark8,flash_blue_dark9,white,flash_blue_dark10,flash_blue1,flash_blue2,flash_blue3,flash_blue4,flash_blue5,flash_purple,flash_light_purple,flash_light_purple2,flash_light_purple3,flash_light_purple4,flash_light_purple5,light_purple,purple_variant1,purple_variant2,purple_variant3,purple_variant4,purple_variant5,purple_variant6,purple_variant7,purple_variant8,purple_variant9,purple_variant10,purple_variant11,purple_variant12,purple_variant13,purple_variant14,purple_variant15")
                .OldAnnotation("Npgsql:Enum:hair_style_type", "hair_style_a,hair_style_b,hair_style_c,hair_style_d,no_hair")
                .OldAnnotation("Npgsql:Enum:item_effect_type", "no_effect,teleport,apply_hair_die,speaker,marriage_proposal,undefined,sp_charger,dropped_sp_recharger,premium_sp_recharger,crafted_sp_recharger,specialist_medal,apply_skin_partner,change_gender,point_initialisation,sealed_tarot_card,tarot_card,red_amulet,blue_amulet,reinforcement_amulet,heroic,random_heroic,attack_amulet,defense_amulet,speed_booster,box_effect,vehicle,gold_nos_merchant_upgrade,silver_nos_merchant_upgrade,inventory_upgrade,pet_space_upgrade,pet_basket_upgrade,pet_backpack_upgrade,inventory_ticket_upgrade,buff_potions,marriage_separation")
                .OldAnnotation("Npgsql:Enum:item_type", "weapon,armor,fashion,jewelery,specialist,box,shell,main,upgrade,production,map,special,potion,event,title,quest1,sell,food,snack,magical,part,teacher,ammo,quest2,house,garden,minigame,terrace,miniland_theme")
                .OldAnnotation("Npgsql:Enum:mate_type", "partner,pet")
                .OldAnnotation("Npgsql:Enum:miniland_state", "open,private,lock")
                .OldAnnotation("Npgsql:Enum:mission_type", "commend_player,complete_raid,complete_daily_quest_from_npc,defeat_monster_in_level_range,complete_rbb,complete_ic,complete_aot,play_minigames,catch_fish,complete_ts_min_level30,defeat_cursed_monster,complete_aot_master,cook_meal,complete_minigames_raid,complete_caligor_raid,upgrade_sp_or_equipement,craft_items,kill_players_in_glacernon,reach_reputation,stay_logged,defeat_bosses_on_map,reach_level_sky_tower,log_in_days_in_row,spend_gold_in_npc_shop_or_upgrade,complete_normal_ts,complete_hidden_ts,complete_level_sky_tower,earn_points_in_combat_arena")
                .OldAnnotation("Npgsql:Enum:monster_type", "unknown,partner,npc,well,portal,boss,elite,peapod,special,gem_space_time")
                .OldAnnotation("Npgsql:Enum:noscore_pocket_type", "equipment,main,etc,miniland,specialist,costume,wear")
                .OldAnnotation("Npgsql:Enum:penalty_type", "muted,banned,block_exp,block_f_exp,block_rep,warning")
                .OldAnnotation("Npgsql:Enum:portal_type", "ts_normal,closed,open,miniland,ts_end,ts_end_closed,exit,exit_closed,raid,effect,blue_raid,dark_raid,time_space,shop_teleport,map_portal")
                .OldAnnotation("Npgsql:Enum:quest_type", "hunt,special_collect,collect_in_raid,brings,capture_without_getting_the_monster,capture,times_space,product,number_of_kill,target_reput,ts_point,dialog1,collect_in_ts,required,wear,needed,collect,transmit_gold,go_to,collect_map_entity,use,dialog2,un_know,inspect,win_raid,flower_quest")
                .OldAnnotation("Npgsql:Enum:region_type", "en,de,fr,it,pl,es,ru,cs,tr")
                .OldAnnotation("Npgsql:Enum:scripted_instance_type", "time_space,raid,raid_act4")
                .OldAnnotation("Npgsql:Enum:static_bonus_type", "bazaar_medal_gold,bazaar_medal_silver,back_pack,pet_basket,pet_back_pack,inventory_ticket_upgrade")
                .OldAnnotation("Npgsql:Enum:teleporter_type", "teleporter,teleporter_on_map")
                .OldAnnotation("Npgsql:Enum:warehouse_type", "warehouse,family_ware_house,pet_warehouse");
        }
    }
}
