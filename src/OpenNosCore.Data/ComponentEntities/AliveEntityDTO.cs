using OpenNosCore.Packets;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Text;

namespace OpenNosCore.Data
{
    public class AliveEntity : VisualEntityDTO
    {
        public AliveEntity() : base()
        {
            InAliveSubPacket = new InAliveSubPacket();
        }

        public bool IsSitting { get; set; }

        public short MapX { get; set; }

        public short MapY { get; set; }

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

        public CondPacket GenerateCond()
        {
            return new CondPacket()
            {
                VisualType = VisualType,
                VisualId = VisualId,
                NoAttack = NoAttack,
                NoMove = NoMove,
                Speed = Speed
            };
        }

        public SayPacket GenerateSay(byte type, string message)
        {
            return new SayPacket()
            {
                VisualType = VisualType,
                VisualId = VisualId,
                Type = type,
                Message = message,
            };
        }

        public CModePacket GenerateCMode()
        {
            return new CModePacket()
            {
                VisualType = VisualType,
                VisualId = VisualId,
                Morph = Morph,
                MorphUpgrade = MorphUpgrade,
                MorphDesign = MorphDesign,
                MorphBonus = MorphBonus
            };
        }
    }
}
