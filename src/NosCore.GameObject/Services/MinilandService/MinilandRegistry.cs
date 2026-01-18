//  __  _  __    __   ___ __  ___ ___
// |  \| |/__\ /' _/ / _//__\| _ \ __|
// | | ' | \/ |`._`.| \_| \/ | v / _|
// |_|\__|\__/ |___/ \__/\__/|_|_\___|
//
// Copyright (C) 2019 - NosCore
//
// NosCore is a free software: you can redistribute it and/or modify
// it under the terms of the GNU General Public License as published by
// the Free Software Foundation, either version 3 of the License, or any later version.
//
// This program is distributed in the hope that it will be useful,
// but WITHOUT ANY WARRANTY; without even the implied warranty of
// MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
// GNU General Public License for more details.
//
// You should have received a copy of the GNU General Public License
// along with this program.  If not, see <http://www.gnu.org/licenses/>.

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;

namespace NosCore.GameObject.Services.MinilandService
{
    public class MinilandRegistry : IMinilandRegistry
    {
        private readonly ConcurrentDictionary<long, Miniland> _minilands = new();

        public Miniland? GetMiniland(long characterId)
        {
            return _minilands.TryGetValue(characterId, out var miniland) ? miniland : null;
        }

        public bool TryRegister(long characterId, Miniland miniland)
        {
            return _minilands.TryAdd(characterId, miniland);
        }

        public bool TryUnregister(long characterId, out Miniland? miniland)
        {
            return _minilands.TryRemove(characterId, out miniland);
        }

        public bool ContainsKey(long characterId)
        {
            return _minilands.ContainsKey(characterId);
        }

        public Miniland? FindByMapInstanceId(Guid mapInstanceId)
        {
            return _minilands.Values.FirstOrDefault(s => s.MapInstanceId == mapInstanceId);
        }

        public IEnumerable<Miniland> GetAll()
        {
            return _minilands.Values;
        }
    }
}
