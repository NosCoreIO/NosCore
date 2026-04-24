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
using NosCore.Data.Enumerations;
using NosCore.Data.Enumerations.I18N;
using NosCore.GameObject.Ecs.Extensions;
using NosCore.GameObject.Services.BroadcastService;
using NosCore.Packets.Enumerations;
using NosCore.Packets.Interfaces;
using NosCore.Packets.ServerPackets.Player;
using NosCore.Packets.ServerPackets.Chats;
using NosCore.Packets.ServerPackets.UI;
using NosCore.Shared.Enumerations;
using NosCore.Shared.I18N;
using Microsoft.Extensions.Logging;
using System.Linq;
using System.Threading.Tasks;
using StatData = NosCore.GameObject.InterChannelCommunication.Messages.StatData;

namespace NosCore.GameObject.Services.ChannelCommunicationService.Handlers
{
    public class StatDataMessageChannelCommunicationMessageHandler(ILogger<StatDataMessageChannelCommunicationMessageHandler> logger,
        ILogLanguageLocalizer<LogLanguageKey> logLanguage, IOptions<WorldConfiguration> worldConfiguration, ISessionRegistry sessionRegistry,
        IExperienceService experienceService, IJobExperienceService jobExperienceService, IHeroExperienceService heroExperienceService) : ChannelCommunicationMessageHandler<StatData>
    {
        public override async Task Handle(StatData data)
        {
            var session = sessionRegistry.GetSession(s => s.Character.Name == data.Character?.Name);

            if (session == null)
            {
                return;
            }

            switch (data.ActionType)
            {
                case UpdateStatActionType.UpdateLevel:
                    {
                        var character = session.Character;
                        character.Level = (byte)data.Data;
                    }
                    break;
                case UpdateStatActionType.UpdateJobLevel:
                    {
                        var character = session.Character;
                        var jobLevel = (byte)data.Data;
                        character.JobLevel = (byte)((character.Class == CharacterClassType.Adventurer) && (jobLevel > 20) ? 20 : jobLevel);
                        character.JobLevelXp = 0;
                        var levPacket = session.Character.GenerateLev(experienceService, jobExperienceService, heroExperienceService);
                        await session.SendPacketAsync(levPacket);
                        await session.SendPacketAsync(new MsgiPacket
                        {
                            Type = MessageType.Default,
                            Message = Game18NConstString.JobLevelIncreased
                        });
                    }
                    break;
                case UpdateStatActionType.UpdateHeroLevel:
                    {
                        var character = session.Character;
                        character.HeroLevel = (byte)data.Data;
                        character.HeroLevelXp = 0;
                        IPacket statPacket = session.Character.GenerateStat();
                        IPacket statInfoPacket = session.Character.GenerateStatInfo();
                        IPacket levPacket = session.Character.GenerateLev(experienceService, jobExperienceService, heroExperienceService);
                        await session.SendPacketAsync(statPacket);
                        await session.SendPacketAsync(statInfoPacket);
                        await session.SendPacketAsync(levPacket);
                        await session.SendPacketAsync(new MsgiPacket
                        {
                            Type = MessageType.Default,
                            Message = Game18NConstString.HeroLevelIncreased
                        });
                    }
                    break;
                case UpdateStatActionType.UpdateReputation:
                    {
                        var character = session.Character;
                        character.Reputation = data.Data;
                        var fdPacket = session.Character.GenerateFd();
                        await session.SendPacketAsync(fdPacket);
                    }
                    break;
                case UpdateStatActionType.UpdateGold:
                    {
                        var character = session.Character;
                        if (character.Gold + data.Data > worldConfiguration.Value.MaxGoldAmount)
                        {
                            return;
                        }
                        character.Gold = data.Data;
                        var goldPacket = session.Character.GenerateGold();
                        await session.SendPacketAsync(goldPacket);
                    }
                    break;
                case UpdateStatActionType.UpdateClass:
                    {
                        var character = session.Character;
                        var classType = (CharacterClassType)data.Data;
                        if (character.InventoryService.Any(s => s.Value.Type == NoscorePocketType.Wear))
                        {
                            var characterId = session.Character.CharacterId;
                            await session.SendPacketAsync(new SayiPacket
                            {
                                VisualType = VisualType.Player,
                                VisualId = characterId,
                                Type = SayColorType.Yellow,
                                Message = Game18NConstString.RemoveEquipment
                            });
                            return;
                        }
                        character.JobLevel = 1;
                        character.JobLevelXp = 0;
                        await session.SendPacketAsync(new NpInfoPacket());
                        await session.SendPacketAsync(new PclearPacket());
                        character = session.Character;
                        if (classType == CharacterClassType.Adventurer)
                        {
                            character.HairStyle = character.HairStyle > HairStyleType.HairStyleB ? 0 : character.HairStyle;
                        }
                        character.Class = classType;
                        character.Hp = character.MaxHp;
                        character.Mp = character.MaxMp;
                        IPacket statPacket = session.Character.GenerateStat();
                        IPacket levPacket = session.Character.GenerateLev(experienceService, jobExperienceService, heroExperienceService);
                        await session.SendPacketAsync(statPacket);
                        await session.SendPacketAsync(levPacket);
                        await session.SendPacketAsync(new MsgiPacket
                        {
                            Type = MessageType.Default,
                            Message = Game18NConstString.ClassChanged
                        });
                    }
                    break;
                default:
                    logger.LogError(logLanguage[LogLanguageKey.UNKWNOWN_RECEIVERTYPE]);
                    break;
            }
        }
    }
}
