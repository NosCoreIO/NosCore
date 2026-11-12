//  __  _  __    __   ___ __  ___ ___
// |  \| |/__\ /' _/ / _//__\| _ \ __|
// | | ' | \/ |`._`.| \_| \/ | v / _|
// |_|\__|\__/ |___/ \__/\__/|_|_\___|
//

using NosCore.Data.Enumerations.Items;
using NosCore.Data.CommandPackets;
using NosCore.Data.Enumerations;
using NosCore.Data.StaticEntities;
using NosCore.GameObject.Ecs.Extensions;
using NosCore.GameObject.Infastructure;
using NosCore.GameObject.Networking.ClientSession;
using NosCore.GameObject.Services.RemoteCharacterService;
using NosCore.Networking;
using NosCore.Packets;
using NosCore.Packets.Enumerations;
using NosCore.Packets.ServerPackets.Chats;
using NosCore.Packets.ServerPackets.UI;
using NosCore.Shared.Enumerations;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace NosCore.PacketHandlers.Command
{
    public class SetGoldCommandPacketHandler(IRemoteCharacterService remoteCharacterService, List<ItemDto> items)
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
                var goldName = items.First(i => i.Effect == ItemEffectType.Gold).Name[session.Account.Language];
#pragma warning disable NosCoreAnalyzers
                await session.SendPacketAsync(new Sayi2Packet
                {
                    VisualType = VisualType.Player,
                    VisualId = session.Character.CharacterId,
                    Type = SayColorType.Green,
                    Message = Game18NConstString.ItemReceived,
                    ArgumentType = 9,
                    Game18NArguments = new Game18NArguments(2) { goldPacket.Gold, goldName },
                });
#pragma warning restore NosCoreAnalyzers
                return;
            }

            if (!await remoteCharacterService.UpdateStatAsync(goldPacket.Name, UpdateStatActionType.UpdateGold, goldPacket.Gold))
            {
                await session.SendPacketAsync(new InfoiPacket
                {
                    Message = Game18NConstString.UnknownCharacter
                });
            }
        }
    }
}
