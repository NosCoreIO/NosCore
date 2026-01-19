//  __  _  __    __   ___ __  ___ ___
// |  \| |/__\ /' _/ / _//__\| _ \ __|
// | | ' | \/ |`._`.| \_| \/ | v / _|
// |_|\__|\__/ |___/ \__/\__/|_|_\___|
//

using NosCore.GameObject.ComponentEntities.Entities;
using NosCore.GameObject.Services.InventoryService;
using NosCore.GameObject.Services.MapInstanceGenerationService;
using NosCore.Packets.Enumerations;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace NosCore.GameObject.Services.MinilandService
{
    public interface IMinilandService
    {
        Miniland GetMiniland(long character);
        Task<Guid?> DeleteMinilandAsync(long characterId);
        Task<Miniland> InitializeAsync(Character character, IMapInstanceGeneratorService generatorService);
        List<Portal> GetMinilandPortals(long characterId);
        Miniland? GetMinilandFromMapInstanceId(Guid mapInstanceId);
        void AddMinilandObject(MapDesignObject mapObject, long characterId, InventoryItemInstance minilandobject);
        Task SetStateAsync(long characterId, MinilandState state);
    }
}
