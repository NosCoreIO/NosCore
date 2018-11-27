//  __  _  __    __   ___ __  ___ ___  
// |  \| |/__\ /' _/ / _//__\| _ \ __| 
// | | ' | \/ |`._`.| \_| \/ | v / _|  
// |_|\__|\__/ |___/ \__/\__/|_|_\___| 
// 
// Copyright (C) 2018 - NosCore
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

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using DotNetty.Transport.Channels;
using NosCore.Core;
using NosCore.Core.Networking;
using NosCore.Core.Serializing;
using NosCore.Data;
using NosCore.Data.AliveEntities;
using NosCore.Data.WebApi;
using NosCore.DAL;
using NosCore.GameObject.ComponentEntities.Extensions;
using NosCore.GameObject.ComponentEntities.Interfaces;
using NosCore.GameObject.Helper;
using NosCore.GameObject.Networking;
using NosCore.GameObject.Networking.ClientSession;
using NosCore.GameObject.Services.Inventory;
using NosCore.GameObject.Services.ItemBuilder.Item;
using NosCore.GameObject.Services.MapInstanceAccess;
using NosCore.Packets.ServerPackets;
using NosCore.Shared.Enumerations;
using NosCore.Shared.Enumerations.Account;
using NosCore.Shared.Enumerations.Character;
using NosCore.Shared.Enumerations.Group;
using NosCore.Shared.Enumerations.Interaction;
using NosCore.Shared.Enumerations.Items;
using NosCore.Shared.I18N;
using Serilog;

namespace NosCore.GameObject
{
    public class Character : CharacterDto, ICharacterEntity
    {
        private byte _speed;

        public Character()
        {
            FriendRequestCharacters = new ConcurrentDictionary<long, long>();
            CharacterRelations = new ConcurrentDictionary<Guid, CharacterRelation>();
            RelationWithCharacter = new ConcurrentDictionary<Guid, CharacterRelation>();
            GroupRequestCharacterIds = new ConcurrentDictionary<long, long>();
            Group = new Group(GroupType.Group);
        }

        private readonly ILogger _logger = Logger.GetLoggerConfiguration().CreateLogger();

        public ConcurrentDictionary<long, long> GroupRequestCharacterIds { get; set; }

        public AccountDto Account { get; set; }

        public bool IsChangingMapInstance { get; set; }

        public ConcurrentDictionary<Guid, CharacterRelation> CharacterRelations { get; set; }

        public ConcurrentDictionary<Guid, CharacterRelation> RelationWithCharacter { get; set; }

        public bool IsFriendListFull
        {
            get => CharacterRelations.Where(s => s.Value.RelationType == CharacterRelationType.Friend).ToList().Count
                >= 80;
        }

        public ConcurrentDictionary<long, long> FriendRequestCharacters { get; set; }

        public DateTime LastPortal { get; set; }

        public ClientSession Session { get; set; }

        public short? Amount { get; set; }

        public DateTime LastSpeedChange { get; set; }

        public DateTime LastMove { get; set; }

        public bool Camouflage { get; set; }

        public bool Invisible { get; set; }
        public IInventoryService Inventory { get; set; }
        public bool InExchangeOrTrade { get; set; }

        public Group Group { get; set; }

        public int ReputIcon => GetReputIco();

        public int DignityIcon => GetDignityIco();

        public IChannel Channel => Session?.Channel;

        public void SendPacket(PacketDefinition packetDefinition) => Session.SendPacket(packetDefinition);

        public void SendPackets(IEnumerable<PacketDefinition> packetDefinitions) => Session.SendPackets(packetDefinitions);

        public MapInstance MapInstance { get; set; }

        public VisualType VisualType => VisualType.Player;

        public short VNum { get; set; }

        public long VisualId => CharacterId;

        public byte Direction { get; set; }

        public short PositionX { get; set; }

        public short PositionY { get; set; }

        public byte Speed
        {
            get
            {
                //    if (HasBuff(CardType.Move, (byte)AdditionalTypes.Move.MovementImpossible))
                //    {
                //        return 0;
                //    }

                const int
                    bonusSpeed = 0; /*(byte)GetBuff(CardType.Move, (byte)AdditionalTypes.Move.SetMovementNegated)[0];*/
                if (_speed + bonusSpeed > 59)
                {
                    return 59;
                }

                return (byte)(_speed + bonusSpeed);
            }

            set
            {
                LastSpeedChange = DateTime.Now;
                _speed = value > 59 ? (byte)59 : value;
            }
        }

