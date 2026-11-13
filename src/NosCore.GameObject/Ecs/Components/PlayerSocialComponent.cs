//  __  _  __    __   ___ __  ___ ___
// |  \| |/__\ /' _/ / _//__\| _ \ __|
// | | ' | \/ |`._`.| \_| \/ | v / _|
// |_|\__|\__/ |___/ \__/\__/|_|_\___|
//

using NodaTime;
using NosCore.GameObject.Ecs.Extensions;
using NosCore.GameObject.Services.ExchangeService;
using NosCore.GameObject.Services.FamilyService;
using System.Collections.Concurrent;

namespace NosCore.GameObject.Ecs.Components;

public record struct PlayerSocialComponent(
    ConcurrentDictionary<long, long> GroupRequestCharacterIds,
    Instant? LastGroupRequest,
    Family? Family,
    ConcurrentDictionary<long, long> FamilyRequestCharacterIds,
    ConcurrentDictionary<long, long> MinilandInviteCharacterIds,
    ConcurrentDictionary<long, Instant> ExchangeRequestCharacterIds,
    Instant? LastExchangeRequest,
    ConcurrentDictionary<(InviteType Type, long TargetId), Instant> LastInvites,
    ConcurrentDictionary<long, long> MarriageProposalCharacterIds,
    ConcurrentDictionary<long, long> ReturnPointShareCharacterIds,
    ExchangeData? Exchange = null);
