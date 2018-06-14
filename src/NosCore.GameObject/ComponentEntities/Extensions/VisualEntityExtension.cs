using NosCore.GameObject.ComponentEntities.Interfaces;
using NosCore.Packets.ServerPackets;

namespace NosCore.GameObject.ComponentEntities.Extensions
{
    public static class VisualEntityExtension
    {
        //in 3 {Effect} {IsSitting} -1 {SecondName} 0 -1 0 0 0 0 0 0 0 0
        //in 2 {Effect} {IsSitting} -1 {SecondName} 0 -1 0 0 0 0 0 0 0 0
        //in 1 {IsSitting} {GroupId} {HaveFairy} {FairyElement} 0 {FairyMorph} 0 {Morph} {EqRare} {FamilyId} {SecondName} {Reput} {Invisible} {MorphUpgrade} {faction} {MorphUpgrade2} {Level} {FamilyLevel} {ArenaWinner} {Compliment} {Size} {HeroLevel}
        //in 1 Carlosta - 754816 71 105 2 0 1 0 14 3 340.4855.4867.4864.4846.802.4150.4142 100 37 0 -1 4 3 0 0 0 7 86 86 2340 ~Luna~(Membre) -2 0 5 0 0 88 10 0 0 10 1
       
        //Character in packet
        public static InPacket GenerateIn(this ICharacterEntity visualEntity)
        {
            return new InPacket
            {
                VisualType = visualEntity.VisualType,
                Name = visualEntity.Name,
                VNum = visualEntity.VNum == 0 ? string.Empty : visualEntity.VNum.ToString(),
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
                    Equipment = new InEquipmentSubPacket()
                    {

                    },
                },
                InAliveSubPacket = new InAliveSubPacket
                {
                    HP = visualEntity.Hp,
                    MP = visualEntity.Mp
                },
				IsSitting = visualEntity.IsSitting,
            };
        }

        //Pet Monster in packet
        public static InPacket GenerateIn(this INamedEntity visualEntity)
        {
            return new InPacket
            {
                VisualType = visualEntity.VisualType,
                Name = visualEntity.Name,
                VNum = visualEntity.VNum == 0 ? string.Empty : visualEntity.VNum.ToString(),
                PositionX = visualEntity.PositionX,
                PositionY = visualEntity.PositionY,
                Direction = visualEntity.Direction
            };
        }

        //TODO move to its own class when correctly defined
        //Item in packet
        public static InPacket GenerateIn(this ICountableEntity visualEntity)
        {
            return new InPacket
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