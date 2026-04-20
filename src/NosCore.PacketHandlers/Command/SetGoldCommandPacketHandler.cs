//  __  _  __    __   ___ __  ___ ___
// |  \| |/__\ /' _/ / _//__\| _ \ __|
// | | ' | \/ |`._`.| \_| \/ | v / _|
// |_|\__|\__/ |___/ \__/\__/|_|_\___|
//

using NosCore.Data.CommandPackets;
using NosCore.Data.Enumerations;
using NosCore.GameObject.Ecs.Extensions;
using NosCore.GameObject.Infastructure;
using NosCore.GameObject.InterChannelCommunication.Hubs.PubSub;
using NosCore.GameObject.InterChannelCommunication.Messages;
using NosCore.GameObject.Networking.ClientSession;
using NosCore.Packets.Enumerations;
using NosCore.Packets.ServerPackets.Chats;
using NosCore.Packets.ServerPackets.UI;
using NosCore.Shared.Enumerations;
using System.Linq;
using System.Threading.Tasks;
using Character = NosCore.Data.WebApi.Character;

namespace NosCore.PacketHandlers.Command
{
    public class SetGoldCommandPacketHandler(IPubSubHub pubSubHub)
        : PacketHandler<SetGoldCommandPacket>, IWorldPacketHandler
    {
        public override async Task ExecuteAsync(SetGoldCommandPacket goldPacket, ClientSession session)
        {
            // Self-targeting short-circuit (mirrors SetLevel/SetHeroLevel/SetReputation): when no
            // Name is given, or the Name matches us, mutate locally and skip the cross-channel
            // PubSub round-trip. The previous implementation always went through PubSub but used
            // raw `goldPacket.Name` (null) for the receiver lookup, which never matched and made
            // bare `$Gold N` fail with UnknownCharacter.
            if (string.IsNullOrEmpty(goldPacket.Name) || goldPacket.Name == session.Character.Name)
            {
                session.Character.Gold = goldPacket.Gold;
                await session.SendPacketAsync(session.Character.GenerateGold());
                await session.SendPacketAsync(new SayiPacket
                {
                    VisualType = VisualType.Player,
                    VisualId = session.Character.CharacterId,
                    Type = SayColorType.Green,
                    Message = Game18NConstString.GoldAward,
                    ArgumentType = 4,
                    Game18NArguments = { goldPacket.Gold },
                });
                return;
            }

            var data = new StatData
            {
                ActionType = UpdateStatActionType.UpdateGold,
                Character = new Character { Name = goldPacket.Name },
                Data = goldPacket.Gold
            };

            var accounts = await pubSubHub.GetSubscribersAsync();
            var receiver = accounts.FirstOrDefault(x => x.ConnectedCharacter?.Name == goldPacket.Name);

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
