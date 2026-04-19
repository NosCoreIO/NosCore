//  __  _  __    __   ___ __  ___ ___
// |  \| |/__\ /' _/ / _//__\| _ \ __|
// | | ' | \/ |`._`.| \_| \/ | v / _|
// |_|\__|\__/ |___/ \__/\__/|_|_\___|
//

using NosCore.Data.Enumerations.Items;
using NosCore.Data.Enumerations.Map;
using NosCore.GameObject.Ecs.Extensions;
using NosCore.GameObject.Networking.ClientSession;
using NosCore.GameObject.Services.InventoryService;
using NosCore.GameObject.Services.MapChangeService;
using NosCore.GameObject.Services.MinilandService;
using NosCore.Packets.ClientPackets.Inventory;
using NosCore.Packets.Enumerations;
using NosCore.Packets.ServerPackets.Chats;
using NosCore.Packets.ServerPackets.UI;
using NosCore.Shared.Enumerations;
using System;
using System.Threading.Tasks;

namespace NosCore.GameObject.Services.ItemGenerationService.Handlers
{
    public class MinilandBellHandler(IMinilandService minilandProvider, IMapChangeService mapChangeService)
        : IUseItemEventHandler
    {
        public bool Condition(Item.Item item) => item.Effect == ItemEffectType.Teleport && item.EffectValue == 2;

        public async Task ExecuteAsync(RequestData<Tuple<InventoryItemInstance, UseItemPacket>> requestData)
        {
            var session = requestData.ClientSession;
            var itemInstance = requestData.Data.Item1;
            var packet = requestData.Data.Item2;

            var character = session.Character;
            if (character.MapInstance.MapInstanceType != MapInstanceType.BaseMapInstance)
            {
                await session.SendPacketAsync(new SayiPacket
                {
                    VisualType = VisualType.Player,
                    VisualId = character.CharacterId,
                    Type = SayColorType.Yellow,
                    Message = Game18NConstString.CanNotBeUsedHere
                });
                return;
            }

            character = session.Character;
            if (character.IsVehicled)
            {
                await session.SendPacketAsync(new SayiPacket
                {
                    VisualType = VisualType.Player,
                    VisualId = character.CharacterId,
                    Type = SayColorType.Yellow,
                    Message = Game18NConstString.OnlyPotionInVehicle
                });
                return;
            }

            if (packet.Mode == 0)
            {
                character = session.Character;
                await session.SendPacketAsync(new DelayPacket
                {
                    Delay = 5000,
                    Type = DelayPacketType.ItemInUse,
                    Packet = character.GenerateUseItem((PocketType)itemInstance.Type, itemInstance.Slot, 2, 0)
                });
                return;
            }

            character = session.Character;
            character.InventoryService.RemoveItemAmountFromInventory(1, itemInstance.ItemInstanceId);
            var characterId = character.CharacterId;
            await session.SendPacketAsync(itemInstance.GeneratePocketChange((PocketType)itemInstance.Type, itemInstance.Slot));
            var miniland = minilandProvider.GetMiniland(characterId);
            await mapChangeService.ChangeMapInstanceAsync(session, miniland.MapInstanceId, 5, 8);
        }
    }
}
