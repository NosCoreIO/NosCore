using System;
using NosCore.Data.AliveEntities;
using NosCore.Data.StaticEntities;
using NosCore.GameObject.ComponentEntities.Interfaces;
using NosCore.Shared.Enumerations;

namespace NosCore.GameObject
{
    public class MapMonster : MapMonsterDTO, INonPlayableEntity
    {
        public bool IsSitting { get; set; }
        public byte Class { get; set; }
        public byte Speed { get; set; }
        public int Mp { get; set; }
        public int Hp { get; set; }
        public byte Morph { get; set; }
        public byte MorphUpgrade { get; set; }
        public byte MorphDesign { get; set; }
        public byte MorphBonus { get; set; }
        public bool NoAttack { get; set; }
        public bool NoMove { get; set; }

        public VisualType VisualType => VisualType.Monster;

        public long VisualId => MapMonsterId;

        public Guid MapInstanceId { get; set; }
        public short PositionX { get; set; }
        public short PositionY { get; set; }
        public short Effect { get; set; }
        public short EffectDelay { get; set; }
        public string Name { get; set; }
        public NpcMonsterDTO Monster { get; set; }
    }
}