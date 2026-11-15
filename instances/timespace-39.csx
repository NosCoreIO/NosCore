// Fernon's Seal
public class TimeSpace39 : InstanceScript
{
    private readonly int _room1;
    private readonly int _room2;
    private readonly int _room3;
    private readonly int _room4;
    private readonly int _room5;
    private readonly int _room6;
    private readonly int _room7;
    private readonly int _room8;
    private readonly int _room9;
    private readonly int _room10;
    private readonly int _room11;
    private readonly int _room12;
    private readonly int _room13;
    private readonly int _room14;
    private readonly int _room15;
    private readonly int _room16;
    private readonly int _room17;
    private readonly int _room18;
    private readonly int _room19;
    private readonly int _room20;
    private readonly int _room21;
    private readonly int _room22;
    private readonly int _room23;
    private readonly int _room24;
    private readonly int _room25;
    private readonly int _room26;
    private readonly int _room27;
    private readonly int _room28;
    private readonly int _room29;
    private readonly int _room30;
    private readonly int _room31;
    private readonly int _room32;
    private readonly int _room33;
    private readonly int _room34;
    private readonly int _room35;
    private readonly int _room36;
    private readonly int _room37;
    private readonly int _room38;
    private readonly int _room39;

    public TimeSpace39()
    {
        Definition = InstanceDefinitionBuilder.Named(39, "To save Kenel you must reach Kliff quickly! Learn the story of Fernon's legendary tree and the tales of the demons trying to break its seal. What will happen?", "Fernon's Seal")
            .ForLevels(49, 99)
            .StartingAt(6, 11)
            .WithLives(2)
            .Requiring(1012, 4)
            .WithDraw(226, 1)
            .WithDraw(239, 1)
            .WithDraw(240, 1)
            .WithDraw(241, 1)
            .WithDraw(242, 1)
            .WithSpecialReward(2036, 15)
            .WithReward(2127, 1)
            .WithReward(1087, 1)
            .WithRoom(4418, out _room1, 0, 1)
            .WithRoom(4409, out _room2, 0, 1)
            .WithRoom(4409, out _room3, 1, 1)
            .WithRoom(4405, out _room4, 2, 1)
            .WithRoom(4412, out _room5, 3, 1)
            .WithRoom(4403, out _room6, 3, 2)
            .WithRoom(4401, out _room7, 3, 3)
            .WithRoom(4400, out _room8, 4, 3)
            .WithRoom(4401, out _room9, 4, 4)
            .WithRoom(4400, out _room10, 3, 4)
            .WithRoom(4403, out _room11, 5, 3)
            .WithRoom(4400, out _room12, 5, 4)
            .WithRoom(4419, out _room13, 6, 4)
            .WithRoom(4404, out _room14, 6, 3)
            .WithRoom(4408, out _room15, 6, 5)
            .WithRoom(4400, out _room16, 5, 5)
            .WithRoom(4406, out _room17, 5, 6)
            .WithRoom(4418, out _room18, 5, 7)
            .WithRoom(4406, out _room19, 4, 7)
            .WithRoom(4410, out _room20, 4, 6)
            .WithRoom(4400, out _room21, 4, 5)
            .WithRoom(4406, out _room22, 3, 6)
            .WithRoom(4411, out _room23, 3, 7)
            .WithRoom(4418, out _room24, 2, 7)
            .WithRoom(4411, out _room25, 1, 7)
            .WithRoom(4401, out _room26, 3, 5)
            .WithRoom(4401, out _room27, 2, 5)
            .WithRoom(4408, out _room28, 2, 4)
            .WithRoom(4410, out _room29, 1, 4)
            .WithRoom(4407, out _room30, 1, 5)
            .WithRoom(4411, out _room31, 1, 6)
            .WithRoom(4400, out _room32, 2, 6)
            .WithRoom(4407, out _room33, 6, 6)
            .WithRoom(4411, out _room34, 6, 7)
            .WithRoom(4411, out _room35, 7, 7)
            .WithRoom(4409, out _room36, 8, 7)
            .WithRoom(4420, out _room37, 9, 7)
            .WithRoom(4409, out _room38, 10, 7)
            .WithRoom(4411, out _room39, 2, 3)
            .Build();
    }

