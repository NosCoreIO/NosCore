//  __  _  __    __   ___ __  ___ ___
// |  \| |/__\ /' _/ / _//__\| _ \ __|
// | | ' | \/ |`._`.| \_| \/ | v / _|
// |_|\__|\__/ |___/ \__/\__/|_|_\___|
//

using System.Threading.Tasks;
using JetBrains.Annotations;
using NosCore.Data.Enumerations.Items;
using NosCore.GameObject.Ecs.Extensions;
using NosCore.GameObject.Messaging.Events;
using NosCore.GameObject.Services.MinilandService;
using NosCore.Packets.Enumerations;
using NosCore.Packets.ServerPackets.UI;
using NosCore.Shared.Enumerations;

namespace NosCore.GameObject.Messaging.Handlers.UseItem
{
    [UsedImplicitly]
    public sealed class ProductionCouponHandler(IMinilandService minilandService)
    {
        [UsedImplicitly]
        public async Task Handle(ItemUsedEvent evt)
        {
            var coupon = evt.InventoryItem;
            if (coupon.ItemInstance.Item.Effect != ItemEffectType.ProductionCoupon)
            {
                return;
            }

            var character = evt.ClientSession.Character;
            var points = coupon.ItemInstance.Item.EffectValue;
            minilandService.GetMiniland(character.CharacterId).MinilandPoint += points;
            await character.UseUpOneAsync(coupon);
            await evt.ClientSession.SendPacketAsync(new MsgiPacket
            {
                Type = MessageType.Default,
                Message = Game18NConstString.ProductionPointsToppedUp,
                ArgumentType = 4,
                Game18NArguments = { points },
            });
        }
    }
}
