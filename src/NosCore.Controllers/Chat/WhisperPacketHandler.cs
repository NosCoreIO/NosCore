using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using ChickenAPI.Packets.ClientPackets.Chat;
using ChickenAPI.Packets.Enumerations;
using ChickenAPI.Packets.Interfaces;
using ChickenAPI.Packets.ServerPackets.Chats;
using ChickenAPI.Packets.ServerPackets.UI;
using NosCore.Core;
using NosCore.Core.I18N;
using NosCore.Core.Networking;
using NosCore.Data.Enumerations;
using NosCore.Data.Enumerations.Account;
using NosCore.Data.Enumerations.I18N;
using NosCore.Data.Enumerations.Interaction;
using NosCore.Data.WebApi;
using NosCore.GameObject;
using NosCore.GameObject.ComponentEntities.Extensions;
using NosCore.GameObject.Networking;
using NosCore.GameObject.Networking.ClientSession;
using Serilog;

namespace NosCore.PacketHandlers.Chat
{
    public class WhisperPacketHandler : PacketHandler<WhisperPacket>, IWorldPacketHandler
    {
        private readonly ILogger _logger;
        private readonly ISerializer _packetSerializer;
        private readonly IWebApiAccess _webApiAccess;
        public WhisperPacketHandler(ILogger logger, ISerializer packetSerializer, IWebApiAccess webApiAccess)
        {
            _logger = logger;
            _packetSerializer = packetSerializer;
            _webApiAccess = webApiAccess;
        }

        public override void Execute(WhisperPacket whisperPacket, ClientSession session)
        {
            try
            {
                var messageBuilder = new StringBuilder();

                //Todo: review this
                var messageData = whisperPacket.Message.Split(' ');
                var receiverName = messageData[whisperPacket.Message.StartsWith("GM ") ? 1 : 0];

                for (var i = messageData[0] == "GM" ? 2 : 1; i < messageData.Length; i++)
                {
                    messageBuilder.Append(messageData[i]).Append(" ");
                }

                var message = new StringBuilder(messageBuilder.ToString().Length > 60
                    ? messageBuilder.ToString().Substring(0, 60) : messageBuilder.ToString());

                session.SendPacket(session.Character.GenerateSpk(new SpeakPacket
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
                    Broadcaster.Instance.GetCharacter(s => s.Name == receiverName);

                var receiver = _webApiAccess.GetCharacter(null, receiverName);

                if (receiver.Item2 == null) //TODO: Handle 404 in WebApi
                {
                    session.SendPacket(session.Character.GenerateSay(
                        Language.Instance.GetMessageFromKey(LanguageKey.CHARACTER_OFFLINE, session.Account.Language),
                        SayColorType.Yellow));
                    return;
                }

                var blacklisteds = _webApiAccess.Get<List<CharacterRelation>>(WebApiRoute.Blacklist, session.Character.VisualId) ?? new List<CharacterRelation>();
                if (blacklisteds.Any(s => s.RelatedCharacterId == receiver.Item2.ConnectedCharacter.Id))
                {
                    session.SendPacket(new SayPacket
                    {
                        Message = Language.Instance.GetMessageFromKey(LanguageKey.BLACKLIST_BLOCKED,
                                session.Account.Language),
                        Type = SayColorType.Yellow
                    });
                    return;
                }

                speakPacket.Message = receiverSession != null ? speakPacket.Message :
                    $"{speakPacket.Message} <{Language.Instance.GetMessageFromKey(LanguageKey.CHANNEL, receiver.Item2.Language)}: {MasterClientListSingleton.Instance.ChannelId}>";

                _webApiAccess.BroadcastPacket(new PostedPacket
                {
                    Packet = _packetSerializer.Serialize(new[] { speakPacket }),
                    ReceiverCharacter = new Data.WebApi.Character { Name = receiverName },
                    SenderCharacter = new Data.WebApi.Character { Name = session.Character.Name },
                    OriginWorldId = MasterClientListSingleton.Instance.ChannelId,
                    ReceiverType = ReceiverType.OnlySomeone
                }, receiver.Item2.ChannelId);

                session.SendPacket(session.Character.GenerateSay(
                    Language.Instance.GetMessageFromKey(LanguageKey.SEND_MESSAGE_TO_CHARACTER,
                        session.Account.Language), SayColorType.Purple));
            }
            catch (Exception e)
            {
                _logger.Error("Whisper failed.", e);
            }

        }
    }
}
