//  __  _  __    __   ___ __  ___ ___
// |  \| |/__\ /' _/ / _//__\| _ \ __|
// | | ' | \/ |`._`.| \_| \/ | v / _|
// |_|\__|\__/ |___/ \__/\__/|_|_\___|
//

using System.Linq;
using System.Threading.Tasks;
using JetBrains.Annotations;
using NosCore.Data.Enumerations;
using NosCore.Data.Enumerations.Items;
using NosCore.GameObject.Ecs.Extensions;
using NosCore.GameObject.Messaging.Events;
using NosCore.GameObject.Services.BattleService;
using NosCore.GameObject.Services.ItemGenerationService.Item;
using NosCore.GameObject.Services.ItemInfoService;
using NosCore.GameObject.Services.SpecialistCardService;
using NosCore.Packets.Enumerations;
using NosCore.Packets.ServerPackets.UI;

namespace NosCore.GameObject.Messaging.Handlers.Guri
{
    // Sent by the specialist card window to give back every skill point spent on the worn card,
    // at the cost of one Point Initialisation Potion.
    [UsedImplicitly]
    public sealed class SpPointInitializerHandler(
        ISpecialistCardService specialistCardService,
        IItemInfoService itemInfoService,
        IBattleStatsProvider battleStatsProvider,
        IVitalityService vitalityService)
    {
        [UsedImplicitly]
        public async Task Handle(GuriPacketReceivedEvent evt)
        {
            if (evt.Packet.Type != GuriPacketType.SpPointInitializer || evt.Packet.Argument != 0)
            {
                return;
            }

            var session = evt.ClientSession;
            var character = session.Character;
            if (!character.UseSp
                || character.InventoryService.LoadBySlotAndType((byte)EquipmentType.Sp, NoscorePocketType.Wear)?.ItemInstance
                    is not SpecialistInstance specialist)
            {
                await session.SendPacketAsync(new MsgiPacket
                {
                    Type = MessageType.Default,
                    Message = Game18NConstString.SpecialistCardMustBeEquipped
                });
                return;
            }

            var potion = character.InventoryService.Values.FirstOrDefault(i =>
                i.Type == NoscorePocketType.Main && i.ItemInstance.Item.Effect == ItemEffectType.PointInitialisation);
            if (potion == null)
            {
                await session.SendPacketAsync(new MsgiPacket
                {
                    Type = MessageType.Default,
                    Message = Game18NConstString.NotEnoughSpecialistPoints
                });
                return;
            }

            if (!specialistCardService.TryResetPoints(specialist))
            {
                await session.SendPacketAsync(new MsgiPacket
                {
                    Type = MessageType.Default,
                    Message = Game18NConstString.SpecialistCardAlreadyReset
                });
                return;
            }

            character.InventoryService.RemoveItemAmountFromInventory(1, potion.ItemInstanceId);
            await session.SendPacketAsync(potion.GeneratePocketChange((PocketType)potion.Type, potion.Slot));
            await vitalityService.RefreshAndNotifyAsync(character);
            await session.SendPacketAsync(itemInfoService.GenerateInfo(specialist)!);
            await session.SendPacketAsync(character.GenerateSc(battleStatsProvider));
            await session.SendPacketAsync(new MsgiPacket
            {
                Type = MessageType.Default,
                Message = Game18NConstString.PointsRestored
            });
        }
    }
}
