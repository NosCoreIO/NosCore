//  __  _  __    __   ___ __  ___ ___
// |  \| |/__\ /' _/ / _//__\| _ \ __|
// | | ' | \/ |`._`.| \_| \/ | v / _|
// |_|\__|\__/ |___/ \__/\__/|_|_\___|
//

using System;

namespace NosCore.GameObject.Services.ExperienceService
{
    // Percentages added to what a character earns. Job experience also takes the experience bonus.
    public readonly record struct ExperienceBonus(int Experience, int JobExperience, int HeroExperience, int Reputation)
    {
        public long ApplyToExperience(long amount) => Apply(amount, Experience);

        public long ApplyToJobExperience(long amount) => Apply(amount, Experience + JobExperience);

        public long ApplyToHeroExperience(long amount) => Apply(amount, HeroExperience);

        public long ApplyToReputation(long amount) => Apply(amount, Reputation);

        private static long Apply(long amount, int percent) => amount * Math.Max(0, 100 + percent) / 100;
    }
}
