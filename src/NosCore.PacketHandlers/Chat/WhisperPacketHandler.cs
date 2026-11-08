//  __  _  __    __   ___ __  ___ ___
// |  \| |/__\ /' _/ / _//__\| _ \ __|
// | | ' | \/ |`._`.| \_| \/ | v / _|
// |_|\__|\__/ |___/ \__/\__/|_|_\___|
//

using NosCore.Core;
using NosCore.Core.I18N;
using NosCore.Data.Enumerations.I18N;
using NosCore.Data.Enumerations.Interaction;
using NosCore.GameObject.Ecs.Extensions;
using NosCore.GameObject.Infastructure;
using NosCore.GameObject.InterChannelCommunication.Hubs.PubSub;
using NosCore.GameObject.InterChannelCommunication.Messages;
using NosCore.GameObject.Networking.ClientSession;
using NosCore.GameObject.Services.BlackListService;
using NosCore.GameObject.Services.BroadcastService;
using NosCore.GameObject.Services.SubscriberCacheService;
using NosCore.Packets.ClientPackets.Chat;
using NosCore.Packets.Enumerations;
using NosCore.Packets.Interfaces;
using NosCore.Packets.ServerPackets.Chats;
using NosCore.Packets.ServerPackets.UI;
using NosCore.Shared.Enumerations;
using Microsoft.Extensions.Logging;
using System;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Character = NosCore.Data.WebApi.Character;

namespace NosCore.PacketHandlers.Chat
{
    public class WhisperPacketHandler(ILogger<WhisperPacketHandler> logger, ISerializer packetSerializer,
            IBlacklistCacheService blacklistCache, ISubscriberCacheService subscriberCache,
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

                var accounts = await subscriberCache.GetSubscribersAsync();
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

                var receiverId = receiver.ConnectedCharacter?.Id;
                var blacklisteds = await blacklistCache.GetBlacklistedAsync(session.Character.VisualId);
                if (receiverId != null && (blacklisteds.Any(s => s.CharacterId == receiverId)
                    || (await blacklistCache.GetBlacklistedAsync(receiverId.Value)).Any(s => s.CharacterId == session.Character.VisualId)))
                {
                    await session.SendPacketAsync(new InfoiPacket
                    {
                        Message = Game18NConstString.AlreadyBlacklisted
                    });
                    return;
                }

                if (receiver.ConnectedCharacter?.WhisperBlocked == true)
                {
                    await session.SendPacketAsync(new MsgiPacket
                    {
                        Type = MessageType.Default,
                        Message = Game18NConstString.ThisPersonRefuseWhisper
                    });
                    return;
                }

                var receiverOnSameChannel = sessionRegistry.TryGetCharacter(s => s.Name == receiverName, out _);
                speakPacket.Message = receiverOnSameChannel ? speakPacket.Message :
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
                logger.LogError(e, "Whisper failed.");
            }
        }
    }
}
