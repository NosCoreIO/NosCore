using OpenNosCore.Core.Logger;
using OpenNosCore.Data;
using OpenNosCore.Enum;
using OpenNosCore.Packets;
using OpenNosCore.GameObject.Helper;
using System;
using OpenNosCore.GameObject.ComponentEntities;

namespace OpenNosCore.GameObject
{
    public class Character : CharacterDTO, ICharacterEntity
    {
        public AccountDTO Account { get; set; }

        public bool IsChangingMapInstance { get; set; }

        public MapInstance MapInstance { get; set; }

        public ClientSession Session { get; set; }

        public byte VisualType {get; set; }

        public short VNum {get; set; }

        public long VisualId { get; set; } = 1;

        public byte? Direction {get; set; }

        public short PositionX {get; set; }

        public short PositionY {get; set; }

        public short? Amount {get; set; }

        public byte Speed {get; set; }

        public byte Morph {get; set; }

        public byte MorphUpgrade {get; set; }

        public byte MorphDesign {get; set; }

        public byte MorphBonus {get; set; }

        public bool NoAttack {get; set; }

        public bool NoMove {get; set; }
        public bool IsSitting { get; set; }
        public Guid MapInstanceId { get; set; }

        public double MPLoad()
        {
            int mp = 0;
            double multiplicator = 1.0;
            return (int)((CharacterHelper.Instance.MpData[(byte)Class, Level] + mp) * multiplicator);
        }
        public double HPLoad()
        {
            double multiplicator = 1.0;
            int hp = 0;

            return (int)((CharacterHelper.Instance.HpData[(byte)Class, Level] + hp) * multiplicator);
        }

        //TODO move to extension
        public AtPacket GenerateAt()
        {
            return new AtPacket()
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

        public CInfoPacket GenerateCInfo()
        {
            return new CInfoPacket()
            {
                Name = (Account.Authority == AuthorityType.Moderator ? $"[{Language.Instance.GetMessageFromKey("SUPPORT")}]" + Name : Name),
                Unknown1 = string.Empty,
                Unknown2 = -1,
                FamilyId = -1,
                FamilyName = string.Empty,
                CharacterId = CharacterId,
                Authority = (byte)Account.Authority,
                Gender = (byte)Gender,
                HairStyle = (byte)HairStyle,
                HairColor = (byte)HairColor,
                Class = (byte)Class,
                Icon = 1,
                Compliment = (short)(Account.Authority == AuthorityType.Moderator ? 500 : Compliment),
                Invisible = false,
                FamilyLevel = 0,
                MorphUpgrade = 0,
                ArenaWinner = false
            };
        }
    }
}
