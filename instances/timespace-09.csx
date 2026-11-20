public class TimeSpace9 : TimeSpaceScript
{
    public TimeSpace9() : base(9, "The Kovolt Riot")
    {
        Levels(18, 99);
        Start(8, 8);
        Requires(1012, 1);
        Lives(4);
        Clock(TimeSpan.FromSeconds(500));
        Objective(ObjectiveKind.KillMonster, 109, 1);
        Draw(162, 1);
        Draw(168, 1);
        Draw(174, 1);
        Special(1016, 1);
        Reward(721, 1);

        var entrance = Room(4014, 3, 2);
        var picket = Room(4004, 4, 2);
        var hall = Room(4004, 5, 2);
        var junction = Room(4000, 6, 2);
        var eastHall = Room(4004, 7, 2);
        var eastLever = Room(4016, 8, 2);
        var shaft = Room(4006, 6, 3);
        var lowerShaft = Room(4006, 6, 4);
        var crossroads = Room(4012, 6, 5);
        var southHall = Room(4006, 6, 6);
        var southLever = Room(4013, 6, 7);
        var gallery = Room(4005, 7, 5);
        var farGallery = Room(4005, 8, 5);
        var galleryLever = Room(4016, 9, 5);
        var westHall = Room(4005, 5, 5);
        var westJunction = Room(4002, 4, 5);
        var northLever = Room(4015, 4, 4);
        var westGallery = Room(4000, 3, 5);
        var westLever = Room(4014, 2, 5);
        var descent = Room(4006, 3, 6);
        var lowerDescent = Room(4010, 3, 7);
        var antechamber = Room(4005, 4, 7);
        var den = Room(2102, 5, 7);

        entrance.Portal(new(17, 9), picket, new(0, 9), locked: false);
        entrance.OnEnter(r => r.Say(Game18NConstString.MissionStartWhenEnterFirstRoom));

        var picketToHall = picket.Portal(new(17, 9), hall, new(0, 9));
        picket.Portal(new(0, 9), entrance, new(17, 9));
        picket.OnEnter(r => r.Dialog(6175).Say(LanguageKey.TIMESPACE_MINERS_PROTESTING))
            .OnDialogClosed(6175, r => r
                .Spawn(Monster(105).At(6, 8), Monster(105).At(3, 10))
                .Objective(LanguageKey.TIMESPACE_GOAL_KILL_ALL_ENEMIES))
            .OnCleared(r => r
                .Unlock(picketToHall)
                .Say(Game18NConstString.ADoorHasOpened)
                .Say(Game18NConstString.QuickMissionComplete));

        hall.Portal(new(17, 9), junction, new(0, 9), locked: false);
        hall.Portal(new(0, 9), picket, new(17, 9), locked: false);
        hall.Spawn(Monster(105).At(15, 10), Monster(108).At(9, 9), Monster(108).At(1, 8));

        var junctionToEastHall = junction.Portal(new(17, 9), eastHall, new(0, 9));
        var junctionToShaft = junction.Portal(new(8, 17), shaft, new(8, 0));
        var junctionToHall = junction.Portal(new(0, 9), hall, new(17, 9));
        junction.OnEnter(r => r.Dialog(6176))
            .OnDialogClosed(6176, r => r
                .Spawn(Monster(105).At(5, 10), Monster(105).At(2, 9))
                .Objective(LanguageKey.TIMESPACE_GOAL_KILL_ALL_ENEMIES))
            .OnCleared(r => r
                .Unlock(junctionToEastHall, junctionToHall)
                .Say(Game18NConstString.ADoorHasOpened)
                .Say(Game18NConstString.QuickMissionComplete));

        eastHall.Portal(new(17, 9), eastLever, new(0, 9), locked: false);
        eastHall.Portal(new(0, 9), junction, new(17, 9), locked: false);
        eastHall.Spawn(
            Monster(105).At(7, 7).OnDeath(r => r.Spawn(Monster(105).At(14, 9))),
            Monster(105).At(3, 9).OnDeath(r => r.Spawn(Monster(108).At(10, 9).Bonus(), Monster(108).At(12, 8))));

        var eastLeverToEastHall = eastLever.Portal(new(0, 9), eastHall, new(17, 9));
        eastLever.Lever(new(9, 9)).OnUse(r => r
            .Say(Game18NConstString.NewGoal)
            .Unlock(junctionToShaft));
        eastLever.OnEnter(r => r.Dialog(6177))
            .OnDialogClosed(6177, r => r
                .Spawn(Monster(105).At(5, 10), Monster(105).At(4, 8))
                .Objective(LanguageKey.TIMESPACE_GOAL_KILL_ALL_ENEMIES))
            .OnCleared(r => r
                .Unlock(eastLeverToEastHall)
                .AddTime(TimeSpan.FromSeconds(200))
                .Say(Game18NConstString.ADoorHasOpened)
                .Say(Game18NConstString.QuickMissionComplete)
                .Say(LanguageKey.TIMESPACE_DEAD_END_TURN_BACK));

        shaft.Portal(new(8, 0), junction, new(8, 17), locked: false);
        shaft.Portal(new(8, 17), lowerShaft, new(8, 0), locked: false);
        shaft.Spawn(Monster(108).At(10, 10), Monster(108).At(9, 4));

        lowerShaft.Portal(new(8, 0), shaft, new(8, 17), locked: false);
        lowerShaft.Portal(new(8, 17), crossroads, new(8, 0), locked: false);
        lowerShaft.Spawn(Monster(105).At(10, 5), Monster(105).At(8, 7).Bonus());

        var crossroadsToLowerShaft = crossroads.Portal(new(8, 0), lowerShaft, new(8, 17));
        var crossroadsToGallery = crossroads.Portal(new(17, 9), gallery, new(0, 9));
        var crossroadsToSouthHall = crossroads.Portal(new(8, 17), southHall, new(8, 0));
        var crossroadsToWestHall = crossroads.Portal(new(0, 9), westHall, new(17, 9));
        crossroads.OnEnter(r => r.Dialog(6178).Say(LanguageKey.TIMESPACE_MINERS_PROTESTING))
            .OnDialogClosed(6178, r => r
                .Spawn(Monster(105).At(4, 11).OnDeath(r => r.Spawn(Monster(108).At(8, 10))), Monster(105).At(4, 9))
                .Objective(LanguageKey.TIMESPACE_GOAL_KILL_ALL_ENEMIES))
            .OnCleared(r => r
                .Unlock(crossroadsToLowerShaft, crossroadsToSouthHall)
                .AddTime(TimeSpan.FromSeconds(150))
                .Say(Game18NConstString.ADoorHasOpened)
                .Say(Game18NConstString.QuickMissionComplete));

        var southHallToCrossroads = southHall.Portal(new(8, 0), crossroads, new(8, 17));
        var southHallToSouthLever = southHall.Portal(new(8, 17), southLever, new(8, 0));
        southHall.OnEnter(r => r.Dialog(6179))
            .OnDialogClosed(6179, r => r
                .Spawn(
                    Monster(105).At(10, 5),
                    Monster(105).At(7, 5).OnDeath(r => r.Spawn(
                        Monster(105).At(8, 10).OnDeath(r => r.Spawn(Monster(105).At(10, 16))))))
                .Objective(LanguageKey.TIMESPACE_GOAL_KILL_ALL_ENEMIES))
            .OnCleared(r => r
                .Unlock(southHallToCrossroads, southHallToSouthLever)
                .Say(Game18NConstString.ADoorHasOpened)
                .Say(Game18NConstString.QuickMissionComplete));

        var southLeverToSouthHall = southLever.Portal(new(8, 0), southHall, new(8, 17));
        southLever.Lever(new(9, 9)).OnUse(r => r
            .Say(Game18NConstString.NewGoal)
            .Unlock(crossroadsToGallery));
        southLever.OnEnter(r => r.Dialog(6180))
            .OnDialogClosed(6180, r => r
                .Spawn(Monster(105).At(10, 5).Bonus(), Monster(105).At(8, 5).OnDeath(r => r.Spawn(Monster(108).At(10, 8))))
                .Objective(LanguageKey.TIMESPACE_GOAL_KILL_ALL_ENEMIES))
            .OnCleared(r => r
                .Unlock(southLeverToSouthHall)
                .AddTime(TimeSpan.FromSeconds(150))
                .Say(Game18NConstString.ADoorHasOpened)
                .Say(Game18NConstString.QuickMissionComplete));

        gallery.Portal(new(17, 9), farGallery, new(0, 9), locked: false);
        gallery.Portal(new(0, 9), crossroads, new(17, 9), locked: false);
        gallery.Spawn(Monster(108).At(1, 9).Bonus().OnDeath(r => r.Spawn(Monster(108).At(6, 8))));

        farGallery.Portal(new(17, 9), galleryLever, new(0, 9), locked: false);
        farGallery.Portal(new(0, 9), gallery, new(17, 9), locked: false);
        farGallery.Spawn(
            Monster(105).At(5, 8).OnDeath(r => r.Spawn(Monster(108).At(12, 9), Monster(108).At(16, 8).Bonus())),
            Monster(105).At(3, 10));

        var galleryLeverToFarGallery = galleryLever.Portal(new(0, 9), farGallery, new(17, 9));
        galleryLever.Lever(new(9, 9)).OnUse(r => r.Say(Game18NConstString.NewGoal));
        galleryLever.OnEnter(r => r.Dialog(6181))
            .OnDialogClosed(6181, r => r.Spawn(Monster(108).At(5, 8), Monster(105).At(4, 10)))
            .OnCleared(r => r
                .Unlock(galleryLeverToFarGallery)
                .AddTime(TimeSpan.FromSeconds(150))
                .Say(Game18NConstString.ADoorHasOpened)
                .Dialog(6182)
                .Unlock(crossroadsToWestHall));

        westHall.Portal(new(17, 9), crossroads, new(0, 9), locked: false);
        westHall.Portal(new(0, 9), westJunction, new(17, 9), locked: false);
        westHall.Spawn(Monster(106).At(4, 7).OnDeath(r => r.Spawn(Monster(106).At(3, 9))));

        var westJunctionToNorthLever = westJunction.Portal(new(8, 0), northLever, new(8, 17));
        westJunction.Portal(new(17, 9), westHall, new(0, 9));
        var westJunctionToWestGallery = westJunction.Portal(new(0, 9), westGallery, new(17, 9));
        westJunction.OnEnter(r => r.Dialog(6183))
            .OnDialogClosed(6183, r => r
                .Spawn(Monster(105).At(13, 9), Monster(108).At(8, 7).OnDeath(r => r.Spawn(Monster(105).At(10, 5))))
                .Objective(LanguageKey.TIMESPACE_GOAL_KILL_ALL_ENEMIES))
            .OnCleared(r => r
                .Unlock(westJunctionToNorthLever)
                .Say(Game18NConstString.ADoorHasOpened)
                .Say(Game18NConstString.QuickMissionComplete));

        var northLeverToWestJunction = northLever.Portal(new(8, 17), westJunction, new(8, 0));
        northLever.Lever(new(8, 7)).OnUse(r => r
            .AddTime(TimeSpan.FromSeconds(167))
            .Say(Game18NConstString.NewGoal)
            .Unlock(westJunctionToWestGallery));
        northLever.OnEnter(r => r.Dialog(6184))
            .OnDialogClosed(6184, r => r
                .Spawn(Monster(105).At(7, 12), Monster(105).At(7, 10))
                .Objective(LanguageKey.TIMESPACE_GOAL_KILL_ALL_ENEMIES))
            .OnCleared(r => r
                .Unlock(northLeverToWestJunction)
                .Say(Game18NConstString.ADoorHasOpened)
                .Say(Game18NConstString.QuickMissionComplete));

        westGallery.Portal(new(17, 9), westJunction, new(0, 9), locked: false);
        var westGalleryToDescent = westGallery.Portal(new(8, 17), descent, new(8, 0));
        westGallery.Portal(new(0, 9), westLever, new(17, 9), locked: false);
        westGallery.Spawn(
            Monster(105).At(16, 8),
            Monster(105).At(14, 9).OnDeath(r => r.Spawn(Monster(105).At(3, 9))));

        var westLeverToWestGallery = westLever.Portal(new(17, 9), westGallery, new(0, 9));
        westLever.Lever(new(7, 7)).OnUse(r => r
            .AddTime(TimeSpan.FromSeconds(142))
            .Say(Game18NConstString.NewGoal)
            .Unlock(westGalleryToDescent));
        westLever.OnEnter(r => r.Dialog(6185))
            .OnDialogClosed(6185, r => r
                .Spawn(Monster(105).At(10, 9).OnDeath(r => r.Spawn(Monster(105).At(13, 9))), Monster(105).At(8, 8))
                .Objective(LanguageKey.TIMESPACE_GOAL_KILL_ALL_ENEMIES))
            .OnCleared(r => r
                .Unlock(westLeverToWestGallery)
                .Say(Game18NConstString.ADoorHasOpened)
                .Say(Game18NConstString.QuickMissionComplete));

        descent.Portal(new(8, 0), westGallery, new(8, 17), locked: false);
        descent.Portal(new(8, 17), lowerDescent, new(8, 0), locked: false);
        descent.Spawn(Monster(106).At(11, 5).OnDeath(r => r.Spawn(Monster(106).At(8, 10))));

        lowerDescent.Portal(new(8, 0), descent, new(8, 17), locked: false);
        lowerDescent.Portal(new(17, 9), antechamber, new(0, 9), locked: false);
        lowerDescent.Spawn(Monster(105).At(9, 6), Monster(105).At(9, 2));

        antechamber.Portal(new(17, 9), den, new(7, 19), locked: false);
        antechamber.Portal(new(0, 9), lowerDescent, new(17, 9), locked: false);
        antechamber.Spawn(
            Monster(105).At(17, 10),
            Monster(105).At(15, 9),
            Monster(106).At(13, 8).Bonus(),
            Monster(108).At(10, 8));

        den.Portal(new(7, 19), antechamber, new(17, 9));
        den.OnEnter(r => r.Dialog(6187))
            .OnDialogClosed(6187, r => r
                .Spawn(
                    Monster(105).At(29, 19).Bonus(),
                    Monster(105).At(26, 15).OnDeath(r => r.Spawn(Monster(109).At(21, 18))),
                    Monster(105).At(25, 13),
                    Monster(105).At(10, 19),
                    Monster(105).At(10, 8))
                .Objective(LanguageKey.TIMESPACE_GOAL_KILL_ALL_ENEMIES))
            .OnCleared(r => r
                .Say(Game18NConstString.QuickMissionComplete)
                .Say(Game18NConstString.NewGoal)
                .Dialog(6188)
                .Succeed());
    }
}

return new TimeSpace9();
