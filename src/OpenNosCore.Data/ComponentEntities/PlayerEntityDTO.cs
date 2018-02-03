using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Text;

namespace OpenNosCore.Data
{
    public class PlayerEntityDTO : AliveEntity
    {
        public PlayerEntityDTO() : base()
        {

        }

        public byte Level { get; set; }

        public long LevelXp { get; set; }
    }
}
