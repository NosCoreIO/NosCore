//  __  _  __    __   ___ __  ___ ___
// |  \| |/__\ /' _/ / _//__\| _ \ __|
// | | ' | \/ |`._`.| \_| \/ | v / _|
// |_|\__|\__/ |___/ \__/\__/|_|_\___|
//

using NosCore.Data.Enumerations.Miniland;
using NosCore.Data.StaticEntities;
using NosCore.GameObject.Services.UpgradeService;
using System.Collections.Generic;
using System.Linq;

namespace NosCore.GameObject.Services.MinilandService
{
    public interface IMinigameRewardService
    {
        /// <summary>The highest score of each band, from the one that earns no reward up.</summary>
        IReadOnlyList<int> GetMaximumScores(MinigameType type);

        /// <summary>The reward box a score earns, or null when it stays in the lowest band.</summary>
        byte? GetLevel(MinigameType type, long score);

        MinigameRewardDto? DrawReward(short minigameVNum, byte level);
    }

    public class MinigameRewardService(List<MinigameScoreDto> scores, List<MinigameRewardDto> rewards,
        IRandomNumberSource random) : IMinigameRewardService
    {
        private readonly Dictionary<MinigameType, int[]> _maximumScores = scores
            .GroupBy(s => s.MinigameType)
            .ToDictionary(g => g.Key, g => g.OrderBy(s => s.Level).Select(s => s.MaximumScore).ToArray());

        private readonly ILookup<(short, byte), MinigameRewardDto> _rewards =
            rewards.ToLookup(r => (r.MinigameVNum, r.Level));

        public IReadOnlyList<int> GetMaximumScores(MinigameType type) =>
            _maximumScores.TryGetValue(type, out var maximums) ? maximums : [];

        public byte? GetLevel(MinigameType type, long score)
        {
            var passed = GetMaximumScores(type).TakeWhile(max => score > max).Count();
            return passed == 0 ? null : (byte)(passed - 1);
        }

        public MinigameRewardDto? DrawReward(short minigameVNum, byte level)
        {
            var candidates = _rewards[(minigameVNum, level)].ToList();
            return candidates.Count == 0 ? null : candidates[(int)(random.NextDouble() * candidates.Count)];
        }
    }
}
