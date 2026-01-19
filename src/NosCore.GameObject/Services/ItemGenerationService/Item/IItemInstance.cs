//  __  _  __    __   ___ __  ___ ___
// |  \| |/__\ /' _/ / _//__\| _ \ __|
// | | ' | \/ |`._`.| \_| \/ | v / _|
// |_|\__|\__/ |___/ \__/\__/|_|_\___|
//

using NosCore.Data.Dto;

namespace NosCore.GameObject.Services.ItemGenerationService.Item
{
    public interface IItemInstance : IItemInstanceDto
    {
        Item Item { get; set; }
        object Clone();
    }
}
