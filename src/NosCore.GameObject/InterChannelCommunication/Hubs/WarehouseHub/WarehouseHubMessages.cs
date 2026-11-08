//  __  _  __    __   ___ __  ___ ___
// |  \| |/__\ /' _/ / _//__\| _ \ __|
// | | ' | \/ |`._`.| \_| \/ | v / _|
// |_|\__|\__/ |___/ \__/\__/|_|_\___|
//

using NosCore.Data.Dto;
using NosCore.Data.Enumerations.Miniland;
using NosCore.Data.WebApi;
using System;

namespace NosCore.GameObject.InterChannelCommunication.Hubs.WarehouseHub;

public record GetWarehouseItemsRequest(Guid? Id, long? OwnerId, WarehouseType WarehouseType, byte? Slot) : IMasterRequest;

public record DeleteWarehouseItemRequest(Guid Id) : IMasterRequest;

public record AddWarehouseItemRequest(WareHouseDepositRequest DepositRequest) : IMasterRequest;

public record MoveWarehouseItemRequest(long OwnerId, WarehouseType WarehouseType, short SourceSlot, short DestinationSlot) : IMasterRequest;

public record TakeWarehouseItemRequest(Guid Id, short Amount) : IMasterRequest;

public record ReturnWarehouseItemRequest(long OwnerId, WarehouseType WarehouseType, short Slot, ItemInstanceDto ItemInstance) : IMasterRequest;

public record SplitWarehouseItemRequest(long OwnerId, WarehouseType WarehouseType, short SourceSlot, short DestinationSlot, ItemInstanceDto Split) : IMasterRequest;
