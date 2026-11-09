//  __  _  __    __   ___ __  ___ ___
// |  \| |/__\ /' _/ / _//__\| _ \ __|
// | | ' | \/ |`._`.| \_| \/ | v / _|
// |_|\__|\__/ |___/ \__/\__/|_|_\___|
//

using System;
using NosCore.Data.WebApi;
using NosCore.Packets.Enumerations;

namespace NosCore.GameObject.InterChannelCommunication.Hubs.BazaarHub;

public record GetBazaarRequest(long Id, byte? Index, byte? PageSize, BazaarListType? TypeFilter, byte? SubTypeFilter,
    byte? LevelFilter, byte? RareFilter, byte? UpgradeFilter, long? SellerFilter) : IMasterRequest;

public record BuyBazaarRequest(Guid OperationId, long Id, short Count, long Price, long BuyerId) : IMasterRequest;

public record ClaimBazaarRequest(Guid OperationId, long Id, long SellerId) : IMasterRequest;

public record SettleBazaarRequest(Guid OperationId) : IMasterRequest;

public record CompensateBazaarRequest(Guid OperationId) : IMasterRequest;

public record AddBazaarRequest(BazaarRequest BazaarRequest) : IMasterRequest;

public record ModifyBazaarRequest(Guid OperationId, long Id, long SellerId, long Price) : IMasterRequest;
