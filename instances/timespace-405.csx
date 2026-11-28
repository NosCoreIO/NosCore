public class TimeSpace405 : TimeSpaceScript
{
    public TimeSpace405() : base(405, "Hermit")
    {
        Levels(65, 99);
        Start(17, 15);
        Lives(3);
        Clock(TimeSpan.FromSeconds(1500));
        GoldPerKill(208);
        Reputation(1950);

        var entrance = Room(4641, 3, 11);
        var stumpHoldout = Room(4641, 4, 11);
        var lizardHoldout = Room(4647, 5, 11);
        var kupeiFight = Room(4642, 5, 10);
        var kupeiMeeting = Room(4642, 5, 9);
        var wildPath = Room(4642, 5, 8);
        var birdHoldout = Room(4642, 5, 7);
        var yakHoldout = Room(4646, 5, 6);
        var lizardPassage = Room(4645, 4, 6);
        var lastHoldout = Room(4647, 4, 7);
        var angelRoom = Room(4644, 3, 7);
        var exitRoom = Room(4642, 3, 6);

        var thirtySeconds = TimeSpan.FromSeconds(30);

        entrance.Portal(new(37, 21), stumpHoldout, new(4, 22), locked: false);
        entrance.OnEnter(r => r.Say(Game18NConstString.MissionStartWhenEnterFirstRoom));

        // Each holdout holds its doors for three minutes while a wave comes every thirty seconds, then
        // takes the monsters still standing away.
        var stumpHoldoutDoors = new[]
        {
            stumpHoldout.Portal(new(37, 21), lizardHoldout, new(4, 23)),
            stumpHoldout.Portal(new(4, 22), entrance, new(37, 21))
        };
        stumpHoldout.OnEnter(r => r
                .Objective(LanguageKey.TIMESPACE_GOAL_MONSTERS_GROW_FIERCE)
                .StartRoomClock(TimeSpan.FromSeconds(180)))
            .Every(thirtySeconds, r => r.Say(Game18NConstString.EnemiesReinforcements), times: 6)
            .Every(thirtySeconds, r => r.Spawn(Monster(182).At(17, 30)), times: 5)
            .Every(thirtySeconds, r => r.Spawn(Monster(186).At(32, 15)), times: 3)
            .Every(thirtySeconds, r => r.Spawn(Monster(182).At(13, 12)), from: TimeSpan.FromSeconds(60))
            .Every(thirtySeconds, r => r.Spawn(Monster(184).At(17, 13), Monster(182).At(31, 30)), from: TimeSpan.FromSeconds(120))
            .OnClockOut(r => r
                .Unlock(stumpHoldoutDoors)
                .Say(Game18NConstString.ADoorHasOpened)
                .Say(Game18NConstString.QuickMissionComplete)
                .Dismiss());

        var lizardHoldoutDoors = new[]
        {
            lizardHoldout.Portal(new(22, 4), kupeiFight, new(22, 37)),
            lizardHoldout.Portal(new(4, 23), stumpHoldout, new(37, 21))
        };
        lizardHoldout.OnEnter(r => r
                .Objective(LanguageKey.TIMESPACE_GOAL_MONSTERS_GROW_FIERCE)
                .StartRoomClock(TimeSpan.FromSeconds(180)))
            .Every(thirtySeconds, r => r.Say(Game18NConstString.EnemiesReinforcements), times: 6)
            .Every(thirtySeconds, r => r.Spawn(Monster(179).At(10, 28), Monster(179).At(14, 8)), times: 3)
            .Every(thirtySeconds, r => r.Spawn(Monster(179).At(29, 18)))
            .Every(thirtySeconds, r => r.Spawn(Monster(446).At(26, 28)), from: TimeSpan.FromSeconds(60), times: 3)
            .Every(thirtySeconds, r => r.Spawn(Monster(446).At(7, 13)), from: TimeSpan.FromSeconds(120))
            .OnClockOut(r => r
                .Unlock(lizardHoldoutDoors)
                .Say(Game18NConstString.ADoorHasOpened)
                .Say(Game18NConstString.QuickMissionComplete)
                .Dismiss());

        // Kupei takes the party for thieves and fights them.
        var kupeiFightDoors = new[]
        {
            kupeiFight.Portal(new(21, 5), kupeiMeeting, new(22, 37)),
            kupeiFight.Portal(new(22, 37), lizardHoldout, new(22, 4))
        };
        kupeiFight.ClearedAfter(1, 515);
        kupeiFight.OnEnter(r => r.Dialog(7325))
            .OnDialogClosed(7325, r => r
                .Spawn(Monster(477).At(14, 15), Monster(515).At(21, 23).Bonus())
                .Objective(LanguageKey.TIMESPACE_GOAL_KILL_THE_BOSS))
            .OnCleared(r => r
                .Dismiss()
                .Unlock(kupeiFightDoors)
                .Say(Game18NConstString.ADoorHasOpened)
                .Say(Game18NConstString.QuickMissionComplete));

        // Then asks for help and follows the party to the end.
        kupeiMeeting.Portal(new(21, 5), wildPath, new(22, 37), locked: false);
        kupeiMeeting.Portal(new(22, 37), kupeiFight, new(21, 5), locked: false);
        kupeiMeeting.Spawn(Npc(515).At(21, 22).Escort());
        kupeiMeeting.OnEnter(r => r.Dialog(7326));

        wildPath.Portal(new(21, 5), birdHoldout, new(22, 37), locked: false);
        wildPath.Portal(new(22, 37), kupeiMeeting, new(21, 5), locked: false);
        wildPath.OnEnter(r => r.Dialog(7327));

        var birdHoldoutDoors = new[]
        {
            birdHoldout.Portal(new(21, 5), yakHoldout, new(20, 38)),
            birdHoldout.Portal(new(22, 37), wildPath, new(21, 5))
        };
        birdHoldout.OnEnter(r => r.StartRoomClock(TimeSpan.FromSeconds(180)))
            .Every(thirtySeconds, r => r.Say(Game18NConstString.EnemiesReinforcements), times: 6)
            .Every(thirtySeconds, r => r.Spawn(Monster(182).At(13, 9)), times: 3)
            .Every(thirtySeconds, r => r.Spawn(Monster(179).At(31, 33)), times: 3)
            .Every(thirtySeconds, r => r.Spawn(Monster(182).At(12, 31), Monster(179).At(30, 7)), from: TimeSpan.FromSeconds(60))
            .Every(thirtySeconds, r => r.Spawn(Monster(179).At(12, 19), Monster(182).At(30, 21)), from: TimeSpan.FromSeconds(120))
            .OnClockOut(r => r
                .Unlock(birdHoldoutDoors)
                .Say(Game18NConstString.ADoorHasOpened)
                .Say(Game18NConstString.QuickMissionComplete)
                .Dismiss());

        var yakHoldoutDoors = new[]
        {
            yakHoldout.Portal(new(20, 38), birdHoldout, new(21, 5)),
            yakHoldout.Portal(new(4, 20), lizardPassage, new(38, 20))
        };
        yakHoldout.OnEnter(r => r.StartRoomClock(TimeSpan.FromSeconds(180)))
            .Every(thirtySeconds, r => r.Say(Game18NConstString.EnemiesReinforcements), times: 5)
            .Every(thirtySeconds, r => r.Spawn(Monster(446).At(6, 26), Monster(447).At(27, 29)), times: 3)
            .Every(thirtySeconds, r => r.Spawn(Monster(447).At(8, 12)), from: TimeSpan.FromSeconds(60), times: 3)
            .OnClockOut(r => r
                .Unlock(yakHoldoutDoors)
                .Say(Game18NConstString.ADoorHasOpened)
                .Say(Game18NConstString.QuickMissionComplete)
                .Dismiss());

        lizardPassage.Portal(new(38, 20), yakHoldout, new(4, 20), locked: false);
        lizardPassage.Portal(new(21, 38), lastHoldout, new(22, 4), locked: false);
        lizardPassage.Spawn(Monster(179).At(33, 33).Bonus(), Monster(179).At(19, 33), Monster(179).At(16, 18));
        lizardPassage.OnKills(1, r => r.Spawn(Monster(179).At(31, 17)));

        var lastHoldoutDoors = new[]
        {
            lastHoldout.Portal(new(22, 4), lizardPassage, new(21, 38)),
            lastHoldout.Portal(new(4, 23), angelRoom, new(38, 23))
        };
        lastHoldout.OnEnter(r => r.StartRoomClock(TimeSpan.FromSeconds(180)))
            .Every(thirtySeconds, r => r.Say(Game18NConstString.EnemiesReinforcements), times: 6)
            .Every(thirtySeconds, r => r.Spawn(Monster(182).At(11, 27), Monster(182).At(27, 12)), times: 3)
            .Every(thirtySeconds, r => r.Spawn(Monster(179).At(15, 13)), times: 5)
            .Every(thirtySeconds, r => r.Spawn(Monster(179).At(23, 28)), from: TimeSpan.FromSeconds(60), times: 3)
            .Every(thirtySeconds, r => r.Spawn(Monster(446).At(7, 16), Monster(447).At(30, 28)), from: TimeSpan.FromSeconds(120))
            .OnClockOut(r => r
                .Unlock(lastHoldoutDoors)
                .Say(Game18NConstString.ADoorHasOpened)
                .Say(Game18NConstString.QuickMissionComplete)
                .Dismiss());

        // Angel Israel and Enkidu wait behind the talk; Enkidu leaves once the angel falls.
        var angelRoomDoors = new[]
        {
            angelRoom.Portal(new(22, 4), exitRoom, new(22, 37)),
            angelRoom.Portal(new(38, 23), lastHoldout, new(4, 23))
        };
        angelRoom.ClearedAfter(1, 518);
        angelRoom.OnEnter(r => r.Dialog(7328))
            .OnDialogClosed(7328, r => r
                .Spawn(Monster(520).At(33, 14), Monster(518).At(22, 18).Bonus())
                .Objective(LanguageKey.TIMESPACE_GOAL_KILL_THE_BOSS))
            .OnCleared(r => r
                .Dismiss()
                .Unlock(angelRoomDoors)
                .Say(Game18NConstString.ADoorHasOpened)
                .Say(Game18NConstString.QuickMissionComplete));

        exitRoom.Portal(new(22, 37), angelRoom, new(22, 4), locked: false);
        exitRoom.Exit(new(21, 5), locked: false)
            .OnReached(r => r.Say(Game18NConstString.PleaseWait))
            .WinsAfterDialog(7329);
    }
}

return new TimeSpace405();
