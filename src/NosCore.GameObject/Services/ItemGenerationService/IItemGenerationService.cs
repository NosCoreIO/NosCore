//  __  _  __    __   ___ __  ___ ___
// |  \| |/__\ /' _/ / _//__\| _ \ __|
// | | ' | \/ |`._`.| \_| \/ | v / _|
// |_|\__|\__/ |___/ \__/\__/|_|_\___|
//

using NosCore.Data.Dto;
using NosCore.GameObject.Services.ItemGenerationService.Item;

namespace NosCore.GameObject.Services.ItemGenerationService
{
    public interface IItemGenerationService
    {
        IItemInstance Create(short itemToCreateVNum);
        IItemInstance Create(short itemToCreateVNum, short amount);
        IItemInstance Create(short itemToCreateVNum, short amount, sbyte rare);
        IItemInstance Create(short itemToCreateVNum, short amount, sbyte rare, byte upgrade);

        IItemInstance Create(short itemToCreateVNum, short amount, sbyte rare, byte upgrade, byte design);

        IItemInstance Convert(IItemInstanceDto k);
    }
}
