//  __  _  __    __   ___ __  ___ ___
// |  \| |/__\ /' _/ / _//__\| _ \ __|
// | | ' | \/ |`._`.| \_| \/ | v / _|
// |_|\__|\__/ |___/ \__/\__/|_|_\___|
//

using Microsoft.Extensions.Options;
using NosCore.Core.Configuration;
using NosCore.GameObject.ComponentEntities.Entities;
using NosCore.GameObject.ComponentEntities.Extensions;
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
            // handle gold drop
            var maxGold = worldConfiguration.Value.MaxGoldAmount;
            if (requestData.ClientSession.Character.Gold + requestData.Data.Item1.Amount <= maxGold)
            {
                if (requestData.Data.Item2.PickerType == VisualType.Npc)
                {
                    await requestData.ClientSession.SendPacketAsync(
                        requestData.ClientSession.Character.GenerateIcon(1, requestData.Data.Item1.VNum));
                }

                requestData.ClientSession.Character.Gold += requestData.Data.Item1.Amount;

#pragma warning disable NosCoreAnalyzers // For some reason this packet doesn't have the right amount of arguments
                await requestData.ClientSession.SendPacketAsync(new Sayi2Packet
                {
                    VisualType = VisualType.Player,
                    VisualId = requestData.ClientSession.Character.CharacterId,
                    Type = SayColorType.Green,
                    Message = Game18NConstString.ItemReceived,
                    ArgumentType = 9,
                    Game18NArguments = { requestData.Data.Item1.Amount, requestData.Data.Item1.ItemInstance!.Item.Name[requestData.ClientSession.Account.Language] }
                });
#pragma warning restore NosCoreAnalyzers
            }
            else
            {
                requestData.ClientSession.Character.Gold = maxGold;
                await requestData.ClientSession.SendPacketAsync(new MsgiPacket
                {
                    Type = MessageType.Default,
                    Message = Game18NConstString.MaxGoldReached
                });
            }

            await requestData.ClientSession.SendPacketAsync(requestData.ClientSession.Character.GenerateGold());
            requestData.ClientSession.Character.MapInstance.MapItems.TryRemove(requestData.Data.Item1.VisualId, out _);
            await requestData.ClientSession.Character.MapInstance.SendPacketAsync(
                requestData.ClientSession.Character.GenerateGet(requestData.Data.Item1.VisualId));
        }
    }
}
