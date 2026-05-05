//  __  _  __    __   ___ __  ___ ___
// |  \| |/__\ /' _/ / _//__\| _ \ __|
// | | ' | \/ |`._`.| \_| \/ | v / _|
// |_|\__|\__/ |___/ \__/\__/|_|_\___|
//

using System.Threading.Tasks;
using NosCore.Algorithm.ExperienceService;
using NosCore.Algorithm.HeroExperienceService;
using NosCore.Algorithm.JobExperienceService;
using NosCore.Data.CommandPackets;
using NosCore.GameObject.Ecs.Extensions;
using NosCore.GameObject.Infastructure;
using NosCore.GameObject.Networking.ClientSession;
using NosCore.Networking;

namespace NosCore.PacketHandlers.Command
{
    public class SetJobLevelXpPacketHandler(IExperienceService experienceService,
        IJobExperienceService jobExperienceService, IHeroExperienceService heroExperienceService)
        : PacketHandler<SetJobLevelXpPacket>, IWorldPacketHandler
    {
        public override Task ExecuteAsync(SetJobLevelXpPacket packet, ClientSession session)
        {
            session.Character.JobLevelXp = packet.JobLevelXp < 0 ? 0 : packet.JobLevelXp;
            return session.SendPacketAsync(session.Character.GenerateLev(experienceService, jobExperienceService, heroExperienceService));
        }
    }
}
