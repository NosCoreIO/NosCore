//  __  _  __    __   ___ __  ___ ___
// |  \| |/__\ /' _/ / _//__\| _ \ __|
// | | ' | \/ |`._`.| \_| \/ | v / _|
// |_|\__|\__/ |___/ \__/\__/|_|_\___|
//

namespace NosCore.GameObject.Services.ItemGenerationService.Item
{
    public static class ItemInstanceExtensions
    {
        // The item others see: a fused costume shows the costume it took its look from.
        public static short LookVNum(this IItemInstance item) =>
            (item as WearableInstance)?.AppearanceVNum ?? item.ItemVNum;
    }
}
