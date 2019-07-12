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

using System.Collections.Generic;
using System.Linq;
using NosCore.Core;
using NosCore.Core.Networking;
using NosCore.Data.Enumerations;
using NosCore.Data.Enumerations.Account;
using NosCore.Data.WebApi;
using NosCore.GameObject.ComponentEntities.Interfaces;
using ChickenAPI.Packets.Enumerations;
using ChickenAPI.Packets.ServerPackets.Visibility;
using ChickenAPI.Packets.ServerPackets.Inventory;
using ChickenAPI.Packets.ServerPackets.Exchanges;
using ChickenAPI.Packets.ServerPackets.Relations;
using ChickenAPI.Packets.ServerPackets.Entities;
using ChickenAPI.Packets.ServerPackets.UI;
using NosCore.Core.I18N;
using NosCore.Data;
using NosCore.Data.Enumerations.I18N;
using NosCore.Data.Enumerations.Interaction;
using ChickenAPI.Packets.Interfaces;
using NosCore.GameObject.HttpClients;

namespace NosCore.GameObject.ComponentEntities.Extensions
{
    public static class CharacterEntityExtension
    {
        public static GoldPacket GenerateGold(this ICharacterEntity characterEntity)
        {
            return new GoldPacket { Gold = characterEntity.Gold };
        }

        public static ServerExcListPacket GenerateServerExcListPacket(this ICharacterEntity aliveEntity, long? gold,
            long? bankGold, List<ServerExcListSubPacket> subPackets)
        {
            return new ServerExcListPacket
            {
                VisualType = aliveEntity.VisualType,
                VisualId = aliveEntity.VisualId,
                Gold = gold,
                SubPackets = subPackets,
                BankGold = bankGold
            };
        }

        public static ClPacket GenerateInvisible(this ICharacterEntity visualEntity)
        {
            return new ClPacket
            {
                VisualId = visualEntity.VisualId,
                Camouflage = visualEntity.Camouflage,
                Invisible = visualEntity.Invisible
            };
        }

        public static BlinitPacket GenerateBlinit(this ICharacterEntity visualEntity, IBlacklistHttpClient blacklistHttpClient)
        {
            var subpackets = new List<BlinitSubPacket>();
            var blackList = blacklistHttpClient.GetCharacterRelationStatus(visualEntity.VisualId);
            foreach (var relation in blackList)
            {
                if (relation.CharacterId == visualEntity.VisualId)
                {
                    continue;
                }

                subpackets.Add(new BlinitSubPacket
                {
                    RelatedCharacterId = relation.CharacterId,
                    CharacterName = relation.CharacterName
                });
            }

            return new BlinitPacket { SubPackets = subpackets };
        }

        public static FinitPacket GenerateFinit(this ICharacterEntity visualEntity, IFriendHttpClient friendHttpClient)
        {
            //same canal
            var servers = webApiAccess.Get<List<ChannelInfo>>(WebApiRoute.Channel)
                ?.Where(c => c.Type == ServerType.WorldServer).ToList();
            var accounts = new List<ConnectedAccount>();
            foreach (var server in servers ?? new List<ChannelInfo>())
            {
                accounts.AddRange(
                    webApiAccess.Get<List<ConnectedAccount>>(WebApiRoute.ConnectedAccount, server.WebApi));
            }

            var subpackets = new List<FinitSubPacket>();
            var friendlist = webApiAccess.Get<List<CharacterRelationStatus>>(WebApiRoute.Friend, visualEntity.VisualId) ?? new List<CharacterRelationStatus>();
            //TODO add spouselist
            //var spouseList = _webApiAccess.Get<List<CharacterRelationDto>>(WebApiRoute.Spouse, friendServer.WebApi, visualEntity.VisualId) ?? new List<CharacterRelationDto>();
            foreach (var relation in friendlist)
            {
                var account = accounts.Find(s =>
                    s.ConnectedCharacter != null && s.ConnectedCharacter.Id == relation.CharacterId);
                subpackets.Add(new FinitSubPacket
                {
                    CharacterId = relation.CharacterId,
                    RelationType = relation.RelationType,
                    IsOnline = account != null,
                    CharacterName = relation.CharacterName
                });
            }

            return new FinitPacket { SubPackets = subpackets };
        }

