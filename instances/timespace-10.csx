public class TimeSpace10 : TimeSpaceScript
{
    public TimeSpace10() : base(10, "A Bold Warrior: Part 1")
    {
        Levels(20, 99);
        Start(11, 11);
        Requires(1012, 2);
        Lives(3);
        Clock(TimeSpan.FromSeconds(240));
        Objective(ObjectiveKind.UseObject, 1055, 5);
        Draw(135, 1);
        Draw(142, 1);
        Draw(149, 1);
        Special(1031, 2);
        Reward(2028, 2);
        Reward(1087, 1);

        var entrance = Room(4100, 3, 7);
        var briefing = Room(4100, 3, 8);
        var hub = Room(4101, 3, 9);
        var eastPassage = Room(4103, 4, 9);
        var eastSculpture = Room(4107, 5, 9);
        var westLever = Room(4102, 2, 9);
        var westSculpture = Room(4108, 1, 9);
        var lowerHub = Room(4101, 3, 10);
        var lowerEastLever = Room(4105, 4, 10);
        var lowerEastSculpture = Room(4107, 5, 10);
        var lowerWest = Room(4103, 2, 10);
        var lowerWestSculpture = Room(4108, 1, 10);
        var bottomHub = Room(4105, 3, 11);
        var buttonRoom = Room(4109, 4, 11);
        var lastSculpture = Room(4109, 2, 11);
        var way = Room(4110, 1, 11);

        entrance.Portal(new(14, 28), briefing, new(14, 1), locked: false);
        entrance.OnEnter(r => r.Say(Game18NConstString.MissionStartWhenEnterFirstRoom));

        briefing.Portal(new(14, 1), entrance, new(14, 28), locked: false);
        briefing.Portal(new(14, 28), hub, new(14, 1), locked: false);
        briefing.OnEnter(r => r.Say(Game18NConstString.NewGoal).Dialog(6052));

        hub.Portal(new(14, 1), briefing, new(14, 28), locked: false);
        var hubToEast = hub.Portal(new(28, 15), eastPassage, new(1, 15));
        var hubToLower = hub.Portal(new(14, 28), lowerHub, new(14, 1));
        var hubToWest = hub.Portal(new(1, 14), westLever, new(28, 14));
        hub.Spawn(
            Monster(131).At(21, 18),
            Monster(131).At(8, 17).OnDeath(r => r.Spawn(
                Monster(19).At(14, 12).OnDeath(r => r.Spawn(Monster(131).At(12, 19))),
                Monster(131).At(23, 14).Bonus().OnDeath(r => r.Spawn(
                    Monster(57).At(11, 10),
                    Monster(57).At(20, 10).OnDeath(r => r.Spawn(Monster(57).At(15, 17))))))));
        hub.Spawn(Npc(317).At(14, 7));
        hub.OnCleared(r => r
            .Unlock(hubToEast, hubToWest, hubToLower)
            .AddTime(TimeSpan.FromSeconds(100))
            .Say(Game18NConstString.ADoorHasOpened)
            .Dialog(6053));

        eastPassage.Portal(new(28, 15), eastSculpture, new(1, 14), locked: false);
        eastPassage.Portal(new(1, 15), hub, new(28, 15), locked: false);

        var eastSculptureToPassage = eastSculpture.Portal(new(1, 14), eastPassage, new(28, 15));
        eastSculpture.OnEnter(r => r.Say(LanguageKey.TIMESPACE_SEALED_SCULPTURE))
            .OnCleared(r => r
                .Unlock(eastSculptureToPassage)
                .AddTime(TimeSpan.FromSeconds(100))
                .Say(Game18NConstString.ADoorHasOpened)
                .Say(Game18NConstString.QuickMissionComplete));
        eastSculpture.Sculpture(new(20, 15), 1055, 1056)
            .OnUse(r => r
                .Say(Game18NConstString.ObjectHasBeenMoved)
                .Say(Game18NConstString.NewGoal)
                .Dialog(6055)
                .Objective(LanguageKey.TIMESPACE_GOAL_KILL_ALL_ENEMIES)
                .Spawn(
                    Monster(129).At(25, 19),
                    Monster(129).At(25, 17).Bonus(),
                    Monster(129).At(22, 22),
                    Monster(129).At(19, 23),
                    Monster(56).At(16, 16),
                    Monster(129).At(14, 21),
                    Monster(129).At(12, 15)));

        westLever.Portal(new(28, 14), hub, new(1, 14), locked: false);
        var westLeverToSculpture = westLever.Portal(new(1, 14), westSculpture, new(28, 15));
        westLever.Lever(new(15, 15))
            .OnUse(r => r
                .Unlock(westLeverToSculpture)
                .Say(Game18NConstString.ADoorHasOpened)
                .Say(Game18NConstString.NewGoal));
        westLever.Spawn(Monster(18).At(21, 9).OnDeath(r => r.Spawn(Monster(131).At(8, 12).Bonus())));

        var westSculptureToLever = westSculpture.Portal(new(28, 15), westLever, new(1, 14));
        westSculpture.OnEnter(r => r.Say(LanguageKey.TIMESPACE_SEALED_SCULPTURE))
            .OnCleared(r => r
                .Unlock(westSculptureToLever)
                .AddTime(TimeSpan.FromSeconds(150))
                .Say(Game18NConstString.ADoorHasOpened)
                .Say(Game18NConstString.QuickMissionComplete));
        westSculpture.Sculpture(new(10, 15), 1055, 1056)
            .OnUse(r => r
                .Say(Game18NConstString.ObjectHasBeenMoved)
                .Say(Game18NConstString.NewGoal)
                .Dialog(6054)
                .Objective(LanguageKey.TIMESPACE_GOAL_KILL_ALL_ENEMIES)
                .Spawn(
                    Monster(130).At(15, 15),
                    Monster(130).At(15, 8).Bonus().OnDeath(r => r.Spawn(
                        Monster(57).At(18, 20).OnDeath(r => r.Spawn(
                            Monster(57).At(18, 13).OnDeath(r => r.Spawn(Monster(57).At(12, 9))))))),
                    Monster(130).At(13, 18),
                    Monster(18).At(7, 11)));

        lowerHub.Portal(new(14, 1), hub, new(14, 28), locked: false);
        lowerHub.Portal(new(28, 15), lowerEastLever, new(1, 14), locked: false);
        var lowerHubToBottom = lowerHub.Portal(new(14, 28), bottomHub, new(14, 1));
        lowerHub.Portal(new(1, 14), lowerWest, new(28, 15), locked: false);

        var lowerEastLeverToSculpture = lowerEastLever.Portal(new(28, 15), lowerEastSculpture, new(1, 14));
        lowerEastLever.Portal(new(1, 14), lowerHub, new(28, 15), locked: false);
        lowerEastLever.Lever(new(18, 11))
            .OnUse(r => r
                .Unlock(lowerEastLeverToSculpture)
                .Say(Game18NConstString.ADoorHasOpened)
                .Say(Game18NConstString.NewGoal));
        lowerEastLever.Spawn(
            Monster(129).At(17, 8),
            Monster(129).At(16, 5).Bonus(),
            Monster(129).At(10, 4),
            Monster(129).At(7, 14));

        var lowerEastSculptureToLever = lowerEastSculpture.Portal(new(1, 14), lowerEastLever, new(28, 15));
        lowerEastSculpture.OnEnter(r => r.Say(LanguageKey.TIMESPACE_SEALED_SCULPTURE))
            .OnCleared(r => r
                .Unlock(lowerEastSculptureToLever)
                .AddTime(TimeSpan.FromSeconds(120))
                .Say(Game18NConstString.ADoorHasOpened)
                .Say(Game18NConstString.QuickMissionComplete));
        lowerEastSculpture.Sculpture(new(20, 15), 1055, 1056)
            .OnUse(r => r
                .Say(Game18NConstString.ObjectHasBeenMoved)
                .Say(Game18NConstString.NewGoal)
                .Dialog(6056)
                .Objective(LanguageKey.TIMESPACE_GOAL_KILL_ALL_ENEMIES)
                .Spawn(
                    Monster(57).At(17, 16).OnDeath(r => r.Spawn(Monster(57).At(7, 12))),
                    Monster(57).At(17, 11).OnDeath(r => r.Spawn(
                        Monster(57).At(14, 19).OnDeath(r => r.Spawn(
                            Monster(57).At(13, 8).OnDeath(r => r.Spawn(
                                Monster(102).At(19, 19).OnDeath(r => r.Spawn(Monster(136).At(15, 14)))))))))));

        lowerWest.Portal(new(28, 15), lowerHub, new(1, 14), locked: false);
        lowerWest.Portal(new(1, 15), lowerWestSculpture, new(28, 15), locked: false);
        lowerWest.Spawn(
            Monster(19).At(25, 20).Bonus(),
            Monster(19).At(25, 9),
            Monster(19).At(22, 20),
            Monster(19).At(22, 9),
            Monster(19).At(19, 20),
            Monster(19).At(19, 9),
            Monster(19).At(17, 20),
            Monster(19).At(17, 9),
            Monster(19).At(12, 20),
            Monster(19).At(12, 9),
            Monster(19).At(10, 20),
            Monster(19).At(10, 9).Bonus(),
            Monster(19).At(7, 20),
            Monster(19).At(7, 9),
            Monster(19).At(4, 20),
            Monster(19).At(4, 9).Bonus());

        var lowerWestSculptureToWest = lowerWestSculpture.Portal(new(28, 15), lowerWest, new(1, 15));
        lowerWestSculpture.OnEnter(r => r.Say(LanguageKey.TIMESPACE_SEALED_SCULPTURE))
            .OnCleared(r => r
                .Unlock(lowerWestSculptureToWest)
                .AddTime(TimeSpan.FromSeconds(120))
                .Say(Game18NConstString.ADoorHasOpened)
                .Say(Game18NConstString.QuickMissionComplete)
                .Unlock(lowerHubToBottom));
        lowerWestSculpture.Sculpture(new(10, 15), 1055, 1056)
            .OnUse(r => r
                .Say(Game18NConstString.ObjectHasBeenMoved)
                .Say(Game18NConstString.NewGoal)
                .Dialog(6057)
                .Objective(LanguageKey.TIMESPACE_GOAL_KILL_ALL_ENEMIES)
                .Spawn(
                    Monster(129).At(9, 22).OnDeath(r => r.Spawn(
                        Monster(57).At(11, 10).OnDeath(r => r.Spawn(Monster(129).At(13, 12))))),
                    Monster(129).At(9, 7).OnDeath(r => r.Spawn(Monster(57).At(10, 20))),
                    Monster(19).At(7, 13).Bonus(),
                    Monster(129).At(5, 17),
                    Monster(129).At(5, 12)));

        bottomHub.Portal(new(14, 1), lowerHub, new(14, 28), locked: false);
        bottomHub.Portal(new(28, 15), buttonRoom, new(1, 15), locked: false);
        bottomHub.Portal(new(1, 14), lastSculpture, new(28, 15), locked: false);

        var buttonRoomToHub = buttonRoom.Portal(new(1, 15), bottomHub, new(28, 15));
        buttonRoom.OnEnter(r => r.Say(LanguageKey.TIMESPACE_STRANGE_BUTTON))
            .OnCleared(r => r
                .Unlock(buttonRoomToHub)
                .Say(Game18NConstString.ADoorHasOpened)
                .Say(Game18NConstString.QuickMissionComplete));
        buttonRoom.Lever(new(15, 15), 1051, 1052)
            .OnUse(r => r
                .Spawn(
                    Monster(136).At(14, 11),
                    Monster(136).At(10, 15),
                    Monster(19).At(10, 11).OnDeath(r => r.Spawn(
                        Monster(136).At(15, 17).OnDeath(r => r.Spawn(Monster(58).At(20, 20))))))
                .Objective(LanguageKey.TIMESPACE_GOAL_BUTTON_SUMMONED_ENEMIES)
                .Say(Game18NConstString.NewGoal));

        var lastSculptureToHub = lastSculpture.Portal(new(28, 15), bottomHub, new(1, 14));
        var lastSculptureToWay = lastSculpture.Portal(new(1, 15), way, new(28, 15));
        lastSculpture.OnEnter(r => r.Say(LanguageKey.TIMESPACE_SEALED_SCULPTURE_AND_EXIT))
            .OnCleared(r => r
                .Unlock(lastSculptureToHub, lastSculptureToWay)
                .Say(Game18NConstString.ADoorHasOpened)
                .Say(Game18NConstString.QuickMissionComplete));
        lastSculpture.Sculpture(new(15, 15), 1055, 1056)
            .OnUse(r => r
                .Say(Game18NConstString.ObjectHasBeenMoved)
                .Say(Game18NConstString.NewGoal)
                .Dialog(6058)
                .Objective(LanguageKey.TIMESPACE_GOAL_KILL_ALL_ENEMIES)
                .Spawn(
                    Monster(57).At(12, 10).OnDeath(r => r.Spawn(
                        Monster(57).At(16, 10).OnDeath(r => r.Spawn(
                            Monster(57).At(14, 21).OnDeath(r => r.Spawn(Monster(136).At(11, 14), Monster(136).At(20, 14))))))),
                    Monster(57).At(8, 17).OnDeath(r => r.Spawn(Monster(57).At(22, 17)))));

        var wayToLastSculpture = way.Portal(new(28, 15), lastSculpture, new(1, 15));
        var exit = way.Exit(new(1, 15));
        way.OnEnter(r => r.Dialog(6060).Say(LanguageKey.TIMESPACE_EXIT_OPENED_GET_OUT))
            .OnDialogClosed(6060, r => r.Spawn(
                Monster(130).At(14, 11),
                Monster(130).At(13, 21),
                Monster(130).At(13, 15).Bonus(),
                Monster(130).At(11, 19),
                Monster(130).At(11, 17),
                Monster(130).At(10, 11)))
            .OnCleared(r => r
                .Unlock(exit)
                .Unlock(wayToLastSculpture)
                .Say(Game18NConstString.ADoorHasOpened)
                .Dialog(6061));
    }
}

return new TimeSpace10();
