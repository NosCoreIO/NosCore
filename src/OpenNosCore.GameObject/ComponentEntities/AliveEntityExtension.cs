using OpenNosCore.Packets;
using System;
using System.Collections.Generic;
using System.Text;

namespace OpenNosCore.GameObject.ComponentEntities
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

        public static SayPacket GenerateSay(this IAliveEntity aliveEntity, byte type, string message)
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
        }
}
