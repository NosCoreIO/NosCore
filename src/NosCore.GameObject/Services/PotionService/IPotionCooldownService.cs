//  __  _  __    __   ___ __  ___ ___
// |  \| |/__\ /' _/ / _//__\| _ \ __|
// | | ' | \/ |`._`.| \_| \/ | v / _|
// |_|\__|\__/ |___/ \__/\__/|_|_\___|
//

using NosCore.GameObject.Ecs;

namespace NosCore.GameObject.Services.PotionService
{
    public interface IPotionCooldownService
    {
        bool IsReady(PlayerComponentBundle character);

        void Start(PlayerComponentBundle character);
    }
}
