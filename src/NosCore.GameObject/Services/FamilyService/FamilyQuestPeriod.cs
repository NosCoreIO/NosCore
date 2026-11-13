//  __  _  __    __   ___ __  ___ ___
// |  \| |/__\ /' _/ / _//__\| _ \ __|
// | | ' | \/ |`._`.| \_| \/ | v / _|
// |_|\__|\__/ |___/ \__/\__/|_|_\___|
//

using NodaTime;
using NosCore.Data.Dto;
using NosCore.Data.StaticEntities;

namespace NosCore.GameObject.Services.FamilyService
{
    public static class FamilyQuestPeriod
    {
        public static bool HasExpired(FamilyQuestDto quest, FamilyQuestProgressDto progress, Instant now) =>
            quest.ResetsDaily && progress.ProgressedAt is { } progressedAt && progressedAt.InUtc().Date < now.InUtc().Date;

        public static FamilyQuestProgressDto Current(FamilyQuestDto quest, FamilyQuestProgressDto progress, Instant now) =>
            HasExpired(quest, progress, now)
                ? new FamilyQuestProgressDto
                {
                    FamilyQuestProgressId = progress.FamilyQuestProgressId,
                    FamilyId = progress.FamilyId,
                    QuestId = progress.QuestId,
                    TotalCompletions = progress.TotalCompletions,
                    ProgressedAt = progress.ProgressedAt
                }
                : progress;
    }
}