        public byte Morph { get; set; }

        public byte MorphUpgrade { get; set; }

        public byte MorphDesign { get; set; }

        public byte MorphBonus { get; set; }
        public bool NoAttack { get; set; }

        public bool NoMove { get; set; }
        public bool IsSitting { get; set; }
        public Guid MapInstanceId { get; set; }

        public AuthorityType Authority => Account.Authority;

        public byte Equipment { get; set; }
        public bool IsAlive { get; set; }

        public int MaxHp => (int)HpLoad();

        public int MaxMp => (int)MpLoad();

        public void SetLevel(byte level)
        {
            Level = level;
            LevelXp = 0;
            Hp = MaxHp;
            Mp = MaxMp;
            SendPacket(GenerateStat());
            SendPacket(this.GenerateStatInfo());
            //Session.SendPacket(Session.Character.GenerateStatChar());
            SendPacket(GenerateLev());
            var mapSessions = Broadcaster.Instance.GetCharacters(s => s.MapInstance == MapInstance);

            Parallel.ForEach(mapSessions, s =>
            {
                if (s.VisualId != VisualId)
                {
                    s.SendPacket(this.GenerateIn(Authority == AuthorityType.Moderator ? Language.Instance.GetMessageFromKey(LanguageKey.SUPPORT, Account.Language) : string.Empty));
                    //TODO: Generate GIDX
                }

                s.SendPacket(this.GenerateEff(6));
                s.SendPacket(this.GenerateEff(198));
            });

            foreach (var member in Group.Keys)
            {
                var groupMember = Broadcaster.Instance.GetCharacter(s => s.VisualId == member.Item2 && member.Item1 == VisualType.Player);

                groupMember?.SendPacket(groupMember.Group.GeneratePinit());
            }

            SendPacket(Group.GeneratePinit());
            Session.SendPacket(new MsgPacket { Type = MessageType.Whisper, Message = Language.Instance.GetMessageFromKey(LanguageKey.LEVEL_CHANGED, Session.Account.Language) });
        }

        public void SetJobLevel(byte jobLevel)
        {
            JobLevel = (byte)((CharacterClassType)Class == CharacterClassType.Adventurer && jobLevel > 20 ? 20 : jobLevel);
            JobLevelXp = 0;
            SendPacket(GenerateLev());
            var mapSessions = Broadcaster.Instance.GetCharacters(s => s.MapInstance == MapInstance);
            Parallel.ForEach(mapSessions, s =>
            {
                //if (s.VisualId != Session.Character.VisualId)
                //{
                //    TODO: Generate GIDX
                //}

                s.SendPacket(this.GenerateEff(8));
            });
            Session.SendPacket(new MsgPacket { Type = MessageType.Whisper, Message = Language.Instance.GetMessageFromKey(LanguageKey.JOB_LEVEL_CHANGED, Session.Account.Language) });
        }

        public LevPacket GenerateLev()
        {
            return new LevPacket
            {
                Level = Level,
                LevelXp = LevelXp,
                JobLevel = JobLevel,
                JobLevelXp = JobLevelXp,
                XpLoad = (int)CharacterHelper.Instance.XpLoad(Level),
                JobXpLoad = (int)CharacterHelper.Instance.JobXpLoad(JobLevel, Class),
                Reputation = Reput,
                SkillCp = 0,
                HeroXp = HeroXp,
                HeroLevel = HeroLevel,
                HeroXpLoad = (int)CharacterHelper.Instance.HeroXpLoad(HeroLevel)
            };
        }

        public void JoinGroup(Group group)
        {
            Group = group;
            group.JoinGroup(this);
        }

        public void LeaveGroup()
        {
            Group.LeaveGroup(this);
            foreach (var member in Group.Keys.Where(s => s.Item2 != CharacterId || s.Item1 != VisualType.Player))
            {
                var groupMember = Broadcaster.Instance.GetCharacter(s =>
                    s.VisualId == member.Item2 && member.Item1 == VisualType.Player);

                if (Group.Count == 1)
                {
                    groupMember?.LeaveGroup();
                    groupMember?.SendPacket(Group.GeneratePidx(groupMember));
                    groupMember?.SendPacket(new MsgPacket
                    {
                        Message = groupMember.GetMessageFromKey(LanguageKey.GROUP_CLOSED),
                        Type = MessageType.Whisper
                    });
                }

                groupMember?.SendPacket(groupMember.Group.GeneratePinit());
            }

            Group = new Group(GroupType.Group);
            Group.JoinGroup(this);
        }

