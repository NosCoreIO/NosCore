using System;
using System.Collections.Generic;
using System.Linq;
using NosCore.Data;
using NosCore.Data.AliveEntities;
using NosCore.Database.Entities;
using NosCore.DAL;
using NosCore.GameObject.ComponentEntities.Interfaces;
using NosCore.GameObject.Helper;
using NosCore.GameObject.Networking;
using NosCore.Packets.ServerPackets;
using NosCore.Shared.Enumerations;
using NosCore.Shared.Enumerations.Account;
using NosCore.Shared.Enumerations.Character;
using NosCore.Shared.I18N;
using NosCore.Shared.Enumerations.Items;

namespace NosCore.GameObject
{
    public class Character : CharacterDTO, ICharacterEntity
    {
        public Character()
        {
            FriendRequestCharacters = new List<long>();
        }

        private byte _speed;
        public AccountDTO Account { get; set; }

        public bool IsChangingMapInstance { get; set; }

        public List<CharacterRelationDTO> CharacterRelations
        {
            get
            {
                lock (ServerManager.Instance.CharacterRelations)
                {
                    return ServerManager.Instance.CharacterRelations == null
                        ? new List<CharacterRelationDTO>()
                        : ServerManager.Instance.CharacterRelations.Where(s =>
                            s.CharacterId == CharacterId || s.RelatedCharacterId == CharacterId).ToList();
                }
            }
        }

        public List<long> FriendRequestCharacters { get; set; }

        public MapInstance MapInstance { get; set; }

        public double LastPortal { get; set; }

        public ClientSession Session { get; set; }

        public short? Amount { get; set; }

        public DateTime LastSpeedChange { get; set; }

        public DateTime LastMove { get; set; }
        public bool InvisibleGm { get; set; }

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

        public void DeleteItem(PocketType pocketType, byte slot)
        {
            Inventory.DeleteFromTypeAndSlot(pocketType, slot);
            Session.SendPacket(new IvnPacket { Type = pocketType, IvnSubPackets = new List<IvnSubPacket> { new IvnSubPacket { Slot = slot } } });
        }

        public bool NoAttack { get; set; }

        public bool NoMove { get; set; }
        public bool IsSitting { get; set; }
        public Guid MapInstanceId { get; set; }
        public byte Authority { get; set; }

        public byte Equipment { get; set; }
        public bool IsAlive { get; set; }
        public Inventory Inventory { get; set; }
        public bool InExchangeOrTrade { get; set; }

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
            const int i = 0;
            //foreach (CharacterDTO characterDto in ServerManager.Instance.TopReputation)
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

        public void GenerateFinit()
        {
            var subpackets = new List<FinitSubPacket>();
            foreach (CharacterRelationDTO relation in CharacterRelations.Where(s => s.RelationType == CharacterRelationType.Friend || s.RelationType == CharacterRelationType.Spouse))
            {
                if (relation.RelatedCharacterId == CharacterId)
                {
                    continue;
                }

                string relatedCharacter = DAOFactory.CharacterDAO.FirstOrDefault(s => s.CharacterId == relation.RelatedCharacterId)?.Name;

                if (relatedCharacter == null)
                {
                    continue;
                }

                subpackets.Add(new FinitSubPacket
                {
                    CharacterId = relation.RelatedCharacterId,
                    RelationType = relation.RelationType,
                    IsOnline = ServerManager.Instance.IsCharacterConnected(relation.RelatedCharacterId),
                    CharacterName = relatedCharacter
                });
            }

            Session.SendPacket(new FinitPacket
            {
                SubPackets = subpackets
            });
        }

        public void AddRelation(long characterId, CharacterRelationType relationType)
        {
            var relation = new CharacterRelationDTO
            {
                CharacterId = CharacterId,
                RelatedCharacterId = characterId,
                RelationType = relationType
            };

            DAOFactory.CharacterRelationDAO.InsertOrUpdate(ref relation);
            ServerManager.Instance.RefreshRelations(relation.CharacterRelationId);
            GenerateFinit();
        }

        public bool IsFriendListFull()
        {
            return CharacterRelations.Where(s => s.RelationType == CharacterRelationType.Friend).ToList().Count >= 80;
        }

        public bool IsBlockedByCharacter(long characterId)
        {
            return CharacterRelations.Any(s => s.RelationType == CharacterRelationType.Blocked && s.RelatedCharacterId.Equals(characterId) && !s.RelatedCharacterId.Equals(CharacterId) && s.CharacterId.Equals(CharacterId));
        }

        public bool IsFriendOfCharacter(long characterId)
        {
            return CharacterRelations.Any(s => s.RelationType == CharacterRelationType.Friend && s.RelatedCharacterId.Equals(characterId) && s.CharacterId.Equals(CharacterId));
        }

        public bool IsMarriedToCharacter(long characterId)
        {
            return CharacterRelations.Any(s => s.RelationType == CharacterRelationType.Spouse && s.RelatedCharacterId.Equals(characterId) && CharacterId.Equals(s.CharacterId));
        }

        public int GetReputIco()
        {
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
                }
            }

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

            return Reput <= 5000000 ? 26 : 27;
        }

        public void Save()
        {
            try
            {
                AccountDTO account = Session.Account;
                DAOFactory.AccountDAO.InsertOrUpdate(ref account);

                CharacterDTO character = (Character)MemberwiseClone();
                DAOFactory.CharacterDAO.InsertOrUpdate(ref character);
            }
            catch (Exception e)
            {
                Logger.Log.Error("Save Character failed. SessionId: " + Session.SessionId, e);
            }
        }

        public void LoadSpeed()
        {
            Speed = CharacterHelper.Instance.SpeedData[Class];
        }

        public double MPLoad()
        {
            const int mp = 0;
            const double multiplicator = 1.0;
            return (int)((CharacterHelper.Instance.MpData[Class, Level] + mp) * multiplicator);
        }

        public double HPLoad()
        {
            const double multiplicator = 1.0;
            const int hp = 0;

            return (int)((CharacterHelper.Instance.HpData[Class, Level] + hp) * multiplicator);
        }

        //TODO move to extension
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
                    Enum.Parse(typeof(CharacterClassType), Class.ToString()).ToString().ToUpper())),
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
                Icon = 1,
                Compliment = (short)(Account.Authority == AuthorityType.Moderator ? 500 : Compliment),
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
                HP = Hp,
                HPMaximum = HPLoad(),
                MP = Mp,
                MPMaximum = MPLoad(),
                Unknown = 0,
                Option = 0
            };
        }
    }
}