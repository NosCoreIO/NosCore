using OpenNosCore.Packets;
using System;
using System.Collections.Generic;
using System.Text;

namespace OpenNosCore.Data
{
    public class VisualEntityDTO
    {
        public byte VisualType { get; set; }

        public virtual short VNum { get; set; }

        public string Name { get; set; }

        public long VisualId { get; set; }

        public byte? Direction { get; set; }

        public short MapId { get; set; }

        public short PositionX { get; set; }

        public short PositionY { get; set; }

        public short? Amount { get; set; }

        public InCharacterSubPacket InCharacterSubPacket { get; set; }

        public InAliveSubPacket InAliveSubPacket { get; set; }

        public InItemSubPacket InItemSubPacket { get; set; }

        public InNonPlayerSubPacket InNonPlayerSubPacket { get; set; }

        public InOwnableSubPacket InOwnableSubPacket { get; set; }
        //in 3 {Effect} {IsSitting} -1 {SecondName} 0 -1 0 0 0 0 0 0 0 0
        //in 2 {Effect} {IsSitting} -1 {SecondName} 0 -1 0 0 0 0 0 0 0 0
        //in 1 {IsSitting} {GroupId} {HaveFairy} {FairyElement} 0 {FairyMorph} 0 {Morph} {EqRare} {FamilyId} {SecondName} {Reput} {Invisible} {MorphUpgrade} {faction} {MorphUpgrade2} {Level} {FamilyLevel} {ArenaWinner} {Compliment} {Size} {HeroLevel}

        public InPacket GenerateIn()
        {
            return new InPacket()
            {
                VisualType = VisualType,
                Name = Name,
                VNum = VNum == 0 ? string.Empty : VNum.ToString(),
                PositionX = PositionX,
                PositionY = PositionY,
                Direction = Direction,
                Amount = Amount,
                InCharacterSubPacket = InCharacterSubPacket,
                InAliveSubPacket = InAliveSubPacket,
                InItemSubPacket = InItemSubPacket,
                InNonPlayerSubPacket = InNonPlayerSubPacket,
                InOwnableSubPacket = InOwnableSubPacket,
            };
        }
    }
}
