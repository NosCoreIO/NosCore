//  __  _  __    __   ___ __  ___ ___
// |  \| |/__\ /' _/ / _//__\| _ \ __|
// | | ' | \/ |`._`.| \_| \/ | v / _|
// |_|\__|\__/ |___/ \__/\__/|_|_\___|
//

using Microsoft.VisualStudio.TestTools.UnitTesting;
using NosCore.Data.Enumerations.I18N;
using NosCore.Data.Resource;
using System.Globalization;

namespace NosCore.Core.Tests
{
    // The client has no text of its own for these time-space lines, so the game sends them as
    // plain text; the English ones read as the game's own do in the English time-space captures.
    [TestClass]
    public class TimeSpaceLineTests
    {
        [TestMethod]
        [DataRow(LanguageKey.TIMESPACE_ALL_LOCKED_DOORS_OPENED, "All locked doors have opened.")]
        [DataRow(LanguageKey.TIMESPACE_ALL_MONSTERS_DEFEATED, "You've defeated all the monsters!")]
        [DataRow(LanguageKey.TIMESPACE_CAN_YOU_SEE_THE_EXIT, "Can you see whether we're getting closer to the exit?")]
        [DataRow(LanguageKey.TIMESPACE_CHOOSE_A_KEY, "Select one of the keys!")]
        [DataRow(LanguageKey.TIMESPACE_DEAD_END_TURN_BACK, "Dead end! Go back!")]
        [DataRow(LanguageKey.TIMESPACE_EXIT_IS_NEAR, "It's not much further to the exit!")]
        [DataRow(LanguageKey.TIMESPACE_FIND_KEY_FOR_BONUS_TIME, "Find the key to receive bonus time!")]
        [DataRow(LanguageKey.TIMESPACE_GOAL_CAREFUL_MONSTERS_IN_SIGHT, "Warning, incoming enemies!")]
        [DataRow(LanguageKey.TIMESPACE_GOAL_ENEMIES_APPROACHING_KILL_ALL, "Enemies are approaching. Eliminate them!")]
        [DataRow(LanguageKey.TIMESPACE_GOAL_ENEMIES_COMING, "Enemies are coming!")]
        [DataRow(LanguageKey.TIMESPACE_GOAL_ENEMIES_IN_SIGHT, "Enemies in sight!")]
        [DataRow(LanguageKey.TIMESPACE_GOAL_ENEMY_CREATURES_APPROACHING, "Dangerous creatures are approaching!")]
        [DataRow(LanguageKey.TIMESPACE_GOAL_IMPS_ATTACK, "The Imps are attacking!")]
        [DataRow(LanguageKey.TIMESPACE_GOAL_KILL_ALL_IN_TIME, "Defeat all the enemies before the time runs out!")]
        [DataRow(LanguageKey.TIMESPACE_GOAL_MONSTERS_KILL_THEM_ALL, "Monsters! Eliminate them all!")]
        [DataRow(LanguageKey.TIMESPACE_GOAL_MORE_MONSTERS_KILL_THEM, "More enemies have appeared! Defeat them!")]
        [DataRow(LanguageKey.TIMESPACE_GOAL_PROTECT_GUARD_KILL_ALL, "Protect the NosVille Guards and eliminate all the monsters!")]
        [DataRow(LanguageKey.TIMESPACE_GOAL_PROTECT_GUARD_SOLDIER, "Protect the NosVille Guard soldier from attack.")]
        [DataRow(LanguageKey.TIMESPACE_GOAL_SURVIVE_UNTIL_DOOR_OPENS, "Survive one minute until a door opens.")]
        [DataRow(LanguageKey.TIMESPACE_GOAL_UNDER_ATTACK, "You're under attack!")]
        [DataRow(LanguageKey.TIMESPACE_GOAL_WATCH_OUT_ENEMIES, "Look out, enemies!")]
        [DataRow(LanguageKey.TIMESPACE_LABYRINTH_ENTRANCE, "The entrance to the maze...")]
        [DataRow(LanguageKey.TIMESPACE_LEVER_TOO_DANGEROUS, "If only we could operate the lever! But it seems very dangerous. Let's go back...")]
        [DataRow(LanguageKey.TIMESPACE_MINERS_PROTESTING, "The miners are demonstrating...")]
        [DataRow(LanguageKey.TIMESPACE_MORE_KEEP_COMING, "There seems to be no end to them!")]
        [DataRow(LanguageKey.TIMESPACE_NOSVILLE_GUARD_SAFE, "The NosVille Guard is safe.")]
        [DataRow(LanguageKey.TIMESPACE_NO_SHAME_IN_FLEEING, "There's no shame in running away from monsters which are too powerful for you.")]
        [DataRow(LanguageKey.TIMESPACE_ROOM_WITH_A_WAY_OUT, "A room with an exit!")]
        [DataRow(LanguageKey.TIMESPACE_SWEET_SCENT_PURE_GLOW, "Sweet Smells and Soft Lights?")]
        public void ATimeSpaceLineReadsAsTheGameWritesIt(LanguageKey key, string line) =>
            Assert.AreEqual(line, LocalizedResources.ResourceManager.GetString(key.ToString(), CultureInfo.InvariantCulture));
    }
}
