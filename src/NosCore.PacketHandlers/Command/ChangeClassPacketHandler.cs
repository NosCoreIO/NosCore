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
using NosCore.Data.CommandPackets;
using NosCore.Data.Enumerations;
using NosCore.GameObject.ComponentEntities.Extensions;
using NosCore.GameObject.Infastructure;
using NosCore.GameObject.InterChannelCommunication.Hubs.PubSub;
using NosCore.GameObject.InterChannelCommunication.Messages;
using NosCore.GameObject.Networking.ClientSession;
using NosCore.Packets.Enumerations;
using NosCore.Packets.ServerPackets.UI;
using System.Linq;
using System.Threading.Tasks;
using Character = NosCore.Data.WebApi.Character;

namespace NosCore.PacketHandlers.Command
{
    public class ChangeClassPacketHandler(IPubSubHub pubSubHub,
        IOptions<WorldConfiguration> worldConfiguration, IExperienceService experienceService,
        IJobExperienceService jobExperienceService, IHeroExperienceService heroExperienceService)
        : PacketHandler<ChangeClassPacket>, IWorldPacketHandler
    {
        public override async Task ExecuteAsync(ChangeClassPacket changeClassPacket, ClientSession session)
        {
            if ((changeClassPacket.Name == session.Character.Name) || string.IsNullOrEmpty(changeClassPacket.Name))
            {
                await session.Character.ChangeClassAsync(changeClassPacket.ClassType, worldConfiguration, experienceService, jobExperienceService, heroExperienceService);
                return;
            }

            var data = new StatData
            {
                ActionType = UpdateStatActionType.UpdateClass,
                Character = new Character { Name = changeClassPacket.Name },
                Data = (byte)changeClassPacket.ClassType
            };

            var accounts = await pubSubHub.GetSubscribersAsync();
            var receiver = accounts.FirstOrDefault(x => x.ConnectedCharacter?.Name == changeClassPacket.Name);

            if (receiver == null)
            {
                await session.SendPacketAsync(new InfoiPacket
                {
                    Message = Game18NConstString.UnknownCharacter
                });
                return;
            }

            await pubSubHub.SendMessageAsync(data);
        }
    }
}
