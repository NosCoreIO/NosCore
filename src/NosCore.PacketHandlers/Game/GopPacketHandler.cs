//  __  _  __    __   ___ __  ___ ___
// |  \| |/__\ /' _/ / _//__\| _ \ __|
// | | ' | \/ |`._`.| \_| \/ | v / _|
// |_|\__|\__/ |___/ \__/\__/|_|_\___|
//

using Microsoft.Extensions.Logging;
using NosCore.Data.WebApi;
using NosCore.GameObject.Ecs.Extensions;
using NosCore.GameObject.Infastructure;
using NosCore.GameObject.InterChannelCommunication.Hubs.PubSub;
using NosCore.GameObject.Networking.ClientSession;
using NosCore.GameObject.Services.GroupService;
using NosCore.Packets.ClientPackets.UI;
using NosCore.Packets.Enumerations;
using NosCore.Packets.ServerPackets.UI;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace NosCore.PacketHandlers.Game
{
    public class GopPacketHandler(IPubSubHub pubSubHub, IGroupLootService groupLootService, ILogger<GopPacketHandler> logger)
        : PacketHandler<GopPacket>, IWorldPacketHandler
    {
        // The client ticks these to accept, so they are stored inverted as "blocked".
        private static readonly HashSet<GopPacketType> TickedToAccept =
        [
            GopPacketType.ExchangeBlocked,
            GopPacketType.FriendRequestBlocked,
            GopPacketType.FamilyRequestBlocked,
            GopPacketType.WhisperBlocked,
            GopPacketType.GroupRequestBlocked
        ];

        private static readonly Dictionary<GopPacketType, (Game18NConstString WhenSet, Game18NConstString WhenCleared)> Messages = new()
        {
            [GopPacketType.ExchangeBlocked] = (Game18NConstString.TradeBlocked, Game18NConstString.TradeAllowed),
            [GopPacketType.FriendRequestBlocked] = (Game18NConstString.FriendBlocked, Game18NConstString.FriendAllowed),
            [GopPacketType.FamilyRequestBlocked] = (Game18NConstString.FamilyBlocked, Game18NConstString.FamilyAllowed),
            [GopPacketType.WhisperBlocked] = (Game18NConstString.WhisperBlocked, Game18NConstString.WhisperAllowed),
            [GopPacketType.GroupRequestBlocked] = (Game18NConstString.GroupRequestBlocked, Game18NConstString.GroupRequestAllowed),
            [GopPacketType.PetAutoRelive] = (Game18NConstString.PetWillReviveAfterDeath, Game18NConstString.PetWillReturnMinilandAfterDeath),
            [GopPacketType.PartnerAutoRelive] = (Game18NConstString.PartnerWillReviveAfterDeath, Game18NConstString.PartnerWillReturnMinilandAfterDeath),
            [GopPacketType.HeroChatBlocked] = (Game18NConstString.SpeakerDisabled, Game18NConstString.SpeakerEnabled),
            [GopPacketType.QuickGetUp] = (Game18NConstString.ClickToGetUp, Game18NConstString.FastGetUpDisabled),
            [GopPacketType.EmoticonsBlocked] = (Game18NConstString.SmileysDisabled, Game18NConstString.SmileysEnabled),
            [GopPacketType.HpBlocked] = (Game18NConstString.HpStatusDisabled, Game18NConstString.HpStatusEnabled),
            [GopPacketType.BuffBlocked] = (Game18NConstString.BuffSequenceHidden, Game18NConstString.BuffSequenceShown),
            [GopPacketType.MinilandInviteBlocked] = (Game18NConstString.MinilandInviteBlocked, Game18NConstString.MinilandInvitePermitted),
        };

        public override async Task ExecuteAsync(GopPacket packet, ClientSession session)
        {
            if (!Enum.IsDefined(packet.Option))
            {
                return;
            }

            var character = session.Character;
            if (packet.Option == GopPacketType.GroupSharing)
            {
                await groupLootService.ChangeSharingAsync(character, packet.IsActive);
                await session.SendPacketAsync(character.GenerateStat());
                return;
            }

            var value = TickedToAccept.Contains(packet.Option) ? !packet.IsActive : packet.IsActive;
            character.SetOption(packet.Option, value);

            // Friend requests and whispers are checked on the master server, so it needs the new state.
            if (packet.Option is GopPacketType.FriendRequestBlocked or GopPacketType.WhisperBlocked)
            {
                var published = await pubSubHub.SubscribeAsync(new Subscriber
                {
                    Id = session.SessionId,
                    Name = session.Account.Name,
                    Language = session.Account.Language,
                    ConnectedCharacter = new Character
                    {
                        Id = character.CharacterId,
                        Name = character.Name,
                        FriendRequestBlocked = character.FriendRequestBlocked,
                        WhisperBlocked = character.WhisperBlocked
                    }
                });
                if (!published)
                {
                    logger.LogWarning("The master server rejected the {Option} option of {CharacterId}", packet.Option, character.CharacterId);
                }
            }

            if (Messages.TryGetValue(packet.Option, out var message))
            {
                await session.SendPacketAsync(new MsgiPacket
                {
                    Type = MessageType.Default,
                    Message = value ? message.WhenSet : message.WhenCleared
                });
            }

            await session.SendPacketAsync(character.GenerateStat());
        }
    }
}