    public override void OnFirstEnter(IInstanceRun r)
    {
        r.SpawnPortal(_room1, new(23, 12), _room2, new(1, 11), locked: false);
        r.SpawnPortal(_room2, new(1, 11), _room1, new(23, 12), locked: false);
        r.SpawnPortal(_room2, new(23, 11), _room3, new(1, 11), locked: false);
        r.SpawnPortal(_room3, new(1, 11), _room2, new(23, 11), locked: false);
        r.SpawnPortal(_room3, new(23, 11), _room4, new(1, 12), locked: false);
        r.SpawnPortal(_room4, new(1, 12), _room3, new(23, 11));
        r.SpawnPortal(_room4, new(12, 23), _room5, new(11, 1));
        r.Summon(_room4,
            new MonsterSpawn(339, new(11, 12)),
            new MonsterSpawn(311, new(12, 7)),
            new MonsterSpawn(311, new(13, 8), IsBonus: true, OnDeath: r =>
            {
                r.Dialog(_room4, 6217);
                r.Message(Game18NConstString.ADoorHasOpened);
                r.UnlockPortal(_room4, _room3);
                r.UnlockPortal(_room4, _room5);
                r.Dismiss(_room4, 311);
                r.Dismiss(_room4, 339);
                r.Summon(_room4,
                new MonsterSpawn(311, new(17, 16)),
                new MonsterSpawn(311, new(14, 18)),
                new MonsterSpawn(311, new(8, 7)));
            }),
            new MonsterSpawn(311, new(16, 12)));
        r.SpawnPortal(_room5, new(11, 23), _room6, new(12, 1), locked: false);
        r.SpawnPortal(_room5, new(11, 1), _room4, new(12, 23), locked: false);
        r.SpawnPortal(_room6, new(23, 12), _room7, new(1, 11), locked: false);
        r.SpawnPortal(_room6, new(12, 1), _room5, new(11, 23), locked: false);
        r.SpawnPortal(_room7, new(1, 11), _room6, new(23, 12), locked: false);
        r.SpawnPortal(_room7, new(12, 23), _room8, new(12, 1), locked: false);
        r.Summon(_room7,
            new MonsterSpawn(253, new(8, 15)),
            new MonsterSpawn(253, new(18, 14)),
            new MonsterSpawn(253, new(14, 20), IsBonus: true));
        r.SpawnPortal(_room8, new(1, 12), _room9, new(23, 11), locked: false);
        r.SpawnPortal(_room8, new(12, 1), _room7, new(12, 23), locked: false);
        r.SpawnPortal(_room9, new(23, 11), _room10, new(1, 12), locked: false);
        r.Summon(_room9,
            new MonsterSpawn(87, new(16, 14)),
            new MonsterSpawn(253, new(13, 15), IsBonus: true),
            new MonsterSpawn(87, new(14, 10), IsBonus: true, OnDeath: r =>
            {
                r.Summon(_room9,
                new MonsterSpawn(87, new(10, 18), IsBonus: true),
                new MonsterSpawn(253, new(10, 14), IsBonus: true));
            }));
        r.SpawnPortal(_room10, new(1, 12), _room9, new(23, 11), locked: false);
        r.SpawnPortal(_room10, new(12, 1), _room10, new(12, 1), locked: false);
        r.SpawnPortal(_room11, new(1, 12), _room12, new(23, 12), locked: false);
        r.SpawnPortal(_room11, new(12, 1), _room10, new(12, 0), locked: false);
        r.Summon(_room11,
            new MonsterSpawn(253, new(4, 5), IsBonus: true),
            new MonsterSpawn(253, new(13, 7), IsBonus: true, OnDeath: r =>
            {
                r.Summon(_room11, new MonsterSpawn(253, new(11, 14), IsBonus: true));
            }));
        r.SpawnPortal(_room12, new(23, 12), _room11, new(1, 12));
        r.SpawnPortal(_room12, new(12, 1), _room12, new(12, 1), locked: false);
        r.SpawnPortal(_room13, new(12, 23), _room14, new(12, 1));
        r.PlaceButton(_room13, 1, new(11, 16), 1000, 1045);
        r.Summon(_room13,
            new MonsterSpawn(253, new(10, 12)),
            new MonsterSpawn(253, new(15, 11)),
            new MonsterSpawn(253, new(16, 16)));
        r.SpawnPortal(_room14, new(12, 23), _room14, new(12, 23), locked: false);
        r.SpawnPortal(_room14, new(12, 1), _room13, new(12, 23), locked: false);
        r.SpawnPortal(_room15, new(12, 23), _room16, new(12, 1), locked: false);
        r.SpawnPortal(_room15, new(23, 12), _room14, new(1, 0), locked: false);
        r.SpawnPortal(_room16, new(12, 23), _room17, new(12, 1), locked: false);
        r.SpawnPortal(_room16, new(12, 1), _room15, new(12, 23), locked: false);
        r.Summon(_room16,
            new MonsterSpawn(62, new(3, 10), OnDeath: r =>
            {
                r.Summon(_room16,
                new MonsterSpawn(253, new(13, 8)),
                new MonsterSpawn(62, new(12, 11)));
            }),
            new MonsterSpawn(62, new(6, 13), IsBonus: true),
            new MonsterSpawn(62, new(19, 4), OnDeath: r =>
            {
                r.Summon(_room16, new MonsterSpawn(253, new(10, 14)));
            }),
            new MonsterSpawn(62, new(15, 4)));
        r.SpawnPortal(_room17, new(1, 12), _room18, new(23, 12), locked: false);
        r.SpawnPortal(_room17, new(12, 1), _room16, new(12, 23), locked: false);
        r.Summon(_room17,
            new MonsterSpawn(253, new(13, 8), IsBonus: true),
            new MonsterSpawn(253, new(10, 14), IsBonus: true),
            new MonsterSpawn(132, new(9, 12), IsBonus: true),
            new MonsterSpawn(132, new(15, 8), IsBonus: true),
            new MonsterSpawn(104, new(12, 14), IsBonus: true),
            new MonsterSpawn(104, new(7, 6)),
            new MonsterSpawn(104, new(13, 7)));
        r.SpawnPortal(_room18, new(23, 12), _room19, new(1, 12));
        r.Summon(_room18, new MonsterSpawn(254, new(13, 11), OnDeath: r =>
            {
                r.StopRoomClock(_room18);
                r.Message(Game18NConstString.ADoorHasOpened);
                r.UnlockPortal(_room18, _room19);
            }));
        r.SpawnPortal(_room19, new(1, 12), _room18, new(23, 12), locked: false);
        r.SpawnPortal(_room19, new(12, 1), _room19, new(12, 1), locked: false);
        r.SpawnPortal(_room20, new(12, 23), _room19, new(12, 0));
        r.SpawnPortal(_room20, new(12, 1), _room21, new(12, 23));
        r.PlaceButton(_room20, 1, new(13, 9), 1057, 1057);
        r.PlaceButton(_room20, 2, new(9, 9), 1057, 1057);
        r.PlaceButton(_room20, 3, new(17, 9), 1057, 1057);
        r.SpawnPortal(_room21, new(12, 23), _room20, new(12, 1));
        r.SpawnPortal(_room21, new(12, 1), _room21, new(12, 1), locked: false);
        r.SpawnPortal(_room22, new(1, 12), _room23, new(23, 11), locked: false);
        r.SpawnPortal(_room22, new(12, 1), _room21, new(12, 0), locked: false);
        r.Summon(_room22,
            new MonsterSpawn(132, new(15, 8), IsBonus: true, OnDeath: r =>
            {
                r.Summon(_room22, new MonsterSpawn(132, new(9, 12), IsBonus: true, OnDeath: r =>
                {
                    r.Summon(_room22, new MonsterSpawn(253, new(10, 14), IsBonus: true));
                }));
            }),
            new MonsterSpawn(104, new(15, 18), OnDeath: r =>
            {
                r.Summon(_room22, new MonsterSpawn(253, new(13, 8), IsBonus: true));
            }),
            new MonsterSpawn(104, new(11, 9)),
            new MonsterSpawn(104, new(13, 6), IsBonus: true));
        r.SpawnPortal(_room23, new(1, 11), _room24, new(23, 12), locked: false);
        r.SpawnPortal(_room23, new(23, 11), _room22, new(1, 12), locked: false);
        r.SpawnPortal(_room24, new(23, 12), _room25, new(1, 11));
        r.Summon(_room24,
            new MonsterSpawn(253, new(13, 8), IsBonus: true, OnDeath: r =>
            {
                r.Message(Game18NConstString.ADoorHasOpened);
                r.UnlockPortal(_room24, _room25);
            }),
            new MonsterSpawn(75, new(16, 10)),
            new MonsterSpawn(132, new(12, 6), IsBonus: true),
            new MonsterSpawn(132, new(15, 6), IsBonus: true),
            new MonsterSpawn(75, new(14, 15), IsBonus: true),
            new MonsterSpawn(75, new(12, 11), IsBonus: true),
            new MonsterSpawn(62, new(17, 12), IsBonus: true),
            new MonsterSpawn(62, new(21, 9)));
        r.SpawnPortal(_room25, new(1, 11), _room24, new(23, 12), locked: false);
        r.SpawnPortal(_room25, new(23, 11), _room25, new(23, 11), locked: false);
        r.SpawnPortal(_room26, new(12, 23), _room25, new(12, 0));
        r.SpawnPortal(_room26, new(23, 11), _room26, new(23, 11), locked: false);
        r.SpawnPortal(_room27, new(1, 11), _room28, new(23, 12), locked: false);
        r.SpawnPortal(_room27, new(12, 23), _room26, new(12, 0), locked: false);
        r.Summon(_room27,
            new MonsterSpawn(74, new(4, 5), IsBonus: true, OnDeath: r =>
            {
                r.Summon(_room27, new MonsterSpawn(74, new(10, 16), IsBonus: true, OnDeath: r =>
                {
                    r.Summon(_room27, new MonsterSpawn(253, new(13, 8), IsBonus: true));
                }));
            }),
            new MonsterSpawn(74, new(9, 4), IsBonus: true, OnDeath: r =>
            {
                r.Summon(_room27, new MonsterSpawn(253, new(10, 14)));
            }));
        r.SpawnPortal(_room28, new(12, 23), _room29, new(12, 1), locked: false);
        r.SpawnPortal(_room28, new(23, 12), _room27, new(1, 11), locked: false);
        r.Summon(_room28,
            new MonsterSpawn(253, new(15, 11)),
            new MonsterSpawn(253, new(11, 9), IsBonus: true),
            new MonsterSpawn(253, new(19, 7)));
        r.SpawnPortal(_room29, new(12, 23), _room30, new(12, 1), locked: false);
        r.SpawnPortal(_room29, new(12, 1), _room28, new(12, 23), locked: false);
        r.Summon(_room29,
            new MonsterSpawn(253, new(13, 8)),
            new MonsterSpawn(62, new(12, 11)),
            new MonsterSpawn(253, new(10, 14), IsBonus: true),
            new MonsterSpawn(62, new(16, 4), IsBonus: true),
            new MonsterSpawn(62, new(15, 8)),
            new MonsterSpawn(62, new(6, 14)),
            new MonsterSpawn(62, new(15, 14)));
        r.SpawnPortal(_room30, new(23, 12), _room31, new(1, 11), locked: false);
        r.SpawnPortal(_room30, new(12, 1), _room29, new(12, 23));
        r.SummonNpcs(_room30, new NpcSpawn(329, new(13, 10)));
        r.SpawnPortal(_room31, new(1, 11), _room30, new(23, 12), locked: false);
        r.SpawnPortal(_room31, new(23, 11), _room32, new(1, 12), locked: false);
        r.Summon(_room31,
            new MonsterSpawn(62, new(19, 9)),
            new MonsterSpawn(62, new(21, 18), IsBonus: true));
        r.SpawnPortal(_room32, new(1, 12), _room31, new(23, 11));
        r.SpawnPortal(_room32, new(12, 1), _room32, new(12, 1), locked: false);
        r.SpawnPortal(_room33, new(23, 12), _room34, new(1, 11), locked: false);
        r.SpawnPortal(_room33, new(12, 1), _room32, new(12, 0), locked: false);
        r.Summon(_room33,
            new MonsterSpawn(253, new(20, 18), IsBonus: true),
            new MonsterSpawn(253, new(10, 12), OnDeath: r =>
            {
                r.Summon(_room33, new MonsterSpawn(253, new(15, 11), IsBonus: true));
            }));
        r.SpawnPortal(_room34, new(1, 11), _room33, new(23, 12), locked: false);
        r.SpawnPortal(_room34, new(23, 11), _room35, new(1, 11), locked: false);
        r.SpawnPortal(_room35, new(1, 11), _room34, new(23, 11), locked: false);
        r.SpawnPortal(_room35, new(23, 11), _room36, new(1, 11));
        r.PlaceButton(_room35, 1, new(13, 10), 1000, 1045);
        r.Summon(_room35,
            new MonsterSpawn(253, new(20, 9), IsBonus: true),
            new MonsterSpawn(253, new(18, 14), IsBonus: true),
            new MonsterSpawn(253, new(15, 8), IsBonus: true),
            new MonsterSpawn(253, new(13, 13), IsBonus: true),
            new MonsterSpawn(253, new(10, 8), IsBonus: true),
            new MonsterSpawn(253, new(6, 13)),
            new MonsterSpawn(253, new(7, 8), IsBonus: true),
            new MonsterSpawn(253, new(8, 13)));
        r.SpawnPortal(_room36, new(1, 11), _room35, new(23, 11), locked: false);
        r.SpawnPortal(_room36, new(23, 11), _room37, new(1, 12));
        r.PlaceButton(_room36, 1, new(13, 11), 1000, 1045);
        r.Summon(_room36,
            new MonsterSpawn(253, new(20, 9), IsBonus: true),
            new MonsterSpawn(253, new(18, 14), IsBonus: true),
            new MonsterSpawn(253, new(8, 13), IsBonus: true),
            new MonsterSpawn(253, new(15, 8), IsBonus: true),
            new MonsterSpawn(253, new(13, 13), IsBonus: true),
            new MonsterSpawn(253, new(10, 8)),
            new MonsterSpawn(253, new(6, 13)),
            new MonsterSpawn(253, new(7, 8), IsBonus: true));
        r.SpawnPortal(_room37, new(1, 12), _room38, new(23, 11), locked: false);
        r.SummonNpcs(_room37, new NpcSpawn(316, new(10, 13)));
        r.PlaceButton(_room37, 1, new(19, 14), 1000, 1045);
        r.SpawnPortal(_room38, new(1, 11), _room38, new(1, 11), locked: false);
        r.SpawnPortal(_room38, new(23, 11), _room37, new(1, 12), locked: false);
        r.SpawnExit(_room39, new(1, 11), locked: false);
        r.SpawnPortal(_room39, new(23, 11), _room38, new(1, 0), locked: false);
        r.Summon(_room39,
            new MonsterSpawn(253, new(10, 12)),
            new MonsterSpawn(253, new(16, 16), IsBonus: true, OnDeath: r =>
            {
                r.Summon(_room39, new MonsterSpawn(253, new(15, 11), IsBonus: true));
            }));
    }

