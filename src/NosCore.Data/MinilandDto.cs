using ChickenAPI.Packets.Enumerations;
using System;

namespace NosCore.Data
{
    public class MinilandDto : IDto
    {
        public Guid MinilandId { get; set; }

        public MinilandState State { get; set; }

        public long OwnerId { get; set; }

        public string MinilandMessage { get; set; }

        public int DailyVisitCount { get; set; }

        public int VisitCount { get; set; }

        public long MinilandPoint { get; set; }

        public string WelcomeMusicInfo { get; set; }
    }
}
