//  __  _  __    __   ___ __  ___ ___
// |  \| |/__\ /' _/ / _//__\| _ \ __|
// | | ' | \/ |`._`.| \_| \/ | v / _|
// |_|\__|\__/ |___/ \__/\__/|_|_\___|
//

using System.Threading.Tasks;
using JetBrains.Annotations;
using Microsoft.Extensions.Options;
using NosCore.Algorithm.ExperienceService;
using NosCore.Algorithm.HeroExperienceService;
using NosCore.Algorithm.JobExperienceService;
using NosCore.Core.Configuration;
using NosCore.Data.Enumerations.I18N;
using NosCore.GameObject.Ecs.Extensions;
using NosCore.GameObject.Messaging.Events;
using NosCore.Packets.Enumerations;
using NosCore.Packets.ServerPackets.Chats;
using NosCore.Packets.ServerPackets.UI;
using NosCore.Shared.Enumerations;
using NosCore.Shared.I18N;
using Serilog;

namespace NosCore.GameObject.Messaging.Handlers.Nrun
{
    [UsedImplicitly]
    public sealed class ChangeClassHandler(
        ILogger logger,
        ILogLanguageLocalizer<LogLanguageKey> languageLocalizer,
        IOptions<WorldConfiguration> worldConfiguration,
        IExperienceService experienceService,
        IJobExperienceService jobExperienceService,
        IHeroExperienceService heroExperienceService,
        NosCore.GameObject.Services.ItemGenerationService.IItemGenerationService itemProvider)
    {
        [UsedImplicitly]
        public async Task Handle(NrunRequestedEvent evt)
        {
            if (evt.Packet.Runner != NrunRunnerType.ChangeClass
                || evt.Packet.Type is null or <= 0 or >= 4
                || evt.Target == null)
            {
                return;
            }

            var session = evt.ClientSession;
            if (session.Character.Class != (byte)CharacterClassType.Adventurer)
            {
                return;
            }

            if (!session.Character.Group!.IsEmpty)
            {
                await session.SendPacketAsync(new SayiPacket
                {
                    VisualType = VisualType.Player,
                    VisualId = session.Character.CharacterId,
                    Type = SayColorType.Red,
                    Message = Game18NConstString.CantUseInGroup
                });
                return;
            }

            if (session.Character.Level < 15 || session.Character.JobLevel < 20)
            {
                await session.SendPacketAsync(new MsgiPacket
                {
                    Type = MessageType.Default,
                    Message = Game18NConstString.CanNotChangeJobAtThisLevel
                });
                await session.SendPacketAsync(new SayiPacket
                {
                    VisualType = VisualType.Player,
                    VisualId = session.Character.CharacterId,
                    Type = SayColorType.Yellow,
                    Message = Game18NConstString.CanNotChangeJobAtThisJobLevel
                });
                return;
            }

            var classType = (CharacterClassType)(evt.Packet.Type ?? 0);
            if ((CharacterClassType)session.Character.Class == classType)
            {
                logger.Error(languageLocalizer[LogLanguageKey.CANT_CHANGE_SAME_CLASS]);
                return;
            }

            await session.ChangeClassAsync(classType, worldConfiguration, experienceService,
                jobExperienceService, heroExperienceService, itemProvider);
        }
    }
}
