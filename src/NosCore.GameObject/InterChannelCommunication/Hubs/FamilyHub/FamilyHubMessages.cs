//  __  _  __    __   ___ __  ___ ___
// |  \| |/__\ /' _/ / _//__\| _ \ __|
// | | ' | \/ |`._`.| \_| \/ | v / _|
// |_|\__|\__/ |___/ \__/\__/|_|_\___|
//

using NosCore.Data.Enumerations.Family;
using NosCore.GameObject.Services.FamilyService;
using System.Collections.Generic;

namespace NosCore.GameObject.InterChannelCommunication.Hubs.FamilyHub;

public record PurchaseFamilyExtensionRequest(long FamilyId, long CharacterId, short ItemVNum) : IMasterRequest;

public record AddFamilyExperienceRequest(long FamilyId, long CharacterId, int Experience) : IMasterRequest;

public record AdvanceFamilyQuestsRequest(long FamilyId, long? CharacterId, FamilyQuestObjective Objective, int Amount) : IMasterRequest;

public record CreateFamilyRequest(string Name, byte MaxSize, long HeadId, IReadOnlyList<long> DeputyIds) : IMasterRequest;

public record DisbandFamilyRequest(long FamilyId, long HeadId) : IMasterRequest;

public record AddFamilyMemberRequest(long FamilyId, long InviterId, long CharacterId) : IMasterRequest;

public record RemoveFamilyMemberRequest(long FamilyId, long ActorId, long CharacterId) : IMasterRequest;

public record ChangeFamilyRequest(long FamilyId, long ActorId, FamilyChange? Family, IReadOnlyList<FamilyMemberChange> Members) : IMasterRequest;
