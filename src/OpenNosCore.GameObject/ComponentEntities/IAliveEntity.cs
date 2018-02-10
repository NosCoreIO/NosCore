using OpenNosCore.Data;
using OpenNosCore.Packets;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Text;

namespace OpenNosCore.GameObject
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
