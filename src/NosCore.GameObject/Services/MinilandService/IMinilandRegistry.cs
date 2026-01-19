//  __  _  __    __   ___ __  ___ ___
// |  \| |/__\ /' _/ / _//__\| _ \ __|
// | | ' | \/ |`._`.| \_| \/ | v / _|
// |_|\__|\__/ |___/ \__/\__/|_|_\___|
//

using System;
using System.Collections.Generic;

namespace NosCore.GameObject.Services.MinilandService
{
    public interface IMinilandRegistry
    {
        Miniland? GetByCharacterId(long characterId);
        Miniland? GetByMapInstanceId(Guid mapInstanceId);
        IEnumerable<Miniland> GetAll();
        bool ContainsCharacter(long characterId);
        void Register(long characterId, Miniland miniland);
        bool Unregister(long characterId, out Miniland? miniland);
    }
}
