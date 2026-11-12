//  __  _  __    __   ___ __  ___ ___
// |  \| |/__\ /' _/ / _//__\| _ \ __|
// | | ' | \/ |`._`.| \_| \/ | v / _|
// |_|\__|\__/ |___/ \__/\__/|_|_\___|
//

using System.Threading.Tasks;
using JetBrains.Annotations;
using Microsoft.Extensions.Options;
using NosCore.Data.Enumerations.Items;
using NosCore.GameObject.Services.InventoryService;
using NosCore.Core.Configuration;
using NosCore.Data.Enumerations;
using NosCore.GameObject.Ecs.Extensions;
using NosCore.GameObject.Messaging.Events;
using NosCore.GameObject.Services.ItemGenerationService.Item;
using NosCore.GameObject.Services.UpgradeService;
using NosCore.Packets.ClientPackets.Inventory;
using NosCore.Packets.Enumerations;
using NosCore.Packets.ServerPackets.UI;
using NosCore.Shared.Enumerations;

namespace NosCore.GameObject.Messaging.Handlers.UseItem
{
    [UsedImplicitly]
    public sealed class SoulRevivalHandler(IOptions<WorldConfiguration> worldConfiguration)
    {
        [UsedImplicitly]
        public async Task Handle(ItemUsedEvent evt)
        {
            var stone = evt.InventoryItem;
            if (stone.ItemInstance.Item.Effect != ItemEffectType.SoulRevival
                || evt.Packet.TargetType is not { } targetType || evt.Packet.TargetSlot is not { } targetSlot)
            {
                return;
            }

            var session = evt.ClientSession;
            var character = session.Character;
            var card = character.InventoryService.LoadBySlotAndType(targetSlot, (NoscorePocketType)targetType);
            if (card?.ItemInstance is not SpecialistInstance specialist
                || specialist.Rare != SpUpgradeOperationBase.DestroyedSoulRare)
            {
                return;
            }

            if (evt.Packet.Mode == 0)
            {
                await session.SendPacketAsync(new QnaiPacket
                {
                    YesPacket = new UseItemPacket
                    {
                        VisualType = VisualType.Player,
                        VisualId = character.CharacterId,
                        Type = stone.Type.ToPocketType(),
                        Slot = stone.Slot,
                        Mode = 1,
                        Parameter = 0,
                        TargetType = targetType,
                        TargetSlot = targetSlot
                    },
                    Question = Game18NConstString.AskWantToUse,
                    ArgumentType = 2,
                    Game18NArguments = { stone.ItemInstance.ItemVNum.ToString() }
                });
                return;
            }

            specialist.Rare = 0;
            await character.UseUpOneAsync(stone);
            await session.SendPacketAsync(card.GeneratePocketChange(card.Type.ToPocketType(), card.Slot));

            character.AddSpPoints(stone.ItemInstance.Item.EffectValue, worldConfiguration);
            await session.SendPacketAsync(character.GenerateSpPoint(worldConfiguration));
            await session.SendPacketAsync(new MsgiPacket
            {
                Type = MessageType.Default,
                Message = Game18NConstString.CardSoulRevived
            });
        }
    }
}
