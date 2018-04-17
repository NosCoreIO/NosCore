using NosCore.GameObject.ComponentEntities.Interfaces;
using NosCore.GameObject.Packets.ServerPackets;
using NosCore.Packets.ServerPackets;

namespace NosCore.GameObject.ComponentEntities.Extensions
{
    public static class AliveEntityExtension
    {
        public static CondPacket GenerateCond(this IAliveEntity aliveEntity)
        {
            return new CondPacket()
            {
                VisualType = aliveEntity.VisualType,
                VisualId = aliveEntity.VisualId,
                NoAttack = aliveEntity.NoAttack,
                NoMove = aliveEntity.NoMove,
                Speed = aliveEntity.Speed
            };
        }

        public static SayPacket GenerateSay(this IAliveEntity aliveEntity, string message, byte type)
        {
            return new SayPacket()
            {
                VisualType = aliveEntity.VisualType,
                VisualId = aliveEntity.VisualId,
                Type = type,
                Message = message,
            };
        }

        public static CModePacket GenerateCMode(this IAliveEntity aliveEntity)
        {
            return new CModePacket()
            {
                VisualType = aliveEntity.VisualType,
                VisualId = aliveEntity.VisualId,
                Morph = aliveEntity.Morph,
                MorphUpgrade = aliveEntity.MorphUpgrade,
                MorphDesign = aliveEntity.MorphDesign,
                MorphBonus = aliveEntity.MorphBonus
            };
        }

        public static MovePacket GenerateMove(this IAliveEntity aliveEntity)
        {
            return new MovePacket
            {
                VisualEntityId = aliveEntity.VisualId,
                MapX = aliveEntity.PositionX,
                MapY = aliveEntity.PositionY,
                Speed = aliveEntity.Speed,
                VisualType = 1
            };
        }

        public static EffectPacket GenerateEff(this IAliveEntity aliveEntity, int effectid)
        {
            return new EffectPacket
            {
                EffectType = 1,
                VisualEntityId = aliveEntity.VisualId,
                Id = effectid
            };
        }
    }
}
