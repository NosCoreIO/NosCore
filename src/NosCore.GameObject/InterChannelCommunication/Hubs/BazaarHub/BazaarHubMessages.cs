//  __  _  __    __   ___ __  ___ ___
// |  \| |/__\ /' _/ / _//__\| _ \ __|
// | | ' | \/ |`._`.| \_| \/ | v / _|
// |_|\__|\__/ |___/ \__/\__/|_|_\___|
//

using Json.Patch;
using NosCore.Data.WebApi;
using NosCore.Packets.Enumerations;

namespace NosCore.GameObject.InterChannelCommunication.Hubs.BazaarHub;

public record GetBazaarRequest(long Id, byte? Index, byte? PageSize, BazaarListType? TypeFilter, byte? SubTypeFilter,
    byte? LevelFilter, byte? RareFilter, byte? UpgradeFilter, long? SellerFilter) : IMasterRequest;

public record DeleteBazaarRequest(long Id, short Count, string RequestCharacterName, long? RequestCharacterId) : IMasterRequest;

public record AddBazaarRequest(BazaarRequest BazaarRequest) : IMasterRequest;

public record ModifyBazaarRequest(long Id, JsonPatch BzMod) : IMasterRequest;
