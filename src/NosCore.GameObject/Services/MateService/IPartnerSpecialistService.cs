//  __  _  __    __   ___ __  ___ ___
// |  \| |/__\ /' _/ / _//__\| _ \ __|
// | | ' | \/ |`._`.| \_| \/ | v / _|
// |_|\__|\__/ |___/ \__/\__/|_|_\___|
//

using System.Collections.Generic;
using System.Threading.Tasks;
using NosCore.Data.StaticEntities;
using NosCore.GameObject.Ecs;
using NosCore.GameObject.Ecs.Interfaces;
using NosCore.GameObject.Services.InventoryService;
using NosCore.GameObject.Services.ItemGenerationService.Item;
using NosCore.Packets.ClientPackets.Inventory;

namespace NosCore.GameObject.Services.MateService
{
    public interface IPartnerSpecialistService
    {
        /// <summary>
        /// Turns the team partner back when it is transformed, otherwise starts the bar whose end transforms it.
        /// </summary>
        Task ToggleAsync(PlayerComponentBundle owner);

        /// <summary>Transforms the team partner once the bar started by <see cref="ToggleAsync" /> has run out.</summary>
        Task TransformAsync(PlayerComponentBundle owner);

        Task UntransformAsync(PlayerComponentBundle owner, Mate partner);

        /// <summary>The skills of the card a transformed partner wears, in skill bar order; none when it is not transformed.</summary>
        IReadOnlyList<SkillDto> Skills(PlayerComponentBundle owner, Mate partner);

        /// <summary>Gives the skill in a slot of a partner's card a rank, once the card's agility is full.</summary>
        Task LearnSkillAsync(PlayerComponentBundle owner, byte petSlot, byte skillSlot);

        /// <summary>
        /// Puts a learned skill of a partner's card back to unlearned, or every skill for the ticket that resets all,
        /// and refills the card's agility. Asks first, then acts on the confirmed use.
        /// </summary>
        Task UseSkillTicketAsync(PlayerComponentBundle owner, InventoryItemInstance ticket, UseItemPacket packet);

        /// <summary>Casts the skill in a slot of a transformed partner's skill bar.</summary>
        Task<bool> CastAsync(PlayerComponentBundle owner, long mateTransportId, int slot, IAliveEntity target);
    }
}
