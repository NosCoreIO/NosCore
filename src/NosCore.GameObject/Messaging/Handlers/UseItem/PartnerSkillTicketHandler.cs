//  __  _  __    __   ___ __  ___ ___
// |  \| |/__\ /' _/ / _//__\| _ \ __|
// | | ' | \/ |`._`.| \_| \/ | v / _|
// |_|\__|\__/ |___/ \__/\__/|_|_\___|
//

using System.Threading.Tasks;
using JetBrains.Annotations;
using NosCore.Data.Enumerations.Items;
using NosCore.GameObject.Messaging.Events;
using NosCore.GameObject.Services.MateService;

namespace NosCore.GameObject.Messaging.Handlers.UseItem
{
    [UsedImplicitly]
    public sealed class PartnerSkillTicketHandler(IPartnerSpecialistService partnerSpecialistService)
    {
        [UsedImplicitly]
        public Task Handle(ItemUsedEvent evt) =>
            evt.InventoryItem.ItemInstance.Item.Effect is ItemEffectType.PartnerSkillTicket or ItemEffectType.PartnerSkillTicketAll
                ? partnerSpecialistService.UseSkillTicketAsync(evt.ClientSession.Character, evt.InventoryItem, evt.Packet)
                : Task.CompletedTask;
    }
}
