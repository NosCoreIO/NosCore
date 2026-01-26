//  __  _  __    __   ___ __  ___ ___
// |  \| |/__\ /' _/ / _//__\| _ \ __|
// | | ' | \/ |`._`.| \_| \/ | v / _|
// |_|\__|\__/ |___/ \__/\__/|_|_\___|
//

using NosCore.Algorithm.ExperienceService;
using NosCore.Algorithm.HeroExperienceService;
using NosCore.Algorithm.JobExperienceService;
using NosCore.Packets.Enumerations;
using NosCore.Packets.ServerPackets.Inventory;
using NosCore.Packets.ServerPackets.MiniMap;
using NosCore.Packets.ServerPackets.Player;
using NosCore.Packets.ServerPackets.Entities;
using NosCore.Packets.ServerPackets.Relations;
using NosCore.Packets.ServerPackets.Specialists;
using NosCore.Packets.ServerPackets.Visibility;
using NosCore.Shared.Enumerations;

namespace NosCore.GameObject.Ecs.Extensions;

public static class PlayerBundleExtensions
{
    public static InPacket GenerateIn(this PlayerComponentBundle player, string prefix)
    {
        return new InPacket
        {
            VisualType = VisualType.Player,
            Name = prefix + player.Name,
            VisualId = player.VisualId,
            PositionX = player.PositionX,
            PositionY = player.PositionY,
            Direction = player.Direction,
            InCharacterSubPacket = new InCharacterSubPacket
            {
                Authority = player.Authority,
                Gender = player.Gender,
                HairStyle = player.HairStyle,
                HairColor = player.HairColor,
                Class = player.Class,
                Equipment = new InEquipmentSubPacket
                {
                    Hat = null,
                    Armor = null,
                    MainWeapon = null,
                    SecondaryWeapon = null,
                    Mask = null,
                    Fairy = null,
                    CostumeSuit = null,
                    CostumeHat = null,
                    WeaponSkin = null,
                    WingSkin = null
                },
                InAliveSubPacket = new InAliveSubPacket
                {
                    Hp = player.MaxHp > 0 ? (int)(player.Hp / (float)player.MaxHp * 100) : 100,
                    Mp = player.MaxMp > 0 ? (int)(player.Mp / (float)player.MaxMp * 100) : 100
                },
                IsSitting = player.IsSitting,
                GroupId = -1,
                Fairy = 0,
                FairyElement = 0,
                Unknown = 0,
                Morph = 0,
                Unknown2 = 0,
                Unknown3 = 0,
                WeaponUpgradeRareSubPacket = new UpgradeRareSubPacket { Upgrade = 0, Rare = 0 },
                ArmorUpgradeRareSubPacket = new UpgradeRareSubPacket { Upgrade = 0, Rare = 0 },
                FamilySubPacket = new FamilySubPacket(),
                FamilyName = null,
                ReputIco = (byte)GetReputationIcon(player.Reputation),
                Invisible = false,
                MorphUpgrade = player.MorphUpgrade,
                Faction = 0,
                MorphUpgrade2 = player.MorphDesign,
                Level = player.Level,
                FamilyLevel = 0,
                FamilyIcons = new System.Collections.Generic.List<bool> { false, false, false },
                ArenaWinner = false,
                Compliment = (short)player.Compliment,
                Size = player.Size,
                HeroLevel = player.HeroLevel
            }
        };
    }

    public static StatPacket GenerateStat(this PlayerComponentBundle player)
    {
        return new StatPacket
        {
            Hp = player.Hp,
            HpMaximum = player.MaxHp,
            Mp = player.Mp,
            MpMaximum = player.MaxMp,
            Unknown = 0,
            Option = 0
        };
    }

    public static GoldPacket GenerateGold(this PlayerComponentBundle player)
    {
        return new GoldPacket { Gold = player.Gold };
    }

