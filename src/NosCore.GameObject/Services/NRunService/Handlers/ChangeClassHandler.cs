//  __  _  __    __   ___ __  ___ ___
// |  \| |/__\ /' _/ / _//__\| _ \ __|
// | | ' | \/ |`._`.| \_| \/ | v / _|
// |_|\__|\__/ |___/ \__/\__/|_|_\___|
//

using Microsoft.Extensions.Options;
using NosCore.Algorithm.ExperienceService;
using NosCore.Algorithm.HeroExperienceService;
using NosCore.Algorithm.JobExperienceService;
using NosCore.Core.Configuration;
using NosCore.Data.Enumerations.I18N;
using NosCore.GameObject.ComponentEntities.Extensions;
using NosCore.GameObject.ComponentEntities.Interfaces;
using NosCore.GameObject.Networking.ClientSession;
using NosCore.Packets.ClientPackets.Npcs;
using NosCore.Packets.Enumerations;
using NosCore.Packets.ServerPackets.Chats;
using NosCore.Packets.ServerPackets.UI;
using NosCore.Shared.Enumerations;
using NosCore.Shared.I18N;
using Serilog;
using System;
using System.Threading.Tasks;

namespace NosCore.GameObject.Services.NRunService.Handlers
{
    public class ChangeClassEventHandler(ILogger logger, ILogLanguageLocalizer<LogLanguageKey> languageLocalizer,
        IOptions<WorldConfiguration> worldConfiguration, IExperienceService experienceService,
        IJobExperienceService jobExperienceService, IHeroExperienceService heroExperienceService)
        : INrunEventHandler
    {
        public bool Condition(Tuple<IAliveEntity, NrunPacket> item)
        {
            return (item.Item2.Runner == NrunRunnerType.ChangeClass) &&
                (item.Item2.Type > 0) && (item.Item2.Type < 4) && (item.Item1 != null);
        }

        public async Task ExecuteAsync(RequestData<Tuple<IAliveEntity, NrunPacket>> requestData)
        {
            if (requestData.ClientSession.Character.Class != (byte)CharacterClassType.Adventurer)
            {
                return;
            }

            if (!requestData.ClientSession.Character.Group!.IsEmpty)
            {
                await requestData.ClientSession.SendPacketAsync(new SayiPacket
                {
                    VisualType = VisualType.Player,
                    VisualId = requestData.ClientSession.Character.CharacterId,
                    Type = SayColorType.Red,
                    Message = Game18NConstString.CantUseInGroup
                });
                return;
            }

            if ((requestData.ClientSession.Character.Level < 15) || (requestData.ClientSession.Character.JobLevel < 20))
            {
                await requestData.ClientSession.SendPacketAsync(new MsgiPacket
                {
                    Type = MessageType.Default,
                    Message = Game18NConstString.CanNotChangeJobAtThisLevel
                });

                await requestData.ClientSession.SendPacketAsync(new SayiPacket
                {
                    VisualType = VisualType.Player,
                    VisualId = requestData.ClientSession.Character.CharacterId,
                    Type = SayColorType.Yellow,
                    Message = Game18NConstString.CanNotChangeJobAtThisJobLevel
                });
                return;
            }

            var classType = (CharacterClassType)(requestData.Data.Item2.Type ?? 0);
            if (requestData.ClientSession.Character.Class == classType)
            {
                logger.Error(languageLocalizer[LogLanguageKey.CANT_CHANGE_SAME_CLASS]);
                return;
            }

            await requestData.ClientSession.Character.ChangeClassAsync(classType, worldConfiguration, experienceService, jobExperienceService, heroExperienceService);
        }
    }
}