        public string GetMessageFromKey(LanguageKey languageKey) => Session.GetMessageFromKey(languageKey);

        public FdPacket GenerateFd()
        {
            return new FdPacket
            {
                Reput = Reput,
                Dignity = (int)Dignity,
                ReputIcon = GetReputIco(),
                DignityIcon = Math.Abs(GetDignityIco())
            };
        }

        public int GetDignityIco()
        {
            var icoDignity = 1;

            if (Dignity <= -100)
            {
                icoDignity = 2;
            }

            if (Dignity <= -200)
            {
                icoDignity = 3;
            }

            if (Dignity <= -400)
            {
                icoDignity = 4;
            }

            if (Dignity <= -600)
            {
                icoDignity = 5;
            }

            if (Dignity <= -800)
            {
                icoDignity = 6;
            }

            return icoDignity;
        }

        public int IsReputHero()
        {
            //const int i = 0;
            //foreach (CharacterDTO characterDto in Broadcaster.Instance.TopReputation)
            //{
            //    Character character = (Character)characterDto;
            //    i++;
            //    if (character.CharacterId != CharacterId)
            //    {
            //        continue;
            //    }
            //    switch (i)
            //    {
            //        case 1:
            //            return 5;
            //        case 2:
            //            return 4;
            //        case 3:
            //            return 3;
            //    }
            //    if (i <= 13)
            //    {
            //        return 2;
            //    }
            //    if (i <= 43)
            //    {
            //        return 1;
            //    }
            //}
            return 0;
        }

        public PacketDefinition GenerateSpPoint()
        {
            return new SpPacket
            {
                AdditionalPoint = SpAdditionPoint,
                MaxAdditionalPoint = 1000000,
                SpPoint = SpPoint,
                MaxSpPoint = 10000
            };
        }

        public void SendRelationStatus(bool status)
        {
            foreach (var characterRelation in CharacterRelations)
            {
                var targetSession = Broadcaster.Instance.GetCharacter(s => s.VisualId == characterRelation.Value.RelatedCharacterId);

                if (targetSession != null)
                {
                    targetSession.SendPacket(new FinfoPacket
                    {
                        FriendList = new List<FinfoSubPackets>
                        {
                            new FinfoSubPackets
                            {
                                CharacterId = CharacterId,
                                IsConnected = status
                            }
                        }
                    });
                }
                else
                {
                    WebApiAccess.Instance.BroadcastPacket(new PostedPacket
                    {
                        Packet = PacketFactory.Serialize(new[]
                        {
                            new FinfoPacket
                            {
                                FriendList = new List<FinfoSubPackets>
                                {
                                    new FinfoSubPackets
                                    {
                                        CharacterId = CharacterId,
                                        IsConnected = status
                                    }
                                }
                            }
                        }),
                        ReceiverType = ReceiverType.OnlySomeone,
                        SenderCharacter = new Data.WebApi.Character { Id = CharacterId, Name = Name },
                        ReceiverCharacter = new Data.WebApi.Character
                        {
                            Id = characterRelation.Value.RelatedCharacterId,
                            Name = characterRelation.Value.CharacterName
                        }
                    });
                }
            }
        }

        public BlinitPacket GenerateBlinit()
        {
            var subpackets = new List<BlinitSubPacket>();
            foreach (var relation in CharacterRelations.Values.Where(s =>
                s.RelationType == CharacterRelationType.Blocked))
            {
                if (relation.RelatedCharacterId == CharacterId)
                {
                    continue;
                }

                subpackets.Add(new BlinitSubPacket
                {
                    RelatedCharacterId = relation.RelatedCharacterId,
                    CharacterName = relation.CharacterName
                });
            }

            return new BlinitPacket { SubPackets = subpackets };
        }

        public void DeleteBlackList(long characterId)
        {
            var relation = CharacterRelations.Values.FirstOrDefault(s =>
                s.RelatedCharacterId == characterId && s.RelationType == CharacterRelationType.Blocked);

            if (relation == null)
            {
                Session.SendPacket(new InfoPacket
                {
                    Message = Language.Instance.GetMessageFromKey(LanguageKey.CANT_FIND_CHARACTER,
                        Session.Account.Language)
                });
                return;
            }

            CharacterRelations.TryRemove(relation.CharacterRelationId, out _);
            Session.SendPacket(GenerateBlinit());
        }

