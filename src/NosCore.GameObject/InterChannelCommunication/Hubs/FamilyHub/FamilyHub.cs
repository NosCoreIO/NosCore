//  __  _  __    __   ___ __  ___ ___
// |  \| |/__\ /' _/ / _//__\| _ \ __|
// | | ' | \/ |`._`.| \_| \/ | v / _|
// |_|\__|\__/ |___/ \__/\__/|_|_\___|
//

using Microsoft.Extensions.DependencyInjection;
using NosCore.GameObject.Services.FamilyService;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace NosCore.GameObject.InterChannelCommunication.Hubs.FamilyHub
{
    public class FamilyHub(IFamilyExtensionService familyExtensionService,
        IFamilyProgressionService familyProgressionService, IFamilyRecordService familyRecordService) : IFamilyHub
    {
        public Task<FamilyExtensionPurchase> PurchaseExtensionAsync(long familyId, long characterId, short itemVNum) =>
            familyExtensionService.PurchaseAsync(familyId, characterId, itemVNum);

        public static async Task<Reply<FamilyExtensionPurchase>> Handle(PurchaseFamilyExtensionRequest r, IServiceProvider services) =>
            new(await services.GetRequiredService<FamilyHub>().PurchaseExtensionAsync(r.FamilyId, r.CharacterId, r.ItemVNum));

        public Task<FamilyExperienceGain?> AddExperienceAsync(long familyId, long characterId, int experience) =>
            familyProgressionService.AddExperienceAsync(familyId, characterId, experience);

        public static async Task<Reply<FamilyExperienceGain?>> Handle(AddFamilyExperienceRequest r, IServiceProvider services) =>
            new(await services.GetRequiredService<FamilyHub>().AddExperienceAsync(r.FamilyId, r.CharacterId, r.Experience));

        public Task<long?> CreateAsync(string name, byte maxSize, long headId, IReadOnlyList<long> deputyIds) =>
            familyRecordService.CreateAsync(name, maxSize, headId, deputyIds);

        public static async Task<Reply<long?>> Handle(CreateFamilyRequest r, IServiceProvider services) =>
            new(await services.GetRequiredService<FamilyHub>().CreateAsync(r.Name, r.MaxSize, r.HeadId, r.DeputyIds));

        public Task<bool> DisbandAsync(long familyId, long headId) => familyRecordService.DisbandAsync(familyId, headId);

        public static async Task<Reply<bool>> Handle(DisbandFamilyRequest r, IServiceProvider services) =>
            new(await services.GetRequiredService<FamilyHub>().DisbandAsync(r.FamilyId, r.HeadId));

        public Task<FamilyMemberAddition> AddMemberAsync(long familyId, long characterId) =>
            familyRecordService.AddMemberAsync(familyId, characterId);

        public static async Task<Reply<FamilyMemberAddition>> Handle(AddFamilyMemberRequest r, IServiceProvider services) =>
            new(await services.GetRequiredService<FamilyHub>().AddMemberAsync(r.FamilyId, r.CharacterId));

        public Task<bool> RemoveMemberAsync(long familyId, long characterId) =>
            familyRecordService.RemoveMemberAsync(familyId, characterId);

        public static async Task<Reply<bool>> Handle(RemoveFamilyMemberRequest r, IServiceProvider services) =>
            new(await services.GetRequiredService<FamilyHub>().RemoveMemberAsync(r.FamilyId, r.CharacterId));

        public Task<bool> ChangeAsync(long familyId, FamilyChange? family, IReadOnlyList<FamilyMemberChange> members) =>
            familyRecordService.ChangeAsync(familyId, family, members);

        public static async Task<Reply<bool>> Handle(ChangeFamilyRequest r, IServiceProvider services) =>
            new(await services.GetRequiredService<FamilyHub>().ChangeAsync(r.FamilyId, r.Family, r.Members));
    }
}
