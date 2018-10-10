using System;
using System.Collections.Generic;
using System.Linq;
using log4net.Core;
using NosCore.GameObject.ComponentEntities.Interfaces;
using NosCore.GameObject.Networking;
using NosCore.Packets.ServerPackets;
using NosCore.Shared.Enumerations;
using NosCore.Shared.Enumerations.Character;

namespace NosCore.GameObject.ComponentEntities.Extensions
{
    public static class VisualEntityExtension
    {
        public static ServerGetPacket GenerateGet(this ICharacterEntity visualEntity, long itemId)
        {
            return new ServerGetPacket
            {
                VisualType = visualEntity.VisualType,
                VisualId = visualEntity.VisualId,
                ItemId = itemId,
            };
        }

        public static IconPacket GenerateIcon(this ICharacterEntity visualEntity, byte iconType, short iconParameter)
        {
            return new IconPacket
            {
                VisualType = visualEntity.VisualType,
                VisualId = visualEntity.VisualId,
                IconParameter = iconParameter,
                IconType = iconType
            };
        }

        public static OutPacket GenerateOut(this ICharacterEntity visualEntity)
        {
            return new OutPacket
            {
                VisualType = visualEntity.VisualType,
                VisualId = visualEntity.VisualId
            };
        }

        //in 9 {vnum} {id} {x} {y} {amount} {IsQuestRelative} 0 {owner}
        //in 3 {Effect} {IsSitting} {GroupId} {SecondName} 0 -1 0 0 0 0 0 0 0 0
        //in 2 {Effect} {IsSitting} {GroupId} {SecondName} 0 -1 0 0 0 0 0 0 0 0
        //in 1 {IsSitting} {GroupId} {HaveFairy} {FairyElement} 0 {FairyMorph} 0 {Morph} {EqRare} {FamilyId} {SecondName} {Reput} {Invisible} {MorphUpgrade} {faction} {MorphUpgrade2} {Level} {FamilyLevel} {ArenaWinner} {Compliment} {Size} {HeroLevel}
        //in 1 Carlosta - 754816 71 105 2 0 1 0 14 3 340.4855.4867.4864.4846.802.4150.4142 100 37 0 -1 4 3 0 0 0 7 86 86 2340 ~Luna~(Membre) -2 0 5 0 0 88 10 0 0 10 1

        //Character in packet
        public static InPacket GenerateIn(this ICharacterEntity visualEntity)
        {
            return new InPacket
            {
                VisualType = visualEntity.VisualType,
                Name = visualEntity.Name,
                VNum = visualEntity.VNum == 0 ? "-" : visualEntity.VNum.ToString(),
                VisualId = visualEntity.VisualId,
                PositionX = visualEntity.PositionX,
                PositionY = visualEntity.PositionY,
                Direction = visualEntity.Direction,
                InCharacterSubPacket = new InCharacterSubPacket
                {
                    Authority = visualEntity.Authority,
                    Gender = (byte)visualEntity.Gender,
                    HairStyle = (byte)visualEntity.HairStyle,
                    HairColor = (byte)visualEntity.HairColor,
                    Class = visualEntity.Class,
                    Equipment = new InEquipmentSubPacket
                    {
                        Armor = -1,
                        CostumeHat = -1,
                        CostumeSuit = -1,
                        Fairy = -1,
                        Hat = -1,
                        MainWeapon = -1,
                        Mask = -1,
                        SecondaryWeapon = -1,
                        WeaponSkin = -1
                    },
                    InAliveSubPacket = new InAliveSubPacket
                    {
                        HP = (int)(visualEntity.Hp / (float)visualEntity.MaxHp * 100),
                        MP = (int)(visualEntity.Mp / (float)visualEntity.MaxMp * 100)
                    },
                    IsSitting = visualEntity.IsSitting,
                    GroupId = visualEntity.GroupId,
                    Fairy = 0,
                    FairyElement = 0,
                    Unknown = 0,
                    Morph = 0,
                    WeaponUpgrade = 0,
                    WeaponRare = 0,
                    ArmorUpgrade = 0,
                    ArmorRare = 0,
                    FamilyId = -1,
                    FamilyName = "-",
                    ReputIco = (short)(visualEntity.DignityIcon == 1 ? visualEntity.ReputIcon : -visualEntity.DignityIcon),
                    Invisible = false,
                    MorphUpgrade = 0,
                    Faction = 0,
                    MorphUpgrade2 = 0,
                    Level = visualEntity.Level,
                    FamilyLevel = 0,
                    ArenaWinner = false,
                    Compliment = 0,
                    Size = 0,
                    HeroLevel = visualEntity.HeroLevel
                }
            };
        }

        //Pet Monster in packet
        public static InPacket GenerateIn(this INonPlayableEntity visualEntity)
        {
            return new InPacket
            {
                VisualType = visualEntity.VisualType,
                Name = visualEntity.Name,
                VisualId = visualEntity.VisualId,
                VNum = visualEntity.VNum == 0 ? string.Empty : visualEntity.VNum.ToString(),
                PositionX = visualEntity.PositionX,
                PositionY = visualEntity.PositionY,
                Direction = visualEntity.Direction,
                InNonPlayerSubPacket = new InNonPlayerSubPacket
                {
                    Dialog = 0,
                    InAliveSubPacket = new InAliveSubPacket
                    {
                        MP = (int)(visualEntity.Mp / (float)(visualEntity.NpcMonster?.MaxMP ?? 1) * 100),
                        HP = (int)(visualEntity.Hp / (float)(visualEntity.NpcMonster?.MaxHP ?? 1) * 100)
                    },
                    IsSitting = visualEntity.IsSitting
                }
            };
        }

        //TODO move to its own class when correctly defined
        //Item in packet
        public static InPacket GenerateIn(this ICountableEntity visualEntity)
        {
            return new InPacket
            {
                VisualType = visualEntity.VisualType,
                VisualId = visualEntity.VisualId,
                VNum = visualEntity.VNum == 0 ? string.Empty : visualEntity.VNum.ToString(),
                PositionX = visualEntity.PositionX,
                PositionY = visualEntity.PositionY,
                Direction = visualEntity.Direction,
                InItemSubPacket = new InItemSubPacket
                {
                    Amount = visualEntity.Amount,
                    IsQuestRelative = false,
                    Owner = 0
                }
            };
        }

        public static SpeakPacket GenerateSpk(this INamedEntity visualEntity, SpeakPacket packet)
        {
            return new SpeakPacket
            {
                VisualType = visualEntity.VisualType,
                VisualId = visualEntity.VisualId,
                SpeakType = packet.SpeakType,
                EntityName = visualEntity.Name,
                Message = packet.Message
            };
        }
    }
}