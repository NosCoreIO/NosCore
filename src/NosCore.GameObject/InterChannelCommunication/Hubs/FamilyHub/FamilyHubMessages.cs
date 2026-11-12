//  __  _  __    __   ___ __  ___ ___
// |  \| |/__\ /' _/ / _//__\| _ \ __|
// | | ' | \/ |`._`.| \_| \/ | v / _|
// |_|\__|\__/ |___/ \__/\__/|_|_\___|
//

using NosCore.GameObject.Services.FamilyService;
using System.Collections.Generic;

namespace NosCore.GameObject.InterChannelCommunication.Hubs.FamilyHub;

public record PurchaseFamilyExtensionRequest(long FamilyId, long CharacterId, short ItemVNum) : IMasterRequest;

public record AddFamilyExperienceRequest(long FamilyId, long CharacterId, int Experience) : IMasterRequest;

public record CreateFamilyRequest(string Name, byte MaxSize, long HeadId, IReadOnlyList<long> DeputyIds) : IMasterRequest;

public record DisbandFamilyRequest(long FamilyId) : IMasterRequest;

public record AddFamilyMemberRequest(long FamilyId, long CharacterId) : IMasterRequest;

public record RemoveFamilyMemberRequest(long FamilyId, long CharacterId) : IMasterRequest;

public record ChangeFamilyRequest(long FamilyId, FamilyChange? Family, IReadOnlyList<FamilyMemberChange> Members) : IMasterRequest;