    public override void OnRoomEntered(IInstanceRun r, int room)
    {
        if (room == _room1)
        {
            r.Message(Game18NConstString.MissionStartWhenEnterFirstRoom);
        }
        else if (room == _room2)
        {
            r.StartClock(TimeSpan.FromSeconds(600));
            r.Dialog(_room2, 6214);
        }
        else if (room == _room3)
        {
            r.Dialog(_room3, 6215);
        }
        else if (room == _room4)
        {
            r.Dialog(_room4, 6216);
        }
        else if (room == _room6)
        {
            r.Dialog(_room6, 6218);
        }
        else if (room == _room18)
        {
            r.StartRoomClock(_room18, TimeSpan.FromSeconds(60));
        }
        else if (room == _room24)
        {
            r.StartRoomClock(_room24, TimeSpan.FromSeconds(-0.1));
        }
        else if (room == _room30)
        {
            r.UnlockPortal(_room30, _room29);
            r.Dialog(_room30, 6219);
            r.Message(Game18NConstString.ADoorHasOpened);
        }
        else if (room == _room34)
        {
            r.Dialog(_room34, 6220);
        }
        else if (room == _room37)
        {
            r.Dialog(_room37, 6221);
        }
        else if (room == _room39)
        {
            r.Dialog(_room39, 6222);
        }
    }

    public override void OnButtonUsed(IInstanceRun r, int room, int button)
    {
        if (room == _room13 && button == 1)
        {
            r.Message(Game18NConstString.ADoorHasOpened);
            r.UnlockPortal(_room13, _room14);
        }
        else if (room == _room20 && button == 2)
        {
            r.Message(Game18NConstString.ADoorHasOpened);
            r.UnlockPortal(_room20, _room19);
            r.UnlockPortal(_room20, _room21);
            r.Message(Game18NConstString.DoorOpenedSomewhere);
        }
        else if (room == _room35 && button == 1)
        {
            r.Message(Game18NConstString.ADoorHasOpened);
            r.UnlockPortal(_room35, _room36);
        }
        else if (room == _room36 && button == 1)
        {
            r.Message(Game18NConstString.ADoorHasOpened);
            r.UnlockPortal(_room36, _room37);
        }
        else if (room == _room37 && button == 1)
        {
            r.AddClockTime(TimeSpan.FromSeconds(352));
        }
    }

    public override void OnExitReached(IInstanceRun r, int room) => r.Succeed();

    public override void OnTimeout(IInstanceRun r) => r.Fail();
}

return new TimeSpace39();
