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
using Arch.Core;
using NosCore.GameObject.Ecs;
using NosCore.GameObject.Ecs.Components;
using System.Collections.Concurrent;

namespace NosCore.GameObject.Services.BattleService;

[Obsolete("CombatState is now part of ECS via CombatStateComponent. Use entity.GetCombatState() extension method instead.")]
public interface ICombatStateRegistry
{
    CombatState GetOrCreate(Entity entity);
    void Remove(Entity entity);
}

[Obsolete("CombatState is now part of ECS via CombatStateComponent. Use entity.GetCombatState() extension method instead.")]
public class CombatStateRegistry : ICombatStateRegistry
{
    private readonly ConcurrentDictionary<Entity, CombatState> _states = new();

    public CombatState GetOrCreate(Entity entity)
    {
        return _states.GetOrAdd(entity, _ => new CombatState());
    }

    public void Remove(Entity entity)
    {
        _states.TryRemove(entity, out _);
    }
}
