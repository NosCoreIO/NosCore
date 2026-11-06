//  __  _  __    __   ___ __  ___ ___
// |  \| |/__\ /' _/ / _//__\| _ \ __|
// | | ' | \/ |`._`.| \_| \/ | v / _|
// |_|\__|\__/ |___/ \__/\__/|_|_\___|
//

using System.Threading.Tasks;
using NosCore.GameObject.Ecs;
using NosCore.Packets.Enumerations;

namespace NosCore.GameObject.Services.MateService
{
    public interface IPartnerEquipmentService
    {
        /// <param name="partnerSlot">The partner's slot plus one, as the client sends it.</param>
        Task WearAsync(PlayerComponentBundle character, short equipmentSlot, byte partnerSlot);

        /// <inheritdoc cref="WearAsync" />
        Task RemoveAsync(PlayerComponentBundle character, EquipmentType slot, byte partnerSlot);
    }
}
