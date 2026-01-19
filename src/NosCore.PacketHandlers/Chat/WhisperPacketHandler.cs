//  __  _  __    __   ___ __  ___ ___
// |  \| |/__\ /' _/ / _//__\| _ \ __|
// | | ' | \/ |`._`.| \_| \/ | v / _|
// |_|\__|\__/ |___/ \__/\__/|_|_\___|
//

using NosCore.Core;
using NosCore.Core.I18N;
using NosCore.Data.Enumerations.I18N;
using NosCore.Data.Enumerations.Interaction;
using NosCore.GameObject.ComponentEntities.Extensions;
using NosCore.GameObject.Infastructure;
using NosCore.GameObject.InterChannelCommunication.Hubs.BlacklistHub;
using NosCore.GameObject.InterChannelCommunication.Hubs.PubSub;
using NosCore.GameObject.InterChannelCommunication.Messages;
using NosCore.GameObject.Networking.ClientSession;
using NosCore.GameObject.Services.BroadcastService;
using NosCore.Packets.ClientPackets.Chat;
using NosCore.Packets.Enumerations;
using NosCore.Packets.Interfaces;
using NosCore.Packets.ServerPackets.Chats;
using NosCore.Packets.ServerPackets.UI;
using NosCore.Shared.Enumerations;
using Serilog;
using System;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Character = NosCore.Data.WebApi.Character;

namespace NosCore.PacketHandlers.Chat
{
    public class WhisperPacketHandler(ILogger logger, ISerializer packetSerializer,
            IBlacklistHub blacklistHttpClient,
             IPubSubHub pubSubHub, Channel channel,
            IGameLanguageLocalizer gameLanguageLocalizer,
            ISessionRegistry sessionRegistry)
        : PacketHandler<WhisperPacket>, IWorldPacketHandler
    {
        public override async Task ExecuteAsync(WhisperPacket whisperPacket, ClientSession session)
        {
            try
            {
                var messageBuilder = new StringBuilder();

                //Todo: review this
                var messageData = whisperPacket.Message!.Split(' ');
                var receiverName = messageData[whisperPacket.Message.StartsWith("GM ") ? 1 : 0];

                for (var i = messageData[0] == "GM" ? 2 : 1; i < messageData.Length; i++)
                {
                    messageBuilder.Append(messageData[i]).Append(" ");
                }

                var message = new StringBuilder(messageBuilder.ToString().Length > 60
                    ? messageBuilder.ToString().Substring(0, 60) : messageBuilder.ToString());

                await session.SendPacketAsync(session.Character.GenerateSpk(new SpeakPacket
                {
                    SpeakType = SpeakType.Player,
                    Message = message.ToString()
                }));

                var speakPacket = session.Character.GenerateSpk(new SpeakPacket
                {
                    SpeakType = session.Account.Authority >= AuthorityType.GameMaster ? SpeakType.GameMaster
                        : SpeakType.Player,
                    Message = message.ToString()
                });

                var receiverSession =
                    sessionRegistry.GetCharacter(s => s.Name == receiverName);

                var accounts = await pubSubHub.GetSubscribersAsync();
                var receiver = accounts.FirstOrDefault(x => x.ConnectedCharacter?.Name == receiverName);

                if (receiver == null)
                {
                    await session.SendPacketAsync(new Infoi2Packet
                    {
                        Message = Game18NConstString.IsNotPlaying,
                        ArgumentType = 1,
                        Game18NArguments = { receiverName }
                    });
                    return;
                }

                var blacklisteds = await blacklistHttpClient.GetBlacklistedAsync(session.Character.VisualId);
                if (blacklisteds.Any(s => s.CharacterId == receiver.ConnectedCharacter?.Id))
                {
                    await session.SendPacketAsync(new InfoiPacket
                    {
                        Message = Game18NConstString.AlreadyBlacklisted
                    });
                    return;
                }

                speakPacket.Message = receiverSession != null ? speakPacket.Message :
                    $"{speakPacket.Message} <{gameLanguageLocalizer[LanguageKey.CHANNEL, receiver.Language]}: {channel.ChannelId}>";

                await pubSubHub.SendMessageAsync(new PostedPacket
                {
                    Packet = packetSerializer.Serialize(new[] { speakPacket }),
                    ReceiverCharacter = new Character { Name = receiverName },
                    SenderCharacter = new Character { Name = session.Character.Name },
                    OriginWorldId = channel.ChannelId,
                    ReceiverType = ReceiverType.OnlySomeone
                });
            }
            catch (Exception e)
            {
                logger.Error("Whisper failed.", e);
            }
        }
    }
}