        public CharacterRelation AddRelation(long characterId, CharacterRelationType relationType)
        {
            var relation = new CharacterRelation
            {
                CharacterId = CharacterId,
                RelatedCharacterId = characterId,
                RelationType = relationType,
                CharacterName = Broadcaster.Instance.GetCharacter(s => s.VisualId == characterId)?.Name,
                CharacterRelationId = Guid.NewGuid()
            };

            CharacterRelations[relation.CharacterRelationId] = relation;
            CharacterRelationDto relationDto = relation;

            if (DaoFactory.CharacterRelationDao.FirstOrDefault(s =>
                s.CharacterId == CharacterId && s.RelatedCharacterId == characterId) == null)
            {
                DaoFactory.CharacterRelationDao.InsertOrUpdate(ref relationDto);
            }

            if (relationType == CharacterRelationType.Blocked)
            {
                Session.SendPacket(GenerateBlinit());
                return relation;
            }

            Session.SendPacket(this.GenerateFinit());
            return relation;
        }

        public void DeleteRelation(long relatedCharacterId)
        {
            var characterRelation =
                CharacterRelations.Values.FirstOrDefault(s => s.RelatedCharacterId == relatedCharacterId);
            var targetCharacterRelation =
                RelationWithCharacter.Values.FirstOrDefault(s => s.RelatedCharacterId == CharacterId);

            if (characterRelation == null || targetCharacterRelation == null)
            {
                return;
            }

            CharacterRelations.TryRemove(characterRelation.CharacterRelationId, out _);
            RelationWithCharacter.TryRemove(targetCharacterRelation.CharacterRelationId, out _);
            Session.SendPacket(this.GenerateFinit());

            var targetSession = Broadcaster.Instance.GetCharacter(s =>
                s.VisualId == targetCharacterRelation.CharacterId);
            if (targetSession != null)
            {
                targetSession.CharacterRelations.TryRemove(targetCharacterRelation.CharacterRelationId,
                    out _);
                targetSession.RelationWithCharacter.TryRemove(characterRelation.CharacterRelationId, out _);
                targetSession.SendPacket(targetSession.GenerateFinit());
                return;
            }

            var servers = WebApiAccess.Instance.Get<List<WorldServerInfo>>("api/channels");
            foreach (var server in servers)
            {
                var account = WebApiAccess.Instance.Get<List<ConnectedAccount>>("api/connectedAccount", server.WebApi)
                    .Find(s => s.ConnectedCharacter.Id == targetCharacterRelation.CharacterId);

                if (account != null)
                {
                    WebApiAccess.Instance.Delete<CharacterRelation>("api/relation", server.WebApi,
                        targetCharacterRelation.CharacterRelationId);
                    return;
                }
            }

            DaoFactory.CharacterRelationDao.Delete(targetCharacterRelation);
        }

