//  __  _  __    __   ___ __  ___ ___
// |  \| |/__\ /' _/ / _//__\| _ \ __|
// | | ' | \/ |`._`.| \_| \/ | v / _|
// |_|\__|\__/ |___/ \__/\__/|_|_\___|
// 
// Copyright (C) 2019 - NosCore
// 
// NosCore is a free software: you can redistribute it and/or modify
// it under the terms of the GNU General Public License as published by
// the Free Software Foundation, either version 3 of the License, or any later version.
// 
// This program is distributed in the hope that it will be useful,
// but WITHOUT ANY WARRANTY; without even the implied warranty of
// MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
// GNU General Public License for more details.
// 
// You should have received a copy of the GNU General Public License
// along with this program.  If not, see <http://www.gnu.org/licenses/>.

using System;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using NosCore.Core.HttpClients.ConnectedAccountHttpClients;
using NosCore.Packets.ClientPackets.Chat;
using NosCore.Packets.Enumerations;
using NosCore.Packets.Interfaces;
using NosCore.Packets.ServerPackets.Chats;
using NosCore.Core.I18N;
using NosCore.Core.Networking;
using NosCore.Data.Enumerations.I18N;
using NosCore.Data.Enumerations.Interaction;
using NosCore.Data.WebApi;
using NosCore.GameObject;
using NosCore.GameObject.ComponentEntities.Extensions;
using NosCore.GameObject.HttpClients.BlacklistHttpClient;
using NosCore.GameObject.HttpClients.PacketHttpClient;
using NosCore.GameObject.Networking;
using NosCore.GameObject.Networking.ClientSession;
using NosCore.Shared.Enumerations;
using Serilog;
using Character = NosCore.Data.WebApi.Character;

namespace NosCore.PacketHandlers.Chat
{
    public class WhisperPacketHandler : PacketHandler<WhisperPacket>, IWorldPacketHandler
    {
        private readonly IBlacklistHttpClient _blacklistHttpClient;
        private readonly IConnectedAccountHttpClient _connectedAccountHttpClient;
        private readonly ILogger _logger;
        private readonly IPacketHttpClient _packetHttpClient;
        private readonly ISerializer _packetSerializer;

        public WhisperPacketHandler(ILogger logger, ISerializer packetSerializer,
            IBlacklistHttpClient blacklistHttpClient,
            IConnectedAccountHttpClient connectedAccountHttpClient, IPacketHttpClient packetHttpClient)
        {
            _logger = logger;
            _packetSerializer = packetSerializer;
            _blacklistHttpClient = blacklistHttpClient;
            _connectedAccountHttpClient = connectedAccountHttpClient;
            _packetHttpClient = packetHttpClient;
        }

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
                })).ConfigureAwait(false);

                var speakPacket = session.Character.GenerateSpk(new SpeakPacket
                {
                    SpeakType = session.Account.Authority >= AuthorityType.GameMaster ? SpeakType.GameMaster
                        : SpeakType.Player,
                    Message = message.ToString()
                });

                var receiverSession =
                    Broadcaster.Instance.GetCharacter(s => s.Name == receiverName);

                var receiver = await _connectedAccountHttpClient.GetCharacterAsync(null, receiverName).ConfigureAwait(false);

                if (receiver.Item2 == null) //TODO: Handle 404 in WebApi
                {
                    await session.SendPacketAsync(session.Character.GenerateSay(
                        GameLanguage.Instance.GetMessageFromKey(LanguageKey.CHARACTER_OFFLINE, session.Account.Language),
                        SayColorType.Yellow)).ConfigureAwait(false);
                    return;
                }

                var blacklisteds = await _blacklistHttpClient.GetBlackListsAsync(session.Character.VisualId).ConfigureAwait(false);
                if (blacklisteds.Any(s => s.CharacterId == receiver.Item2.ConnectedCharacter?.Id))
                {
                    await session.SendPacketAsync(new SayPacket
                    {
                        Message = GameLanguage.Instance.GetMessageFromKey(LanguageKey.BLACKLIST_BLOCKED,
                            session.Account.Language),
                        Type = SayColorType.Yellow
                    }).ConfigureAwait(false);
                    return;
                }

                speakPacket.Message = receiverSession != null ? speakPacket.Message :
                    $"{speakPacket.Message} <{GameLanguage.Instance.GetMessageFromKey(LanguageKey.CHANNEL, receiver.Item2.Language)}: {MasterClientListSingleton.Instance.ChannelId}>";

                await _packetHttpClient.BroadcastPacketAsync(new PostedPacket
                {
                    Packet = _packetSerializer.Serialize(new[] {speakPacket}),
                    ReceiverCharacter = new Character {Name = receiverName},
                    SenderCharacter = new Character {Name = session.Character.Name},
                    OriginWorldId = MasterClientListSingleton.Instance.ChannelId,
                    ReceiverType = ReceiverType.OnlySomeone
                }, receiver.Item2.ChannelId).ConfigureAwait(false);

                await session.SendPacketAsync(session.Character.GenerateSay(
                    GameLanguage.Instance.GetMessageFromKey(LanguageKey.SEND_MESSAGE_TO_CHARACTER,
                        session.Account.Language), SayColorType.Purple)).ConfigureAwait(false);
            }
            catch (Exception e)
            {
                _logger.Error("Whisper failed.", e);
            }
        }
    }
}