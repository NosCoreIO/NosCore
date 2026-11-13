//  __  _  __    __   ___ __  ___ ___
// |  \| |/__\ /' _/ / _//__\| _ \ __|
// | | ' | \/ |`._`.| \_| \/ | v / _|
// |_|\__|\__/ |___/ \__/\__/|_|_\___|
//

using System;
using NodaTime;

namespace NosCore.GameObject.Ecs.Extensions;

public static class MinigameExtensions
{
    public static readonly Duration MinimumMinigameDuration = Duration.FromSeconds(10);

    public static void StartMinigame(this PlayerComponentBundle player, Guid minilandObjectId, Instant now)
    {
        player.MinigameObjectId = minilandObjectId;
        player.MinigameStartedAt = now;
        player.MinigameRewardLevel = null;
    }

    // A score only counts for the game the server saw start on that object, once it has run for
    // the minimum duration. Returns the reward level that can be claimed, if any.
    public static byte? FinishMinigame(this PlayerComponentBundle player, Guid minilandObjectId, byte? earnedLevel,
        Instant now)
    {
        var running = player.MinigameObjectId == minilandObjectId
            && player.MinigameRewardLevel == null
            && now - player.MinigameStartedAt >= MinimumMinigameDuration;
        player.MinigameObjectId = running && earnedLevel != null ? minilandObjectId : null;
        player.MinigameRewardLevel = running ? earnedLevel : null;
        return player.MinigameRewardLevel;
    }

    public static bool TryClaimMinigameReward(this PlayerComponentBundle player, Guid minilandObjectId, int tier)
    {
        if (player.MinigameObjectId != minilandObjectId || player.MinigameRewardLevel is not { } level
            || tier < 0 || tier > level)
        {
            return false;
        }

        player.MinigameObjectId = null;
        player.MinigameRewardLevel = null;
        return true;
    }
}