        [Obsolete(
            "GenerateStartupInventory should be used only on startup, for refreshing an inventory slot please use GenerateInventoryAdd instead.")]
        public IEnumerable<PacketDefinition> GenerateInv()
        {
            var inv0 = new InvPacket { Type = PocketType.Equipment, IvnSubPackets = new List<IvnSubPacket>() };
            var inv1 = new InvPacket { Type = PocketType.Main, IvnSubPackets = new List<IvnSubPacket>() };
            var inv2 = new InvPacket { Type = PocketType.Etc, IvnSubPackets = new List<IvnSubPacket>() };
            var inv3 = new InvPacket { Type = PocketType.Miniland, IvnSubPackets = new List<IvnSubPacket>() };
            var inv6 = new InvPacket { Type = PocketType.Specialist, IvnSubPackets = new List<IvnSubPacket>() };
            var inv7 = new InvPacket { Type = PocketType.Costume, IvnSubPackets = new List<IvnSubPacket>() };

            if (Inventory != null)
            {
                foreach (var inv in Inventory.Select(s => s.Value))
                {
                    switch (inv.Type)
                    {
                        case PocketType.Equipment:
                            if (inv.Item.EquipmentSlot == EquipmentType.Sp)
                            {
                                if (inv is SpecialistInstance specialistInstance)
                                {
                                    inv7.IvnSubPackets.Add(new IvnSubPacket
                                    {
                                        Slot = inv.Slot,
                                        VNum = inv.ItemVNum,
                                        RareAmount = specialistInstance.Rare,
                                        UpgradeDesign = specialistInstance.Upgrade,
                                        SecondUpgrade = specialistInstance.SpStoneUpgrade
                                    });
                                }
                            }
                            else
                            {
                                if (inv is WearableInstance wearableInstance)
                                {
                                    inv0.IvnSubPackets.Add(new IvnSubPacket
                                    {
                                        Slot = inv.Slot,
                                        VNum = inv.ItemVNum,
                                        RareAmount = wearableInstance.Rare,
                                        UpgradeDesign = inv.Item.IsColored ? wearableInstance.Design
                                            : wearableInstance.Upgrade
                                    });
                                }
                            }

                            break;

                        case PocketType.Main:
                            inv1.IvnSubPackets.Add(new IvnSubPacket { Slot = inv.Slot, VNum = inv.ItemVNum, RareAmount = inv.Amount });
                            break;

                        case PocketType.Etc:
                            inv2.IvnSubPackets.Add(new IvnSubPacket { Slot = inv.Slot, VNum = inv.ItemVNum, RareAmount = inv.Amount });
                            break;

                        case PocketType.Miniland:
                            inv3.IvnSubPackets.Add(new IvnSubPacket { Slot = inv.Slot, VNum = inv.ItemVNum, RareAmount = inv.Amount });
                            break;

                        case PocketType.Specialist:
                            if (inv is SpecialistInstance specialist)
                            {
                                inv6.IvnSubPackets.Add(new IvnSubPacket
                                {
                                    Slot = inv.Slot,
                                    VNum = inv.ItemVNum,
                                    RareAmount = specialist.Rare,
                                    UpgradeDesign = specialist.Upgrade,
                                    SecondUpgrade = specialist.SpStoneUpgrade
                                });
                            }

                            break;

                        case PocketType.Costume:
                            if (inv is WearableInstance costumeInstance)
                            {
                                inv7.IvnSubPackets.Add(new IvnSubPacket
                                {
                                    Slot = inv.Slot,
                                    VNum = inv.ItemVNum,
                                    RareAmount = costumeInstance.Rare,
                                    UpgradeDesign = costumeInstance.Upgrade
                                });
                            }

                            break;
                        default:
                            _logger.Information(LogLanguage.Instance.GetMessageFromKey(LanguageKey.POCKETTYPE_UNKNOWN));
                            break;
                    }
                }
            }

            return new List<PacketDefinition> { inv0, inv1, inv2, inv3, inv6, inv7 };
        }

        public bool IsRelatedToCharacter(long characterId, CharacterRelationType relationType)
        {
            return CharacterRelations.Values.Any(s =>
                s.RelationType == relationType && s.RelatedCharacterId.Equals(characterId)
                && s.CharacterId.Equals(CharacterId));
        }

        public int GetReputIco()
        {
            if (Reput <= 50)
            {
                return 1;
            }

            if (Reput <= 150)
            {
                return 2;
            }

            if (Reput <= 250)
            {
                return 3;
            }

            if (Reput <= 500)
            {
                return 4;
            }

            if (Reput <= 750)
            {
                return 5;
            }

            if (Reput <= 1000)
            {
                return 6;
            }

            if (Reput <= 2250)
            {
                return 7;
            }

            if (Reput <= 3500)
            {
                return 8;
            }

            if (Reput <= 5000)
            {
                return 9;
            }

            if (Reput <= 9500)
            {
                return 10;
            }

            if (Reput <= 19000)
            {
                return 11;
            }

            if (Reput <= 25000)
            {
                return 12;
            }

            if (Reput <= 40000)
            {
                return 13;
            }

            if (Reput <= 60000)
            {
                return 14;
            }

            if (Reput <= 85000)
            {
                return 15;
            }

            if (Reput <= 115000)
            {
                return 16;
            }

            if (Reput <= 150000)
            {
                return 17;
            }

            if (Reput <= 190000)
            {
                return 18;
            }

            if (Reput <= 235000)
            {
                return 19;
            }

            if (Reput <= 285000)
            {
                return 20;
            }

            if (Reput <= 350000)
            {
                return 21;
            }

            if (Reput <= 500000)
            {
                return 22;
            }

            if (Reput <= 1500000)
            {
                return 23;
            }

            if (Reput <= 2500000)
            {
                return 24;
            }

            if (Reput <= 3750000)
            {
                return 25;
            }

            if (Reput <= 5000000)
            {
                return 26;
            }

            if (Reput >= 5000001)
            {
                switch (IsReputHero())
                {
                    case 1:
                        return 28;
                    case 2:
                        return 29;
                    case 3:
                        return 30;
                    case 4:
                        return 31;
                    case 5:
                        return 32;
                    default:
                        return 27;
                }
            }

            return 0;
        }

