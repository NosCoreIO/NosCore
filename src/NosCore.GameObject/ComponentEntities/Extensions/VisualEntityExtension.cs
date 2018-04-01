using NosCore.GameObject.ComponentEntities.Interfaces;
using NosCore.Packets.ServerPackets;

namespace NosCore.GameObject.ComponentEntities.Extensions
{
    public static class VisualEntityExtension
    {
        //in 3 {Effect} {IsSitting} -1 {SecondName} 0 -1 0 0 0 0 0 0 0 0
        //in 2 {Effect} {IsSitting} -1 {SecondName} 0 -1 0 0 0 0 0 0 0 0
        //in 1 {IsSitting} {GroupId} {HaveFairy} {FairyElement} 0 {FairyMorph} 0 {Morph} {EqRare} {FamilyId} {SecondName} {Reput} {Invisible} {MorphUpgrade} {faction} {MorphUpgrade2} {Level} {FamilyLevel} {ArenaWinner} {Compliment} {Size} {HeroLevel}

        //Character in packet
        public static InPacket GenerateIn(this ICharacterEntity visualEntity)
        {
            return new InPacket()
            {
                VisualType = visualEntity.VisualType,
                Name = visualEntity.Name,
                VNum = visualEntity.VNum == 0 ? string.Empty : visualEntity.VNum.ToString(),
                PositionX = visualEntity.PositionX,
                PositionY = visualEntity.PositionY,
                Direction = visualEntity.Direction,
                InAliveSubPacket = new InAliveSubPacket()
                {
                    HP = visualEntity.Hp,
                    MP = visualEntity.Mp,
                },
                InCharacterSubPacket = new InCharacterSubPacket()
                {
                    Authority = visualEntity.Authority,
                    Class = visualEntity.Class,
                    Equipment = 0,
                    Gender = (byte)visualEntity.Gender,
                    HairColor = (byte)visualEntity.HairColor,
                    HairStyle = (byte)visualEntity.HairStyle
                },
            };
        }

        //Pet Monster in packet
        public static InPacket GenerateIn(this INamedEntity visualEntity)
        {
            return new InPacket()
            {
                VisualType = visualEntity.VisualType,
                Name = visualEntity.Name,
                VNum = visualEntity.VNum == 0 ? string.Empty : visualEntity.VNum.ToString(),
                PositionX = visualEntity.PositionX,
                PositionY = visualEntity.PositionY,
                Direction = visualEntity.Direction,
            };
        }

        //TODO move to its own class when correctly defined
        //Item in packet
        public static InPacket GenerateIn(this ICountableEntity visualEntity)
        {
            return new InPacket()
            {
                VisualType = visualEntity.VisualType,
                VNum = visualEntity.VNum == 0 ? string.Empty : visualEntity.VNum.ToString(),
                PositionX = visualEntity.PositionX,
                PositionY = visualEntity.PositionY,
                Direction = visualEntity.Direction,
                Amount = visualEntity.Amount
            };
        }
    }
}
