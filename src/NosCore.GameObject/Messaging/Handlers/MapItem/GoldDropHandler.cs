//  __  _  __    __   ___ __  ___ ___
// |  \| |/__\ /' _/ / _//__\| _ \ __|
// | | ' | \/ |`._`.| \_| \/ | v / _|
// |_|\__|\__/ |___/ \__/\__/|_|_\___|
//

using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using JetBrains.Annotations;
using Microsoft.Extensions.Options;
using NosCore.Core.Configuration;
using NosCore.Data.StaticEntities;
using NosCore.GameObject.Ecs.Extensions;
using NosCore.GameObject.Messaging.Events;
using NosCore.Networking;
using NosCore.Packets;
using NosCore.Packets.Enumerations;
using NosCore.Packets.ServerPackets.Battle;
using NosCore.Packets.ServerPackets.Chats;
using NosCore.Packets.ServerPackets.UI;
using NosCore.Shared.Enumerations;

namespace NosCore.GameObject.Messaging.Handlers.MapItem
{
    [UsedImplicitly]
    public sealed class GoldDropHandler(IOptions<WorldConfiguration> worldConfiguration, List<ItemDto> items)
    {
        [UsedImplicitly]
        public async Task Handle(MapItemPickedUpEvent evt)
        {
            if (evt.VNum != 1046)
            {
                return;
            }

            var session = evt.ClientSession;
            var maxGold = worldConfiguration.Value.MaxGoldAmount;
            var character = session.Character;
            var visualId = evt.VisualId;
            var amount = evt.Amount;
            var vnum = evt.VNum;

            if (character.Gold + amount <= maxGold)
            {
                if (evt.Packet.PickerType == VisualType.Npc)
                {
                    await session.SendPacketAsync(character.GenerateIcon(1, vnum));
                }

                character = session.Character;
                character.Gold += amount;

                var goldName = items.First(i => i.VNum == vnum).Name[session.Account.Language];
#pragma warning disable NosCoreAnalyzers
                var characterId = character.CharacterId;
                await session.SendPacketAsync(new Sayi2Packet
                {
                    VisualType = VisualType.Player,
                    VisualId = characterId,
                    Type = SayColorType.Green,
                    Message = Game18NConstString.ItemReceived,
                    ArgumentType = 9,
                    Game18NArguments = new Game18NArguments(2) { amount, goldName }
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
            mapInstance.TryRemoveMapItem(visualId);
            await mapInstance.SendPacketAsync(character.GenerateGet(visualId));
            await session.SendPacketAsync(new CancelPacket
            {
                Type = CancelPacketType.CancelPicking,
                TargetId = visualId,
                Unknow = -1
            });
        }
    }
}
