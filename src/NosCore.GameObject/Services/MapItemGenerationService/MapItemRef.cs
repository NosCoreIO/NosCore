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
using NodaTime;
using NosCore.GameObject.Ecs;
using NosCore.GameObject.Networking;
using NosCore.GameObject.Services.ItemGenerationService.Item;
using NosCore.GameObject.Services.MapInstanceGenerationService;
using NosCore.Packets.ClientPackets.Drops;
using NosCore.Packets.ServerPackets.Entities;
using NosCore.Shared.Enumerations;
using System;
using System.Collections.Generic;
using System.Reactive.Subjects;
using System.Threading.Tasks;

namespace NosCore.GameObject.Services.MapItemGenerationService;

public class MapItemRef
{
    private readonly MapItemData _data;

    public MapItemRef(Entity entity, MapInstance mapInstance, MapItemData data)
    {
        EcsEntity = entity;
        MapInstance = mapInstance;
        _data = data;
    }

    public Entity EcsEntity { get; set; }
    public MapInstance MapInstance { get; set; }

    public VisualType VisualType => VisualType.Object;
    public long VisualId => EcsEntity.GetVisualId(MapInstance.EcsWorld);
    public short VNum => _data.ItemInstance.ItemVNum;
    public short Amount => _data.ItemInstance.Amount;

    public IItemInstance ItemInstance => _data.ItemInstance;
    public List<Task> HandlerTasks => _data.HandlerTasks;
    public Dictionary<Type, Subject<RequestData<Tuple<MapItemRef, GetPacket>>>> Requests => _data.Requests;

    public long? OwnerId
    {
        get => EcsEntity.GetMapItemOwnerId(MapInstance.EcsWorld);
        set => EcsEntity.SetMapItemOwnerId(MapInstance.EcsWorld, value);
    }

    public Instant DroppedAt
    {
        get => EcsEntity.GetMapItemDroppedAt(MapInstance.EcsWorld);
        set => EcsEntity.SetMapItemDroppedAt(MapInstance.EcsWorld, value);
    }

    public short PositionX => EcsEntity.GetPositionX(MapInstance.EcsWorld);
    public short PositionY => EcsEntity.GetPositionY(MapInstance.EcsWorld);
    public byte Direction => EcsEntity.GetDirection(MapInstance.EcsWorld);

    public DropPacket GenerateDrop()
    {
        return new DropPacket
        {
            VNum = VNum,
            VisualId = VisualId,
            PositionX = PositionX,
            PositionY = PositionY,
            Amount = Amount,
            OwnerId = OwnerId
        };
    }
}