    public static LevPacket GenerateLev(this PlayerComponentBundle player,
        IExperienceService experienceService,
        IJobExperienceService jobExperienceService,
        IHeroExperienceService heroExperienceService)
    {
        return new LevPacket
        {
            Level = player.Level,
            LevelXp = player.LevelXp,
            JobLevel = player.JobLevel,
            JobLevelXp = player.JobLevelXp,
            XpLoad = experienceService.GetExperience(player.Level),
            JobXpLoad = jobExperienceService.GetJobExperience(player.Class, player.JobLevel),
            Reputation = player.Reputation,
            SkillCp = 0,
            HeroXp = player.HeroLevelXp,
            HeroLevel = player.HeroLevel,
            HeroXpLoad = player.HeroLevel == 0 ? 0 : heroExperienceService.GetHeroExperience(player.HeroLevel)
        };
    }

    public static FdPacket GenerateFd(this PlayerComponentBundle player)
    {
        return new FdPacket
        {
            Reput = player.Reputation,
            ReputIcon = (int)GetReputationIcon(player.Reputation),
            Dignity = player.Dignity,
            DignityIcon = (int)GetDignityIcon(player.Dignity)
        };
    }

    public static AtPacket GenerateAt(this PlayerComponentBundle player, short mapId, int music = 0)
    {
        return new AtPacket
        {
            CharacterId = player.VisualId,
            MapId = mapId,
            PositionX = player.PositionX,
            PositionY = player.PositionY,
            Direction = player.Direction,
            Unknown1 = 0,
            Music = music,
            Unknown2 = 0,
            Unknown3 = -1
        };
    }

    public static CInfoPacket GenerateCInfo(this PlayerComponentBundle player)
    {
        return new CInfoPacket
        {
            Name = player.Name,
            Unknown1 = null,
            GroupId = -1,
            FamilyId = -1,
            FamilyName = null,
            CharacterId = player.VisualId,
            Authority = player.Authority,
            Gender = player.Gender,
            HairStyle = player.HairStyle,
            HairColor = player.HairColor,
            Class = player.Class,
            Icon = (byte)GetReputationIcon(player.Reputation),
            Compliment = (short)player.Compliment,
            Morph = player.Morph,
            Invisible = false,
            FamilyLevel = 0,
            MorphUpgrade = player.MorphUpgrade,
            ArenaWinner = false
        };
    }

    public static CondPacket GenerateCond(this PlayerComponentBundle player)
    {
        return new CondPacket
        {
            VisualType = VisualType.Player,
            VisualId = player.VisualId,
            NoAttack = player.NoAttack,
            NoMove = player.NoMove,
            Speed = player.Speed
        };
    }

    public static CModePacket GenerateCMode(this PlayerComponentBundle player)
    {
        return new CModePacket
        {
            VisualType = VisualType.Player,
            VisualId = player.VisualId,
            Morph = player.Morph,
            MorphUpgrade = player.MorphUpgrade,
            MorphDesign = player.MorphDesign,
            MorphBonus = player.MorphBonus,
            Size = player.Size
        };
    }

    public static OutPacket GenerateOut(this PlayerComponentBundle player)
    {
        return new OutPacket
        {
            VisualType = VisualType.Player,
            VisualId = player.VisualId
        };
    }

    private static short GetReputationIcon(long reputation)
    {
        return reputation switch
        {
            >= 5000001 => 28,
            >= 2500001 => 27,
            >= 500001 => 26,
            >= 250001 => 25,
            >= 100001 => 24,
            >= 50001 => 23,
            >= 10001 => 22,
            >= 5001 => 21,
            >= 2501 => 20,
            >= 501 => 19,
            >= 251 => 18,
            >= 1 => 17,
            _ => 16
        };
    }

    private static short GetDignityIcon(int dignity)
    {
        return dignity switch
        {
            < -1000 => 1,
            <= -800 => 2,
            <= -600 => 3,
            <= -400 => 4,
            <= -200 => 5,
            <= -100 => 6,
            _ => 7
        };
    }
}