        public void Save()
        {
            try
            {
                var account = Session.Account;
                DaoFactory.AccountDao.InsertOrUpdate(ref account);

                CharacterDto character = (Character)MemberwiseClone();
                DaoFactory.CharacterDao.InsertOrUpdate(ref character);

                var savedRelations = DaoFactory.CharacterRelationDao.Where(s => s.CharacterId == CharacterId);
                DaoFactory.CharacterRelationDao.Delete(savedRelations.Except(CharacterRelations.Values));
                DaoFactory.CharacterRelationDao.InsertOrUpdate(CharacterRelations.Values);

                // load and concat inventory with equipment
                var currentlySavedInventorys = DaoFactory.ItemInstanceDao.Where(i => i.CharacterId == CharacterId);
                DaoFactory.ItemInstanceDao.Delete(currentlySavedInventorys.Except(Inventory.Values));
                DaoFactory.ItemInstanceDao.InsertOrUpdate(Inventory.Values);
            }
            catch (Exception e)
            {
                _logger.Error("Save Character failed. SessionId: " + Session.SessionId, e);
            }
        }

        public GoldPacket GenerateGold()
        {
            return new GoldPacket { Gold = Gold };
        }

        public void LoadSpeed()
        {
            Speed = CharacterHelper.Instance.SpeedData[Class];
        }

        public double MpLoad()
        {
            const int mp = 0;
            const double multiplicator = 1.0;
            return (int)((CharacterHelper.Instance.MpData[Class][Level] + mp) * multiplicator);
        }

        public double HpLoad()
        {
            const double multiplicator = 1.0;
            const int hp = 0;

            return (int)((CharacterHelper.Instance.HpData[Class][Level] + hp) * multiplicator);
        }

        public AtPacket GenerateAt()
        {
            return new AtPacket
            {
                CharacterId = CharacterId,
                MapId = MapId,
                PositionX = PositionX,
                PositionY = PositionY,
                Unknown1 = 2,
                Unknown2 = 0,
                Music = MapInstance.Map.Music,
                Unknown3 = -1
            };
        }

        public TitPacket GenerateTit()
        {
            return new TitPacket
            {
                ClassType = Session.GetMessageFromKey((LanguageKey)Enum.Parse(typeof(LanguageKey),
                    Enum.Parse(typeof(CharacterClassType), Class.ToString()).ToString().ToUpperInvariant())),
                Name = Name
            };
        }

        public CInfoPacket GenerateCInfo()
        {
            return new CInfoPacket
            {
                Name = Account.Authority == AuthorityType.Moderator
                    ? $"[{Session.GetMessageFromKey(LanguageKey.SUPPORT)}]" + Name : Name,
                Unknown1 = string.Empty,
                Unknown2 = -1,
                FamilyId = -1,
                FamilyName = string.Empty,
                CharacterId = CharacterId,
                Authority = (byte)Account.Authority,
                Gender = (byte)Gender,
                HairStyle = (byte)HairStyle,
                HairColor = (byte)HairColor,
                Class = Class,
                Icon = (byte)(GetDignityIco() == 1 ? GetReputIco() : -GetDignityIco()),
                Compliment = (short)(Account.Authority == AuthorityType.Moderator ? 500 : Compliment),
                Morph = 0,
                Invisible = false,
                FamilyLevel = 0,
                MorphUpgrade = 0,
                ArenaWinner = false
            };
        }

        public StatPacket GenerateStat()
        {
            return new StatPacket
            {
                Hp = Hp,
                HpMaximum = HpLoad(),
                Mp = Mp,
                MpMaximum = MpLoad(),
                Unknown = 0,
                Option = 0
            };
        }

        public TalkPacket GenerateTalk(string message)
        {
            return new TalkPacket
            {
                CharacterId = CharacterId,
                Message = message
            };
        }        
    }
}