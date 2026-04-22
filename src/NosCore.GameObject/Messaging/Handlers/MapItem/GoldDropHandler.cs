//  __  _  __    __   ___ __  ___ ___
// |  \| |/__\ /' _/ / _//__\| _ \ __|
// | | ' | \/ |`._`.| \_| \/ | v / _|
// |_|\__|\__/ |___/ \__/\__/|_|_\___|
//

using System.Threading.Tasks;
using JetBrains.Annotations;
using Microsoft.Extensions.Options;
using NosCore.Core.Configuration;
using NosCore.GameObject.Ecs.Extensions;
using NosCore.GameObject.Messaging.Events;
using NosCore.Networking;
using NosCore.Packets.Enumerations;
using NosCore.Packets.ServerPackets.Chats;
using NosCore.Packets.ServerPackets.UI;
using NosCore.Shared.Enumerations;

namespace NosCore.GameObject.Messaging.Handlers.MapItem
{
    [UsedImplicitly]
    public sealed class GoldDropHandler(IOptions<WorldConfiguration> worldConfiguration)
    {
        [UsedImplicitly]
        public async Task Handle(MapItemPickedUpEvent evt)
        {
            if (evt.MapItem.VNum != 1046)
            {
                return;
            }

            var session = evt.ClientSession;
            var mapItem = evt.MapItem;
            var maxGold = worldConfiguration.Value.MaxGoldAmount;

            var character = session.Character;
            if (character.Gold + mapItem.Amount <= maxGold)
            {
                if (evt.Packet.PickerType == VisualType.Npc)
                {
                    await session.SendPacketAsync(character.GenerateIcon(1, mapItem.VNum));
                }

                character = session.Character;
                character.Gold += mapItem.Amount;

#pragma warning disable NosCoreAnalyzers
                var characterId = character.CharacterId;
                await session.SendPacketAsync(new Sayi2Packet
                {
                    VisualType = VisualType.Player,
                    VisualId = characterId,
                    Type = SayColorType.Green,
                    Message = Game18NConstString.ItemReceived,
                    ArgumentType = 9,
                    Game18NArguments = { mapItem.Amount, mapItem.VNum }
                });
#pragma warning restore NosCoreAnalyzers
            }
            else
            {
                character = session.Character;
                character.Gold = maxGold;
                await session.SendPacketAsync(new MsgiPacket
                {
                    Type = MessageType.Default,
                    Message = Game18NConstString.MaxGoldReached
                });
            }

            character = session.Character;
            var mapInstance = character.MapInstance;
            await session.SendPacketAsync(character.GenerateGold());
            mapInstance.TryRemoveMapItem(mapItem.VisualId);
            await mapInstance.SendPacketAsync(character.GenerateGet(mapItem.VisualId));
        }
    }
}
