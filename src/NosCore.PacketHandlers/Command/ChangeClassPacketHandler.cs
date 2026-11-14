//  __  _  __    __   ___ __  ___ ___
// |  \| |/__\ /' _/ / _//__\| _ \ __|
// | | ' | \/ |`._`.| \_| \/ | v / _|
// |_|\__|\__/ |___/ \__/\__/|_|_\___|
//

using Microsoft.Extensions.Options;
using NosCore.Algorithm.ExperienceService;
using NosCore.Algorithm.HeroExperienceService;
using NosCore.Algorithm.SpExperienceService;
using NosCore.Algorithm.SkillPointService;
using NosCore.Algorithm.JobExperienceService;
using NosCore.Core.Configuration;
using NosCore.Data.CommandPackets;
using NosCore.Data.Enumerations;
using NosCore.GameObject.Ecs.Extensions;
using NosCore.GameObject.Infastructure;
using NosCore.GameObject.Networking.ClientSession;
using NosCore.GameObject.Services.RemoteCharacterService;
using NosCore.GameObject.Services.ItemGenerationService;
using NosCore.Packets.Enumerations;
using NosCore.Packets.ServerPackets.UI;
using System.Threading.Tasks;

namespace NosCore.PacketHandlers.Command
{
    public class ChangeClassPacketHandler(IRemoteCharacterService remoteCharacterService,
        IOptions<WorldConfiguration> worldConfiguration, IExperienceService experienceService,
        IJobExperienceService jobExperienceService, IHeroExperienceService heroExperienceService, ISpExperienceService spExperienceService, ISkillPointService skillPointService,
        IItemGenerationService itemProvider,
        NosCore.GameObject.Services.SkillService.ISkillService skillService)
        : PacketHandler<ChangeClassPacket>, IWorldPacketHandler
    {
        public override async Task ExecuteAsync(ChangeClassPacket changeClassPacket, ClientSession session)
        {
            if ((changeClassPacket.Name == session.Character.Name) || string.IsNullOrEmpty(changeClassPacket.Name))
            {
                await session.ChangeClassAsync(changeClassPacket.ClassType, worldConfiguration, experienceService, jobExperienceService, heroExperienceService, spExperienceService, skillPointService, itemProvider, skillService);
                return;
            }

            if (!await remoteCharacterService.UpdateStatAsync(changeClassPacket.Name, UpdateStatActionType.UpdateClass, (byte)changeClassPacket.ClassType))
            {
                await session.SendPacketAsync(new InfoiPacket
                {
                    Message = Game18NConstString.UnknownCharacter
                });
            }
        }
    }
}
