//  __  _  __    __   ___ __  ___ ___
// |  \| |/__\ /' _/ / _//__\| _ \ __|
// | | ' | \/ |`._`.| \_| \/ | v / _|
// |_|\__|\__/ |___/ \__/\__/|_|_\___|
//


using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using NosCore.Data.StaticEntities;
using NosCore.GameObject.Ecs.Extensions;
using NosCore.GameObject.Infastructure;
using NosCore.GameObject.Networking.ClientSession;
using NosCore.GameObject.Services.InventoryService;
using NosCore.GameObject.Services.ItemGenerationService;
using NosCore.GameObject.Services.ItemGenerationService.Item;
using NosCore.Packets.Enumerations;
using NosCore.Packets.Interfaces;
using NosCore.Packets.ServerPackets.UI;
using NosCore.Shared.Enumerations;

namespace NosCore.GameObject.Services.NpcExchangeService
{
    public class NpcExchangeService(List<NpcExchangeDto> exchanges, List<NpcExchangeMaterialDto> materials,
            IItemCatalog itemCatalog, IItemGenerationService itemProvider)
        : INpcExchangeService, ISingletonService
    {
        private readonly ILookup<short, NpcExchangeMaterialDto> _materials = materials.ToLookup(s => s.NpcExchangeId);

        public async Task ExchangeAsync(ClientSession session, NrunRunnerType runner)
        {
            var character = session.Character;
            var exchange = exchanges.FirstOrDefault(s => s.Runner == runner
                && itemCatalog.GetItem(s.ItemVNum) is { } item && item.FitsClass(character.Class));
            var cost = exchange == null ? new List<NpcExchangeMaterialDto>() : _materials[exchange.NpcExchangeId].ToList();
            if (exchange == null || cost.Count == 0)
            {
                return;
            }

            if (cost.Any(s => character.InventoryService.CountItem(s.ItemVNum) < s.Amount))
            {
                await session.SendPacketAsync(new MsgiPacket
                {
                    Type = MessageType.Default,
                    Message = Game18NConstString.RawMaterialsMissing
                });
                return;
            }

            var received = character.InventoryService.AddItemToPocket(
                InventoryItemInstance.Create(itemProvider.Create(exchange.ItemVNum, exchange.Amount), character.CharacterId));
            if (received == null || received.Count == 0)
            {
                await session.SendPacketAsync(new MsgiPacket
                {
                    Type = MessageType.Default,
                    Message = Game18NConstString.NotEnoughSpace
                });
                return;
            }

            var packets = new List<IPacket>();
            foreach (var material in cost)
            {
                packets.AddRange(character.InventoryService.RemoveItemAmountByVNum(material.ItemVNum, material.Amount));
            }

            packets.AddRange(received.Select(s => s.GeneratePocketChange(s.Type.ToPocketType(), s.Slot)));
            packets.Add(character.GenerateSayi(SayColorType.Green, Game18NConstString.ReceivedThisItem,
                2, exchange.ItemVNum.ToString(), exchange.Amount));
            await session.SendPacketsAsync(packets);
        }
    }
}
