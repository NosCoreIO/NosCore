//  __  _  __    __   ___ __  ___ ___
// |  \| |/__\ /' _/ / _//__\| _ \ __|
// | | ' | \/ |`._`.| \_| \/ | v / _|
// |_|\__|\__/ |___/ \__/\__/|_|_\___|
//

using NosCore.Algorithm.ExperienceService;
using NosCore.Algorithm.HeroExperienceService;
using NosCore.Algorithm.SkillPointService;
using NosCore.Algorithm.JobExperienceService;
using NosCore.Core.I18N;
using NosCore.Data.CommandPackets;
using NosCore.Data.Enumerations;
using NosCore.GameObject.Ecs.Extensions;
using NosCore.GameObject.Infastructure;
using NosCore.GameObject.Networking.ClientSession;
using NosCore.GameObject.Services.RemoteCharacterService;
using NosCore.GameObject.Services.BroadcastService;
using NosCore.Packets.Enumerations;
using NosCore.Packets.ServerPackets.UI;
using System.Threading.Tasks;

namespace NosCore.PacketHandlers.Command
{
    public class SetLevelCommandPacketHandler(IRemoteCharacterService remoteCharacterService,
        IExperienceService experienceService, IJobExperienceService jobExperienceService,
        IHeroExperienceService heroExperienceService, ISkillPointService skillPointService, ISessionRegistry sessionRegistry,
        IGameLanguageLocalizer gameLanguageLocalizer)
        : PacketHandler<SetLevelCommandPacket>, IWorldPacketHandler
    {
        public override async Task ExecuteAsync(SetLevelCommandPacket levelPacket, ClientSession session)
        {
            if (string.IsNullOrEmpty(levelPacket.Name) || (levelPacket.Name == session.Character.Name))
            {
                await session.Character.SetLevelAsync(levelPacket.Level, experienceService, jobExperienceService, heroExperienceService, skillPointService, sessionRegistry, gameLanguageLocalizer);
                return;
            }

            if (!await remoteCharacterService.UpdateStatAsync(levelPacket.Name, UpdateStatActionType.UpdateLevel, levelPacket.Level))
            {
                await session.SendPacketAsync(new InfoiPacket
                {
                    Message = Game18NConstString.UnknownCharacter
                });
            }
        }
    }
}