        public static void SendFinfo(this ICharacterEntity visualEntity, IFriendHttpClient friendHttpClient, IPacketHttpClient packetHttpClient, ISerializer packetSerializer, bool isConnected)
        {
            var friendlist = friendHttpClient.GetListFriends(visualEntity.VisualId);
            foreach (var friend in friendlist)
            {
                packetHttpClient.BroadcastPacket(new PostedPacket
                {
                    Packet = packetSerializer.Serialize(new[]
                   {
                            new FinfoPacket
                            {
                                FriendList = new List<FinfoSubPackets>
                                {
                                    new FinfoSubPackets
                                    {
                                        CharacterId = visualEntity.VisualId,
                                        IsConnected = isConnected
                                    }
                                }
                            }
                        }),
                    ReceiverType = ReceiverType.OnlySomeone,
                    SenderCharacter = new Data.WebApi.Character { Id = visualEntity.VisualId, Name = visualEntity.Name },
                    ReceiverCharacter = new Data.WebApi.Character
                    {
                        Id = friend.CharacterId,
                        Name = friend.CharacterName
                    }
                });
            }
        }

        public static ServerGetPacket GenerateGet(this ICharacterEntity visualEntity, long itemId)
        {
            return new ServerGetPacket
            {
                VisualType = visualEntity.VisualType,
                VisualId = visualEntity.VisualId,
                ItemId = itemId
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
        public static InPacket GenerateIn(this ICharacterEntity visualEntity, string prefix)
        {
            return new InPacket
            {
                VisualType = visualEntity.VisualType,
                Name = prefix + visualEntity.Name,
                VNum = visualEntity.VNum == 0 ? null : visualEntity.VNum.ToString(),
                VisualId = visualEntity.VisualId,
                PositionX = visualEntity.PositionX,
                PositionY = visualEntity.PositionY,
                Direction = visualEntity.Direction,
                InCharacterSubPacket = new InCharacterSubPacket
                {
                    Authority = (byte)visualEntity.Authority,//todo change chickenapi to short
                    Gender = visualEntity.Gender,
                    HairStyle = visualEntity.HairStyle,
                    HairColor = visualEntity.HairColor,
                    Class = visualEntity.Class,
                    Equipment = visualEntity.Equipment,
                    InAliveSubPacket = new InAliveSubPacket
                    {
                        Hp = (int)(visualEntity.Hp / (float)visualEntity.MaxHp * 100),
                        Mp = (int)(visualEntity.Mp / (float)visualEntity.MaxMp * 100)
                    },
                    IsSitting = visualEntity.IsSitting,
                    GroupId = visualEntity.Group.GroupId,
                    Fairy = 0,
                    FairyElement = 0,
                    Unknown = 0,
                    Morph = 0,
                    Unknown2 = 0,
                    Unknown3 = 0,
                    WeaponUpgradeRareSubPacket = visualEntity.WeaponUpgradeRareSubPacket,
                    ArmorUpgradeRareSubPacket = visualEntity.ArmorUpgradeRareSubPacket,
                    FamilyId = -1,
                    FamilyName = null,
                    ReputIco = (short)(visualEntity.DignityIcon == 1 ? visualEntity.ReputIcon
                        : -visualEntity.DignityIcon),
                    Invisible = false,
                    MorphUpgrade = 0,
                    Faction = 0,
                    MorphUpgrade2 = 0,
                    Level = visualEntity.Level,
                    FamilyLevel = 0,
                    FamilyIcons = new List<bool> { false, false, false },
                    ArenaWinner = false,
                    Compliment = (short)(visualEntity.Authority == AuthorityType.Moderator ? 500 : 0),
                    Size = visualEntity.Size,
                    HeroLevel = visualEntity.HeroLevel
                }
            };
        }
    }
}