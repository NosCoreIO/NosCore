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

using Arch.Core;
using NosCore.GameObject.Networking;
using NosCore.GameObject.Services.ItemGenerationService.Item;
using NosCore.Packets.ClientPackets.Drops;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Reactive.Subjects;
using System.Threading.Tasks;

namespace NosCore.GameObject.Services.MapItemGenerationService;

public class MapItemData
{
    public required IItemInstance ItemInstance { get; init; }
    public List<Task> HandlerTasks { get; } = new();
    public Dictionary<Type, Subject<RequestData<Tuple<MapItemRef, GetPacket>>>> Requests { get; } = new()
    {
        [typeof(IGetMapItemEventHandler)] = new()
    };
}

public interface IMapItemRegistry
{
    MapItemData GetOrCreate(Entity entity, IItemInstance itemInstance);
    MapItemData? Get(Entity entity);
    void Remove(Entity entity);
}

public class MapItemRegistry : IMapItemRegistry
{
    private readonly ConcurrentDictionary<Entity, MapItemData> _data = new();

    public MapItemData GetOrCreate(Entity entity, IItemInstance itemInstance)
    {
        return _data.GetOrAdd(entity, _ => new MapItemData { ItemInstance = itemInstance });
    }

    public MapItemData? Get(Entity entity)
    {
        return _data.TryGetValue(entity, out var data) ? data : null;
    }

    public void Remove(Entity entity)
    {
        _data.TryRemove(entity, out _);
    }
}
