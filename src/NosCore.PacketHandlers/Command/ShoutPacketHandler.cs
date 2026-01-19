//  __  _  __    __   ___ __  ___ ___
// |  \| |/__\ /' _/ / _//__\| _ \ __|
// | | ' | \/ |`._`.| \_| \/ | v / _|
// |_|\__|\__/ |___/ \__/\__/|_|_\___|
//

using NosCore.Core.I18N;
using NosCore.Data.CommandPackets;
using NosCore.Data.Enumerations.I18N;
using NosCore.Data.Enumerations.Interaction;
using NosCore.GameObject.Infastructure;
using NosCore.GameObject.InterChannelCommunication.Hubs.PubSub;
using NosCore.GameObject.InterChannelCommunication.Messages;
using NosCore.GameObject.Networking.ClientSession;
using NosCore.Packets.Enumerations;
using NosCore.Packets.Interfaces;
using NosCore.Packets.ServerPackets.Chats;
using NosCore.Packets.ServerPackets.UI;
using NosCore.Shared.Enumerations;
using System.Collections.Generic;
using System.Threading.Tasks;
using Character = NosCore.Data.WebApi.Character;

//TODO stop using obsolete
#pragma warning disable 618

namespace NosCore.PacketHandlers.Command
{
    public class ShoutPacketHandler(ISerializer packetSerializer, IPubSubHub packetHttpClient,
            IGameLanguageLocalizer gameLanguageLocalizer)
        : PacketHandler<ShoutPacket>, IWorldPacketHandler
    {
        public override async Task ExecuteAsync(ShoutPacket shoutPacket, ClientSession session)
        {
            var message = $"({gameLanguageLocalizer[LanguageKey.ADMINISTRATOR, session.Account.Language]}) {shoutPacket.Message}";

            var sayPacket = new Sayi2Packet
            {
                VisualType = VisualType.Player,
                VisualId = -1,
                Type = SayColorType.Yellow,
                Message = Game18NConstString.Administrator,
                Game18NArguments = { 999, message }
            };

            var msgiPacket = new MsgPacket
            {
                Type = MessageType.Shout,
                Message = message
            };

            var sayPostedPacket = new PostedPacket
            {
                Packet = packetSerializer.Serialize(new[] { sayPacket }),
                SenderCharacter = new Character
                {
                    Name = session.Character.Name,
                    Id = session.Character.CharacterId
                },
                ReceiverType = ReceiverType.All
            };

            var msgPostedPacket = new PostedPacket
            {
                Packet = packetSerializer.Serialize(new[] { msgiPacket }),
                ReceiverType = ReceiverType.All
            };

            await packetHttpClient.SendMessagesAsync(new List<IMessage>(new[] { sayPostedPacket, msgPostedPacket }));
        }
    }
}
