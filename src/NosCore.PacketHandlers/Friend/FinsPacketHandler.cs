//  __  _  __    __   ___ __  ___ ___
// |  \| |/__\ /' _/ / _//__\| _ \ __|
// | | ' | \/ |`._`.| \_| \/ | v / _|
// |_|\__|\__/ |___/ \__/\__/|_|_\___|
//

using NosCore.Data.Enumerations.I18N;
using NosCore.Data.WebApi;
using NosCore.GameObject.ComponentEntities.Extensions;
using NosCore.GameObject.Infastructure;
using NosCore.GameObject.InterChannelCommunication.Hubs.ChannelHub;
using NosCore.GameObject.InterChannelCommunication.Hubs.FriendHub;
using NosCore.GameObject.InterChannelCommunication.Hubs.PubSub;
using NosCore.GameObject.Networking.ClientSession;
using NosCore.GameObject.Services.BroadcastService;
using NosCore.Packets.ClientPackets.Relations;
using NosCore.Packets.Enumerations;
using NosCore.Packets.ServerPackets.UI;
using System;
using System.Threading.Tasks;

namespace NosCore.PacketHandlers.Friend
{
    public class FinsPacketHandler(IFriendHub friendHttpClient, IChannelHub channelHttpClient,
            IPubSubHub pubSubHub, ISessionRegistry sessionRegistry)
        : PacketHandler<FinsPacket>, IWorldPacketHandler
    {
        public override async Task ExecuteAsync(FinsPacket finsPacket, ClientSession session)
        {
            var targetCharacter = sessionRegistry.GetCharacter(s => s.VisualId == finsPacket.CharacterId);
            if (targetCharacter != null)
            {
                var result = await friendHttpClient.AddFriendAsync(new FriendShipRequest
                { CharacterId = session.Character.CharacterId, FinsPacket = finsPacket });

                switch (result)
                {
                    case LanguageKey.FRIENDLIST_FULL:
                        await session.Character.SendPacketAsync(new InfoiPacket
                        {
                            Message = Game18NConstString.MaxFriendReachedAdd
                        });
                        break;

                    case LanguageKey.BLACKLIST_BLOCKED:
                        await session.Character.SendPacketAsync(new InfoiPacket
                        {
                            Message = Game18NConstString.AlreadyBlacklisted
                        });
                        break;

                    case LanguageKey.ALREADY_FRIEND:
                        await session.Character.SendPacketAsync(new InfoiPacket
                        {
                            Message = Game18NConstString.RegisteredAsFriend
                        });
                        break;

                    case LanguageKey.FRIEND_REQUEST_BLOCKED:
                        await session.Character.SendPacketAsync(new Infoi2Packet
                        {
                            Message = Game18NConstString.HAsFriendRequestBlocked,
                            ArgumentType = 1,
                            Game18NArguments = { targetCharacter.Name! }
                        });
                        break;

                    case LanguageKey.FRIEND_REQUEST_SENT:
                        await targetCharacter.SendPacketAsync(new Dlgi2Packet
                        {
                            Question = Game18NConstString.AskBecomeFriend,
                            ArgumentType = 1,
                            Game18NArguments = { session.Character.Name! },
                            YesPacket = new FinsPacket
                            { Type = FinsPacketType.Accepted, CharacterId = session.Character.VisualId },
                            NoPacket = new FinsPacket
                            { Type = FinsPacketType.Rejected, CharacterId = session.Character.VisualId }
                        });
                        break;

                    case LanguageKey.FRIEND_ADDED:
                        await session.Character.SendPacketAsync(new InfoiPacket
                        {
                            Message = Game18NConstString.Registered
                        });
                        await targetCharacter.SendPacketAsync(new InfoiPacket
                        {
                            Message = Game18NConstString.Registered
                        });

                        await targetCharacter.SendPacketAsync(await targetCharacter.GenerateFinitAsync(friendHttpClient, channelHttpClient,
                            pubSubHub));
                        await session.Character.SendPacketAsync(await session.Character.GenerateFinitAsync(friendHttpClient,
                            channelHttpClient, pubSubHub));
                        break;

                    case LanguageKey.FRIEND_REJECTED:
                        await targetCharacter.SendPacketAsync(new InfoiPacket
                        {
                            Message = Game18NConstString.YouAreBlocked
                        });
                        break;

                    default:
                        throw new ArgumentException();
                }
            }
        }
    }
}
