using NosCore.Data;
using NosCore.Packets;
using System;
using System.Collections.Generic;
using System.Text;

namespace NosCore.GameObject
{
    public interface IAliveEntity : IVisualEntity
    {
        bool IsSitting { get; set; }

        byte Class { get; set; }

        byte Speed { get; set; }

        int Mp { get; set; }

        int Hp { get; set; }

        byte Morph { get; set; }

        byte MorphUpgrade { get; set; }

        byte MorphDesign { get; set; }

        byte MorphBonus { get; set; }

        bool NoAttack { get; set; }

        bool NoMove { get; set; }
        
    }
}
