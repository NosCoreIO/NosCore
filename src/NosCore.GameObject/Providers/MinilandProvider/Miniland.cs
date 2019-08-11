using ChickenAPI.Packets.ServerPackets.Miniland;
using NosCore.Data;
using NosCore.GameObject.ComponentEntities.Interfaces;
using System;

namespace NosCore.GameObject.Providers.MinilandProvider
{
    public class Miniland : MinilandDto
    {
        public Guid MapInstanceId { get; set; }

        public ICharacterEntity Owner { get; set; }

        public MlInfoBrPacket GenerateMlinfobr()
        {
            return new MlInfoBrPacket
            {
                Unknown1 = 3800,
                Name = Owner.Name,
                MinilandMessage = MinilandMessage,
                DailyVisitCount = DailyVisitCount,
                Unknown2 = 0,
                VisitCount = VisitCount
            };
        }

        public MlinfoPacket GenerateMlinfo()
        {
            return new MlinfoPacket
            {
                Unknown1 = 3800,
                DailyVisitCount = DailyVisitCount,
                VisitCount = VisitCount,
                Unknown2 = 0,
                Unknown3 = 0,
                MinilandPoint = MinilandPoint,
                MinilandState = State,
                MinilandWelcomeMessage = MinilandMessage,
                WelcomeMusicInfo = WelcomeMusicInfo,
            };
        }
    }
}
