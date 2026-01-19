//  __  _  __    __   ___ __  ___ ___
// |  \| |/__\ /' _/ / _//__\| _ \ __|
// | | ' | \/ |`._`.| \_| \/ | v / _|
// |_|\__|\__/ |___/ \__/\__/|_|_\___|
//

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;

namespace NosCore.GameObject.Services.MinilandService
{
    public class MinilandRegistry : IMinilandRegistry
    {
        private readonly ConcurrentDictionary<long, Miniland> _minilands = new();

        public Miniland? GetByCharacterId(long characterId) =>
            _minilands.TryGetValue(characterId, out var miniland) ? miniland : null;

        public Miniland? GetByMapInstanceId(Guid mapInstanceId) =>
            _minilands.Values.FirstOrDefault(s => s.MapInstanceId == mapInstanceId);

        public IEnumerable<Miniland> GetAll() => _minilands.Values;

        public bool ContainsCharacter(long characterId) => _minilands.ContainsKey(characterId);

        public void Register(long characterId, Miniland miniland) =>
            _minilands.TryAdd(characterId, miniland);

        public bool Unregister(long characterId, out Miniland? miniland) =>
            _minilands.TryRemove(characterId, out miniland);
    }
}
