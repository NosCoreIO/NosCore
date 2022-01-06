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

using Microsoft.Extensions.Options;
using NosCore.Core;
using NosCore.Core.Configuration;
using NosCore.Core.HttpClients.ChannelHttpClients;
using NosCore.Core.HttpClients.ConnectedAccountHttpClients;
using NosCore.Data.Enumerations;
using NosCore.Data.Enumerations.I18N;
using NosCore.Data.Enumerations.Interaction;
using NosCore.Data.StaticEntities;
using NosCore.Data.WebApi;
using NosCore.GameObject.ComponentEntities.Interfaces;
using NosCore.GameObject.HttpClients.BlacklistHttpClient;
using NosCore.GameObject.HttpClients.FriendHttpClient;
using NosCore.GameObject.HttpClients.PacketHttpClient;
using NosCore.Packets.ClientPackets.Player;
using NosCore.Packets.Enumerations;
using NosCore.Packets.Interfaces;
using NosCore.Packets.ServerPackets.Chats;
using NosCore.Packets.ServerPackets.Entities;
using NosCore.Packets.ServerPackets.Exchanges;
using NosCore.Packets.ServerPackets.Inventory;
using NosCore.Packets.ServerPackets.Miniland;
using NosCore.Packets.ServerPackets.MiniMap;
using NosCore.Packets.ServerPackets.Player;
using NosCore.Packets.ServerPackets.Quest;
using NosCore.Packets.ServerPackets.Relations;
using NosCore.Packets.ServerPackets.UI;
using NosCore.Packets.ServerPackets.Visibility;
using NosCore.Shared.Enumerations;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace NosCore.GameObject.ComponentEntities.Extensions
{
    public static class CharacterEntityExtension
    {
        public static BpmPacket GenerateBpmPacket(this ICharacterEntity characterEntity, List<QuestDto> quests, IOptions<WorldConfiguration> conf)
        {
            List<BpmSubTypePacket> subPackets = new();
            foreach (var quest in quests)
            {
                var characterAdvencement = characterEntity.Quests.Values.FirstOrDefault(s => s.CharacterId == characterEntity.VisualId && s.QuestId == quest.QuestId);
                long actualAdvencement = characterAdvencement?.Quest.QuestObjectives.FirstOrDefault(s => s.QuestId == quest.QuestId)?.FirstData ?? 0;
                subPackets.Add(new()
                {
                    //QuestId = quest.QuestId,
                    //MissionType = quest.QuestType,
                    //FrequencyType = quest.FrequencyType,
                    //Advencement = actualAdvencement,
                    //MaxObjectiveValue = quest.,
                    //Reward = (byte)quest.R,
                    //MissionMinutesRemaining = 1 // TODO
                });
            }

            return new BpmPacket
            {
                IsBattlePassIconEnabled = conf.Value.IsBattlePassIconEnabled,
                MaxBattlePassPoints = conf.Value.MaxBattlePassPoints,
                QuestList = subPackets
            };
        }

        public static GoldPacket GenerateGold(this ICharacterEntity characterEntity)
        {
            return new GoldPacket { Gold = characterEntity.Gold };
        }

        public static RsfiPacket GenerateRsfi(this ICharacterEntity _)
        {
            return new RsfiPacket
            {
                Act = 1,
                ActPart = 1,
                Unknown1 = 0,
                Unknown2 = 9,
                Ts = 0,
                TsMax = 9
            };
        }

        public static MlobjlstPacket GenerateMlobjlst(this ICharacterEntity characterEntity)
        {
            var mlobj = new List<MlobjlstSubPacket?>();
            foreach (var item in characterEntity.InventoryService.Where(s => s.Value.Type == NoscorePocketType.Miniland)
                .OrderBy(s => s.Value.Slot).Select(s => s.Value))
            {
                var used = characterEntity.MapInstance.MapDesignObjects.ContainsKey(item.Id);
                var mp = used ? characterEntity.MapInstance.MapDesignObjects[item.Id] : null;

                mlobj.Add(new MlobjlstSubPacket
                {
                    InUse = used,
                    Slot = item.Slot,
                    MlObjSubPacket = new MlobjSubPacket
                    {
                        MapX = used ? mp!.MapX : (short)0,
                        MapY = used ? mp!.MapY : (short)0,
                        Width = item.ItemInstance!.Item!.Width != 0 ? item.ItemInstance.Item.Width : (byte)1,
                        Height = item.ItemInstance.Item.Height != 0 ? item.ItemInstance.Item.Height : (byte)1,
                        DurabilityPoint = used ? item.ItemInstance.DurabilityPoint : 0,
                        Unknown = 100,
                        Unknown2 = false,
                        IsWarehouse = item.ItemInstance.Item.IsWarehouse
                    }
                });
            }

            return new MlobjlstPacket
            {
                MlobjlstSubPacket = mlobj
            };
        }

        public static ServerExcListPacket GenerateServerExcListPacket(this ICharacterEntity aliveEntity, long? gold,
            long? bankGold, List<ServerExcListSubPacket?>? subPackets)
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

        public static TitleInfoPacket GenerateTitInfo(this ICharacterEntity visualEntity)
        {
            var visibleTitle = visualEntity.Titles.FirstOrDefault(s => s.Visible)?.TitleType;
            var effectiveTitle = visualEntity.Titles.FirstOrDefault(s => s.Active)?.TitleType;
            return new TitleInfoPacket
            {
                VisualId = visualEntity.VisualId,
                EffectiveTitle = effectiveTitle ?? 0,
                VisualType = visualEntity.VisualType,
                VisibleTitle = visibleTitle ?? 0,
            };
        }

        public static TitlePacket GenerateTitle(this ICharacterEntity visualEntity)
        {
            var data = visualEntity.Titles.Select(s => new TitleSubPacket
            {
                TitleId = (short)(s.TitleType - 9300),
                TitleStatus = (byte)((s.Visible ? 2 : 0) + (s.Active ? 4 : 0) + 1)
            }).ToList() as List<TitleSubPacket?>;
            return new TitlePacket
            {
                Data = data.Any() ? data : null
            };
        }

        public static ExtsPacket GenerateExts(this ICharacterEntity visualEntity, IOptions<WorldConfiguration> conf)
        {
            return new ExtsPacket
            {
                EquipmentExtension = (byte)(visualEntity.InventoryService.Expensions[NoscorePocketType.Equipment] + conf.Value.BackpackSize),
                MainExtension = (byte)(visualEntity.InventoryService.Expensions[NoscorePocketType.Main] + conf.Value.BackpackSize),
                EtcExtension = (byte)(visualEntity.InventoryService.Expensions[NoscorePocketType.Etc] + conf.Value.BackpackSize)
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

        public static async Task<BlinitPacket> GenerateBlinitAsync(this ICharacterEntity visualEntity,
            IBlacklistHttpClient blacklistHttpClient)
        {
            var subpackets = new List<BlinitSubPacket?>();
            var blackList = await blacklistHttpClient.GetBlackListsAsync(visualEntity.VisualId).ConfigureAwait(false);
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

        public static async Task<FinitPacket> GenerateFinitAsync(this ICharacterEntity visualEntity, IFriendHttpClient friendHttpClient,
            IChannelHttpClient channelHttpClient, IConnectedAccountHttpClient connectedAccountHttpClient)
        {
            //same canal
            var servers = (await channelHttpClient.GetChannelsAsync().ConfigureAwait(false))
                ?.Where(c => c.Type == ServerType.WorldServer).ToList();
            var accounts = new List<ConnectedAccount>();
            foreach (var server in servers ?? new List<ChannelInfo>())
            {
                accounts.AddRange(
                    await connectedAccountHttpClient.GetConnectedAccountAsync(server).ConfigureAwait(false));
            }

            var subpackets = new List<FinitSubPacket?>();
            var friendlist = await friendHttpClient.GetListFriendsAsync(visualEntity.VisualId).ConfigureAwait(false);
            //TODO add spouselist
            //var spouseList = _webApiAccess.Get<List<CharacterRelationDto>>(WebApiRoute.Spouse, friendServer.WebApi, visualEntity.VisualId) ?? new List<CharacterRelationDto>();
            foreach (var relation in friendlist)
            {
                var account = accounts.Find(s =>
                    (s.ConnectedCharacter != null) && (s.ConnectedCharacter.Id == relation.CharacterId));
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

        public static async Task SendFinfoAsync(this ICharacterEntity visualEntity, IFriendHttpClient friendHttpClient,
            IPacketHttpClient packetHttpClient, ISerializer packetSerializer, bool isConnected)
        {
            var friendlist = await friendHttpClient.GetListFriendsAsync(visualEntity.VisualId).ConfigureAwait(false);
            await Task.WhenAll(friendlist.Select(friend =>
                packetHttpClient.BroadcastPacketAsync(new PostedPacket
                {
                    Packet = packetSerializer.Serialize(new[]
                    {
                       new FinfoPacket
                       {
                           FriendList = new List<FinfoSubPackets?>
                           {
                               new()
                               {
                                   CharacterId = visualEntity.VisualId,
                                   IsConnected = isConnected
                               }
                           }
                       }
                    }),
                    ReceiverType = ReceiverType.OnlySomeone,
                    SenderCharacter = new Data.WebApi.Character { Id = visualEntity.VisualId, Name = visualEntity.Name! },
                    ReceiverCharacter = new Data.WebApi.Character
                    {
                        Id = friend.CharacterId,
                        Name = friend.CharacterName!
                    }
                }))).ConfigureAwait(false);
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

        public static QstlistPacket GenerateQuestPacket(this ICharacterEntity visualEntity)
        {
            return new QstlistPacket()
            {
                QuestSubPackets = visualEntity.Quests.Values.Where(s => s.CompletedOn == null).Select(quest => quest.GenerateQuestSubPacket(true)).ToList()
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
                    Authority = visualEntity.Authority >= AuthorityType.Administrator ? AuthorityType.Administrator : AuthorityType.User,
                    Gender = visualEntity.Gender,
                    HairStyle = visualEntity.HairStyle,
                    HairColor = visualEntity.HairColor,
                    Class = visualEntity.Class,
                    Equipment = visualEntity.GetEquipmentSubPacket(),
                    InAliveSubPacket = new InAliveSubPacket
                    {
                        Hp = (int)(visualEntity.Hp / (float)visualEntity.MaxHp * 100),
                        Mp = (int)(visualEntity.Mp / (float)visualEntity.MaxMp * 100)
                    },
                    IsSitting = visualEntity.IsSitting,
                    GroupId = visualEntity.Group!.GroupId,
                    Fairy = 0,
                    FairyElement = 0,
                    Unknown = 0,
                    Morph = 0,
                    Unknown2 = 0,
                    Unknown3 = 0,
                    WeaponUpgradeRareSubPacket = visualEntity.GetWeaponUpgradeRareSubPacket(),
                    ArmorUpgradeRareSubPacket = visualEntity.GetArmorUpgradeRareSubPacket(),
                    FamilySubPacket = new FamilySubPacket(),
                    FamilyName = null,
                    ReputIco = (byte)(visualEntity.DignityIcon == DignityType.Default ? (byte)visualEntity.ReputIcon
                        : -(byte)visualEntity.DignityIcon),//TODO replace type by a byte
                    Invisible = false,
                    MorphUpgrade = 0,
                    Faction = 0,
                    MorphUpgrade2 = 0,
                    Level = visualEntity.Level,
                    FamilyLevel = 0,
                    FamilyIcons = new List<bool> { false, false, false },
                    ArenaWinner = false,
                    Compliment = (short)(visualEntity.Authority >= AuthorityType.Moderator ? 500 : visualEntity.Compliment),
                    Size = visualEntity.Size,
                    HeroLevel = visualEntity.HeroLevel
                }
            };
        }

        public static InEquipmentSubPacket GetEquipmentSubPacket(this ICharacterEntity visualEntity) => new()
        {
            Armor = visualEntity.InventoryService.LoadBySlotAndType((short)EquipmentType.Armor, NoscorePocketType.Wear)?.ItemInstance?
                .ItemVNum,
            CostumeHat = visualEntity.InventoryService.LoadBySlotAndType((short)EquipmentType.CostumeHat, NoscorePocketType.Wear)
                ?.ItemInstance?.ItemVNum,
            CostumeSuit = visualEntity.InventoryService.LoadBySlotAndType((short)EquipmentType.CostumeSuit, NoscorePocketType.Wear)
                ?.ItemInstance?.ItemVNum,
            Fairy = visualEntity.InventoryService.LoadBySlotAndType((short)EquipmentType.Fairy, NoscorePocketType.Wear)?.ItemInstance?
                .ItemVNum,
            Hat = visualEntity.InventoryService.LoadBySlotAndType((short)EquipmentType.Hat, NoscorePocketType.Wear)?.ItemInstance?.ItemVNum,
            MainWeapon = visualEntity.InventoryService.LoadBySlotAndType((short)EquipmentType.MainWeapon, NoscorePocketType.Wear)
                ?.ItemInstance?.ItemVNum,
            Mask = visualEntity.InventoryService.LoadBySlotAndType((short)EquipmentType.Mask, NoscorePocketType.Wear)?.ItemInstance?
                .ItemVNum,
            SecondaryWeapon = visualEntity.InventoryService
                .LoadBySlotAndType((short)EquipmentType.SecondaryWeapon, NoscorePocketType.Wear)?.ItemInstance?
                .ItemVNum,
            WeaponSkin = visualEntity.InventoryService.LoadBySlotAndType((short)EquipmentType.WeaponSkin, NoscorePocketType.Wear)
                ?.ItemInstance?.ItemVNum,
            WingSkin = visualEntity.InventoryService.LoadBySlotAndType((short)EquipmentType.WingSkin, NoscorePocketType.Wear)
                ?.ItemInstance?.ItemVNum
        };

        public static UpgradeRareSubPacket GetWeaponUpgradeRareSubPacket(this ICharacterEntity visualEntity)
        {
            var weapon =
                visualEntity.InventoryService.LoadBySlotAndType((short)EquipmentType.MainWeapon, NoscorePocketType.Wear);
            return new UpgradeRareSubPacket
            {
                Upgrade = weapon?.ItemInstance?.Upgrade ?? 0,
                Rare = (sbyte)(weapon?.ItemInstance?.Rare ?? 0)
            };
        }

        public static UpgradeRareSubPacket GetArmorUpgradeRareSubPacket(this ICharacterEntity visualEntity)
        {
            var armor = visualEntity.InventoryService.LoadBySlotAndType((short)EquipmentType.Armor, NoscorePocketType.Wear);
            return new UpgradeRareSubPacket
            {
                Upgrade = armor?.ItemInstance?.Upgrade ?? 0,
                Rare = (sbyte)(armor?.ItemInstance?.Rare ?? 0)
            };

        }
        public static FdPacket GenerateFd(this ICharacterEntity visualEntity)
        {
            return new FdPacket
            {
                Reput = visualEntity.Reput,
                Dignity = visualEntity.Dignity,
                ReputIcon = (int)visualEntity.ReputIcon, //todo change packet type
                DignityIcon = (int)visualEntity.DignityIcon //todo change packet type
            };
        }

        public static AtPacket GenerateAt(this ICharacterEntity visualEntity)
        {
            return new AtPacket
            {
                CharacterId = visualEntity.VisualId,
                MapId = visualEntity.MapInstance.Map.MapId,
                PositionX = visualEntity.PositionX,
                PositionY = visualEntity.PositionY,
                Direction = visualEntity.Direction,
                Unknown1 = 0,
                Music = visualEntity.MapInstance.Map.Music,
                Unknown2 = 0,
                Unknown3 = -1
            };
        }

        public static TitPacket GenerateTit(this ICharacterEntity visualEntity)
        {
            return new TitPacket
            {
                ClassType = (Game18NConstString)Enum.Parse(typeof(Game18NConstString), visualEntity.Class.ToString()),
                Name = visualEntity.Name
            };
        }

        public static TalkPacket GenerateTalk(this ICharacterEntity visualEntity, string message)
        {
            return new TalkPacket
            {
                CharacterId = visualEntity.VisualId,
                Message = message
            };
        }


        public static EquipPacket? GenerateEquipment(this ICharacterEntity visualEntity)
        {
            EquipmentSubPacket? GenerateEquipmentSubPacket(EquipmentType eqType)
            {
                var eq = visualEntity.InventoryService.LoadBySlotAndType((short)eqType, NoscorePocketType.Wear);
                if (eq == null)
                {
                    return null;
                }

                return new EquipmentSubPacket
                {
                    EquipmentType = eqType,
                    VNum = eq.ItemInstance!.ItemVNum,
                    Rare = eq.ItemInstance.Rare,
                    Upgrade = (eq.ItemInstance!.Item!.IsColored ? eq.ItemInstance?.Design
                        : eq.ItemInstance.Upgrade) ?? 0,
                    Unknown = 0
                };
            }

            return new EquipPacket
            {
                WeaponUpgradeRareSubPacket = visualEntity.GetWeaponUpgradeRareSubPacket(),
                ArmorUpgradeRareSubPacket = visualEntity.GetArmorUpgradeRareSubPacket(),
                Armor = GenerateEquipmentSubPacket(EquipmentType.Armor),
                WeaponSkin = GenerateEquipmentSubPacket(EquipmentType.WeaponSkin),
                SecondaryWeapon = GenerateEquipmentSubPacket(EquipmentType.SecondaryWeapon),
                Sp = GenerateEquipmentSubPacket(EquipmentType.Sp),
                Amulet = GenerateEquipmentSubPacket(EquipmentType.Amulet),
                Boots = GenerateEquipmentSubPacket(EquipmentType.Boots),
                CostumeHat = GenerateEquipmentSubPacket(EquipmentType.CostumeHat),
                CostumeSuit = GenerateEquipmentSubPacket(EquipmentType.CostumeSuit),
                Fairy = GenerateEquipmentSubPacket(EquipmentType.Fairy),
                Gloves = GenerateEquipmentSubPacket(EquipmentType.Gloves),
                Hat = GenerateEquipmentSubPacket(EquipmentType.Hat),
                MainWeapon = GenerateEquipmentSubPacket(EquipmentType.MainWeapon),
                Mask = GenerateEquipmentSubPacket(EquipmentType.Mask),
                Necklace = GenerateEquipmentSubPacket(EquipmentType.Necklace),
                Ring = GenerateEquipmentSubPacket(EquipmentType.Ring),
                Bracelet = GenerateEquipmentSubPacket(EquipmentType.Bracelet),
                WingSkin = GenerateEquipmentSubPacket(EquipmentType.WingSkin)
            };
        }

        public static EqPacket GenerateEq(this ICharacterEntity visualEntity)
        {
            return new EqPacket
            {
                VisualId = visualEntity.VisualId,
                Visibility = (byte)(visualEntity.Authority < AuthorityType.GameMaster ? 0 : 2),
                Gender = visualEntity.Gender,
                HairStyle = visualEntity.HairStyle,
                Haircolor = visualEntity.HairColor,
                ClassType = visualEntity.Class,
                EqSubPacket = visualEntity.GetEquipmentSubPacket(),
                WeaponUpgradeRarePacket = visualEntity.GetWeaponUpgradeRareSubPacket(),
                ArmorUpgradeRarePacket = visualEntity.GetArmorUpgradeRareSubPacket(),
                Size = visualEntity.Size
            };
        }


        public static CInfoPacket GenerateCInfo(this ICharacterEntity visualEntity)
        {
            return new CInfoPacket
            {
                Name = visualEntity.Authority == AuthorityType.Moderator
                    ? $"[{visualEntity.GetMessageFromKey(LanguageKey.SUPPORT)}]" + visualEntity.Name : visualEntity.Name,
                Unknown1 = null,
                GroupId = -1,
                FamilyId = -1,
                FamilyName = null,
                CharacterId = visualEntity.VisualId,
                Authority = visualEntity.Authority,
                Gender = visualEntity.Gender,
                HairStyle = visualEntity.HairStyle,
                HairColor = visualEntity.HairColor,
                Class = visualEntity.Class,
                Icon = (byte)(visualEntity.DignityIcon == DignityType.Default ? (byte)visualEntity.ReputIcon : -(byte)visualEntity.DignityIcon),
                Compliment = (short)(visualEntity.Authority == AuthorityType.Moderator ? 500 : visualEntity.Compliment),
                Morph = 0,
                Invisible = false,
                FamilyLevel = 0,
                MorphUpgrade = 0,
                ArenaWinner = false
            };
        }
    }
}