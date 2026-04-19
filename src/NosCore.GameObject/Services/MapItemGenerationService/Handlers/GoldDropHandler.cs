//  __  _  __    __   ___ __  ___ ___
// |  \| |/__\ /' _/ / _//__\| _ \ __|
// | | ' | \/ |`._`.| \_| \/ | v / _|
// |_|\__|\__/ |___/ \__/\__/|_|_\___|
//

using Microsoft.Extensions.Options;
using NosCore.Core.Configuration;
using NosCore.GameObject.ComponentEntities.Entities;
using NosCore.GameObject.Ecs.Extensions;
using NosCore.GameObject.Networking.ClientSession;
using NosCore.Networking;
using NosCore.Packets.ClientPackets.Drops;
using NosCore.Packets.Enumerations;
using NosCore.Packets.ServerPackets.Chats;
using NosCore.Packets.ServerPackets.UI;
using NosCore.Shared.Enumerations;
using System;
using System.Threading.Tasks;

namespace NosCore.GameObject.Services.MapItemGenerationService.Handlers
{
    public class GoldDropEventHandler(IOptions<WorldConfiguration> worldConfiguration) : IGetMapItemEventHandler
    {
        public bool Condition(MapItem item)
        {
            return item.VNum == 1046;
        }

        public async Task ExecuteAsync(RequestData<Tuple<MapItem, GetPacket>> requestData)
        {
            var session = requestData.ClientSession;
            var mapItem = requestData.Data.Item1;
            var packet = requestData.Data.Item2;
            var maxGold = worldConfiguration.Value.MaxGoldAmount;

            var character = session.Character;
            if (character.Gold + mapItem.Amount <= maxGold)
            {
                if (packet.PickerType == VisualType.Npc)
                {
                    var iconPacket = character.GenerateIcon(1, mapItem.VNum);
                    await session.SendPacketAsync(iconPacket);
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
                    Game18NArguments = { mapItem.Amount, mapItem.ItemInstance!.Item.Name[session.Account.Language] }
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
            var goldPacket = character.GenerateGold();
            var mapInstance = character.MapInstance;
            var getPacket = character.GenerateGet(mapItem.VisualId);

            await session.SendPacketAsync(goldPacket);
            mapInstance.MapItems.TryRemove(mapItem.VisualId, out _);
            await mapInstance.SendPacketAsync(getPacket);
        }
    }
}
