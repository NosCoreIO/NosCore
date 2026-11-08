//  __  _  __    __   ___ __  ___ ___
// |  \| |/__\ /' _/ / _//__\| _ \ __|
// | | ' | \/ |`._`.| \_| \/ | v / _|
// |_|\__|\__/ |___/ \__/\__/|_|_\___|
//

using System.Threading.Tasks;
using JetBrains.Annotations;
using Microsoft.Extensions.Options;
using NosCore.Core.Configuration;
using NosCore.Data.Enumerations.Items;
using NosCore.GameObject.Ecs.Extensions;
using NosCore.GameObject.Messaging.Events;
using NosCore.Packets.Enumerations;
using NosCore.Packets.ServerPackets.UI;

namespace NosCore.GameObject.Messaging.Handlers.UseItem
{
    [UsedImplicitly]
    public sealed class SpecialistMedalHandler(IOptions<WorldConfiguration> worldConfiguration)
    {
        private const int AdditionalPointsPerBasicPoint = 3;

        [UsedImplicitly]
        public async Task Handle(ItemUsedEvent evt)
        {
            var item = evt.InventoryItem.ItemInstance.Item;
            if (item.ItemType != ItemType.Special || item.Effect != ItemEffectType.SpecialistMedal)
            {
                return;
            }

            var session = evt.ClientSession;
            var character = session.Character;
            var configuration = worldConfiguration.Value;

            if (character.SpPoint >= configuration.MaxSpPoints
                && character.SpAdditionPoint >= configuration.MaxAdditionalSpPoints)
            {
                await session.SendPacketAsync(new MsgiPacket
                {
                    Type = MessageType.Default,
                    Message = Game18NConstString.CannotBeUsedExceedsCapacity
                });
                return;
            }

            var medal = evt.InventoryItem;
            character.InventoryService.RemoveItemAmountFromInventory(1, medal.ItemInstanceId);
            await session.SendPacketAsync(medal.GeneratePocketChange((PocketType)medal.Type, medal.Slot));

            character = session.Character;
            character.AddSpPoints(item.EffectValue, worldConfiguration);
            character.AddAdditionalSpPoints(item.EffectValue * AdditionalPointsPerBasicPoint, worldConfiguration);
            await session.SendPacketAsync(character.GenerateSpPoint(worldConfiguration));
            await session.SendPacketAsync(new MsgiPacket
            {
                Type = MessageType.Default,
                Message = Game18NConstString.BasicSpecialistPointsIncreased,
                ArgumentType = 4,
                Game18NArguments = { item.EffectValue }
            });
        }
    }
}
