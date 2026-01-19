//  __  _  __    __   ___ __  ___ ___
// |  \| |/__\ /' _/ / _//__\| _ \ __|
// | | ' | \/ |`._`.| \_| \/ | v / _|
// |_|\__|\__/ |___/ \__/\__/|_|_\___|
//

using NosCore.Data.Dto;
using NosCore.GameObject.ComponentEntities.Interfaces;
using NosCore.Packets.ServerPackets.Miniland;
using System;

namespace NosCore.GameObject.Services.MinilandService
{
    public class Miniland : MinilandDto
    {
        public Guid MapInstanceId { get; set; }

        public ICharacterEntity? CharacterEntity { get; set; }
        public int CurrentMinigame { get; set; }

        public MlInfoBrPacket GenerateMlinfobr()
        {
            return new MlInfoBrPacket
            {
                MinilandMusicId = 3800,
                Name = CharacterEntity?.Name,
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
                WelcomeMusicInfo = WelcomeMusicInfo,
                DailyVisitCount = DailyVisitCount,
                VisitCount = VisitCount,
                Unknown2 = 0,
                Unknown3 = 0,
                MinilandPoint = MinilandPoint,
                MinilandState = State,
                MinilandWelcomeMessage = MinilandMessage ?? "", //todo this has a default value in number in the new mlinfo
                WelcomeMusicInfo2 = WelcomeMusicInfo
            };
        }
    }
}
