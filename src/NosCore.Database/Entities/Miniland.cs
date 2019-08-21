using ChickenAPI.Packets.Enumerations;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace NosCore.Database.Entities
{
    public class Miniland
    {
        public Miniland()
        {
        }

        [MaxLength(255)]
        public string MinilandMessage { get; set; }

        public long MinilandPoint { get; set; }

        public virtual Character Owner { get; set; }

        public Guid MinilandId { get; set; }

        public MinilandState State { get; set; }

        public long OwnerId { get; set; }

        public int DailyVisitCount { get; set; }

        public int VisitCount { get; set; }

        public string WelcomeMusicInfo { get; set; }
    }
}
