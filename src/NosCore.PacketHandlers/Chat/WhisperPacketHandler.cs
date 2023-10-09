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

using NosCore.Core;
using NosCore.Core.I18N;
using NosCore.Data.Enumerations.I18N;
using NosCore.Data.Enumerations.Interaction;
using NosCore.GameObject;
using NosCore.GameObject.ComponentEntities.Extensions;
using NosCore.GameObject.HttpClients.BlacklistHttpClient;
using NosCore.GameObject.Networking;
using NosCore.GameObject.Networking.ClientSession;
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
using NosCore.GameObject.InterChannelCommunication.Hubs.ChannelHub;
using NosCore.GameObject.InterChannelCommunication.Hubs.PubSub;
using NosCore.GameObject.InterChannelCommunication.Messages;
using Character = NosCore.Data.WebApi.Character;

namespace NosCore.PacketHandlers.Chat
{
    public class WhisperPacketHandler(ILogger logger, ISerializer packetSerializer,
            IBlacklistHttpClient blacklistHttpClient,
             IPubSubHub pubSubHub, Channel channel,
            IGameLanguageLocalizer gameLanguageLocalizer)
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
                })).ConfigureAwait(false);

                var speakPacket = session.Character.GenerateSpk(new SpeakPacket
                {
                    SpeakType = session.Account.Authority >= AuthorityType.GameMaster ? SpeakType.GameMaster
                        : SpeakType.Player,
                    Message = message.ToString()
                });

                var receiverSession =
                    Broadcaster.Instance.GetCharacter(s => s.Name == receiverName);

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

                var blacklisteds = await blacklistHttpClient.GetBlackListsAsync(session.Character.VisualId).ConfigureAwait(false);
                if (blacklisteds.Any(s => s.CharacterId == receiver.ConnectedCharacter?.Id))
                {
                    await session.SendPacketAsync(new InfoiPacket
                    {
                        Message = Game18NConstString.AlreadyBlacklisted
                    }).ConfigureAwait(false);
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
                }).ConfigureAwait(false);
            }
            catch (Exception e)
            {
                logger.Error("Whisper failed.", e);
            }
        }
    }
}