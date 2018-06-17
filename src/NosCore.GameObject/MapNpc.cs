using System;
using System.Collections.Generic;
using System.Text;
using NosCore.Data.AliveEntities;
using NosCore.GameObject.ComponentEntities.Interfaces;

namespace NosCore.GameObject
{
    public class MapNpc : MapNpcDTO, INamedEntity, INonPlayableEntity
    {
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
	    public byte VisualType => 2;

        public long VisualId => MapNpcId;

        public Guid MapInstanceId { get; set; }
        public short PositionX { get; set; }
        public short PositionY { get; set; }
        public string Name { get; set; }
    }
}
