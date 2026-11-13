//  __  _  __    __   ___ __  ___ ___
// |  \| |/__\ /' _/ / _//__\| _ \ __|
// | | ' | \/ |`._`.| \_| \/ | v / _|
// |_|\__|\__/ |___/ \__/\__/|_|_\___|
//

using System.Linq;
using System.Threading.Tasks;
using NodaTime;
using NosCore.GameObject.Ecs;
using NosCore.GameObject.Ecs.Extensions;
using NosCore.GameObject.InterChannelCommunication.Hubs.MailHub;
using NosCore.GameObject.Services.ItemGenerationService.Item;
using NosCore.GameObject.Services.MailService;
using NosCore.Packets.Enumerations;
using NosCore.Shared.Enumerations;

namespace NosCore.GameObject.Services.InventoryService
{
    public class ItemDeliveryService(IMailHub mailHub, IClock clock) : IItemDeliveryService
    {
        public async Task<bool> DeliverAsync(PlayerComponentBundle player, IItemInstance item)
        {
            var amount = item.Amount;
            var slots = player.InventoryService.AddItemToPocket(InventoryItemInstance.Create(item, player.CharacterId));
            if (slots is { Count: > 0 })
            {
                await player.SendPacketsAsync(slots.Select(s => s.GeneratePocketChange(s.Type.ToPocketType(), s.Slot)));
                await player.SendPacketAsync(player.GenerateSayi(SayColorType.Green, Game18NConstString.ReceivedThisItem,
                    2, item.ItemVNum.ToString(), amount));
                return true;
            }

            await mailHub.SendMailAsync(GiftHelper.GenerateMailRequest(clock, player, player.CharacterId, null,
                item.ItemVNum, amount, (sbyte)item.Rare, item.Upgrade, false, null, null));
            return false;
        }
    }
}
