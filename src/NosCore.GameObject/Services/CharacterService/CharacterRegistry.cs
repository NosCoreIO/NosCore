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

using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;

namespace NosCore.GameObject.Services.CharacterService;

public class CharacterRegistry : ICharacterRegistry
{
    private readonly ConcurrentDictionary<long, CharacterGameState> _states = new();

    public void Register(long characterId, CharacterGameState state)
    {
        _states[characterId] = state;
    }

    public void Unregister(long characterId)
    {
        _states.TryRemove(characterId, out _);
    }

    public CharacterGameState? GetState(long characterId)
    {
        return _states.TryGetValue(characterId, out var state) ? state : null;
    }

    public bool TryGetState(long characterId, [NotNullWhen(true)] out CharacterGameState? state)
    {
        return _states.TryGetValue(characterId, out state);
    }

    public IEnumerable<CharacterGameState> GetAll()
    {
        return _states.Values;
    }
}
