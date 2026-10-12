//  __  _  __    __   ___ __  ___ ___
// |  \| |/__\ /' _/ / _//__\| _ \ __|
// | | ' | \/ |`._`.| \_| \/ | v / _|
// |_|\__|\__/ |___/ \__/\__/|_|_\___|
//

using NosCore.Data.Enumerations.Miniland;
using NosCore.Data.WebApi;
using System;

namespace NosCore.GameObject.InterChannelCommunication.Hubs.WarehouseHub;

public record GetWarehouseItemsRequest(Guid? Id, long? OwnerId, WarehouseType WarehouseType, byte? Slot) : IMasterRequest;

public record DeleteWarehouseItemRequest(Guid Id) : IMasterRequest;

public record AddWarehouseItemRequest(WareHouseDepositRequest DepositRequest) : IMasterRequest;
