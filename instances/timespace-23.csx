public class TimeSpace23 : InstanceScript
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

    public TimeSpace23()
    {
        Definition = InstanceDefinitionBuilder.Named(23, "The Kovolt Hero", "The Kovolt Hero")
            .ForLevels(33, 99)
            .StartingAt(7, 8)
            .Requiring(1012, 3)
            .WithLives(3)
            .WithDraw(515, 1)
            .WithDraw(702, 1)
            .WithSpecialReward(801, 1)
            .WithReward(1013, 1)
            .WithReward(1087, 1)
            .WithRoom(5203, out _room1, 0, 0)
            .WithRoom(5204, out _room2, 1, 0)
            .WithRoom(5207, out _room3, 2, 0)
            .WithRoom(5208, out _room4, 2, 1)
            .WithRoom(5207, out _room5, 3, 1)
            .WithRoom(5208, out _room6, 3, 2)
            .WithRoom(5204, out _room7, 4, 2)
            .WithRoom(5207, out _room8, 5, 2)
            .WithRoom(5208, out _room9, 5, 3)
            .WithRoom(5204, out _room10, 6, 3)
            .WithRoom(5204, out _room11, 7, 3)
            .WithRoom(5205, out _room12, 8, 3)
            .WithRoom(4003, out _room13, 9, 3)
            .WithRoom(4011, out _room14, 9, 2)
            .WithRoom(4000, out _room15, 10, 2)
            .WithRoom(4003, out _room16, 11, 2)
            .WithRoom(4008, out _room17, 11, 1)
            .WithRoom(4002, out _room18, 10, 1)
            .WithRoom(4014, out _room19, 9, 1)
            .WithRoom(4015, out _room20, 10, 0)
            .WithRoom(4007, out _room21, 11, 3)
            .WithRoom(4006, out _room22, 11, 4)
            .WithRoom(4003, out _room23, 11, 5)
            .WithRoom(4009, out _room24, 11, 6)
            .WithRoom(4014, out _room25, 10, 6)
            .WithRoom(4005, out _room26, 10, 5)
            .WithRoom(4001, out _room27, 9, 5)
            .WithRoom(4013, out _room28, 9, 6)
            .WithRoom(4003, out _room29, 9, 4)
            .WithRoom(4006, out _room30, 10, 3)
            .WithRoom(4013, out _room31, 10, 4)
            .Build();
    }

    public override void OnFirstEnter(IInstanceRun r)
    {
        r.StartClock(TimeSpan.FromSeconds(3000));
        r.SpawnPortal(_room1, new(23, 12), _room2, new(1, 12), locked: false);
        r.SpawnPortal(_room2, new(23, 12), _room3, new(1, 12), locked: false);
        r.SpawnPortal(_room2, new(1, 12), _room1, new(23, 12), locked: false);
        r.SummonNpcs(_room2, new NpcSpawn(311, new(15, 10)), new NpcSpawn(311, new(14, 8)), new NpcSpawn(311, new(13, 11)), new NpcSpawn(311, new(3, 10)));
        r.SpawnPortal(_room3, new(12, 23), _room4, new(12, 1));
        r.SpawnPortal(_room3, new(1, 12), _room2, new(23, 12));
        r.Summon(_room3,
            new MonsterSpawn(116, new(10, 16)),
            new MonsterSpawn(118, new(10, 13)),
            new MonsterSpawn(118, new(8, 12)),
            new MonsterSpawn(115, new(7, 15)),
            new MonsterSpawn(115, new(7, 12), IsBonus: true),
            new MonsterSpawn(116, new(6, 12)),
            new MonsterSpawn(115, new(4, 12)),
            new MonsterSpawn(118, new(3, 12)),
            new MonsterSpawn(115, new(1, 12)));
        r.SummonNpcs(_room3, new NpcSpawn(311, new(10, 14)), new NpcSpawn(311, new(8, 11)), new NpcSpawn(311, new(9, 13)), new NpcSpawn(311, new(5, 10)), new NpcSpawn(311, new(2, 11)), new NpcSpawn(311, new(1, 10)));
        r.SpawnPortal(_room4, new(12, 1), _room3, new(12, 23));
        r.SpawnPortal(_room4, new(23, 12), _room5, new(1, 12));
        r.Summon(_room4,
            new MonsterSpawn(116, new(19, 14)),
            new MonsterSpawn(115, new(17, 14), IsBonus: true),
            new MonsterSpawn(121, new(17, 13)),
            new MonsterSpawn(121, new(16, 9)),
            new MonsterSpawn(115, new(15, 10)),
            new MonsterSpawn(118, new(15, 8)),
            new MonsterSpawn(118, new(12, 9)),
            new MonsterSpawn(115, new(12, 7)),
            new MonsterSpawn(118, new(11, 7), IsBonus: true),
            new MonsterSpawn(121, new(14, 8)),
            new MonsterSpawn(115, new(9, 2)),
            new MonsterSpawn(118, new(9, 4)));
        r.SummonNpcs(_room4, new NpcSpawn(311, new(19, 13)), new NpcSpawn(311, new(18, 14)), new NpcSpawn(311, new(18, 12)), new NpcSpawn(311, new(13, 9)), new NpcSpawn(311, new(10, 4)), new NpcSpawn(311, new(10, 2)), new NpcSpawn(311, new(9, 7)));
        r.SpawnPortal(_room5, new(12, 23), _room6, new(12, 1));
        r.SpawnPortal(_room5, new(1, 12), _room4, new(23, 12));
        r.Summon(_room5,
            new MonsterSpawn(116, new(10, 16)),
            new MonsterSpawn(118, new(10, 13)),
            new MonsterSpawn(118, new(8, 13)),
            new MonsterSpawn(115, new(3, 13)),
            new MonsterSpawn(115, new(10, 14)),
            new MonsterSpawn(116, new(9, 12)),
            new MonsterSpawn(115, new(4, 13)),
            new MonsterSpawn(118, new(3, 10), IsBonus: true),
            new MonsterSpawn(115, new(1, 13)));
        r.SummonNpcs(_room5, new NpcSpawn(311, new(9, 15)), new NpcSpawn(311, new(8, 11)), new NpcSpawn(311, new(7, 14)), new NpcSpawn(311, new(5, 10)), new NpcSpawn(311, new(2, 14)), new NpcSpawn(311, new(1, 10)));
        r.SpawnPortal(_room6, new(12, 1), _room5, new(12, 23), locked: false);
        r.SpawnPortal(_room6, new(23, 12), _room7, new(1, 12), locked: false);
        r.SpawnPortal(_room7, new(23, 12), _room8, new(1, 12));
        r.SpawnPortal(_room7, new(1, 12), _room6, new(23, 12));
        r.Summon(_room7,
            new MonsterSpawn(121, new(16, 11)),
            new MonsterSpawn(115, new(15, 9)),
            new MonsterSpawn(121, new(14, 8)),
            new MonsterSpawn(118, new(13, 10)),
            new MonsterSpawn(116, new(11, 9)),
            new MonsterSpawn(115, new(7, 9), IsBonus: true),
            new MonsterSpawn(118, new(10, 8)),
            new MonsterSpawn(116, new(13, 9)),
            new MonsterSpawn(115, new(7, 12)),
            new MonsterSpawn(121, new(7, 9)),
            new MonsterSpawn(118, new(5, 12)),
            new MonsterSpawn(115, new(3, 12)),
            new MonsterSpawn(121, new(2, 10)));
        r.SummonNpcs(_room7, new NpcSpawn(311, new(16, 10)), new NpcSpawn(311, new(16, 8)), new NpcSpawn(311, new(14, 8)), new NpcSpawn(311, new(12, 7)), new NpcSpawn(311, new(8, 8)), new NpcSpawn(311, new(6, 9)), new NpcSpawn(311, new(5, 13)), new NpcSpawn(311, new(3, 10)), new NpcSpawn(311, new(1, 13)));
        r.SpawnPortal(_room8, new(12, 23), _room9, new(12, 1));
        r.SpawnPortal(_room8, new(1, 12), _room7, new(23, 12));
        r.Summon(_room8,
            new MonsterSpawn(313, new(10, 12)),
            new MonsterSpawn(115, new(1, 13)));
        r.SummonNpcs(_room8, new NpcSpawn(311, new(8, 11)), new NpcSpawn(311, new(7, 14)), new NpcSpawn(311, new(5, 10)), new NpcSpawn(311, new(2, 13)), new NpcSpawn(311, new(1, 10)));
        r.SpawnPortal(_room9, new(12, 1), _room8, new(12, 23), locked: false);
        r.SpawnPortal(_room9, new(23, 12), _room10, new(1, 12), locked: false);
        r.SpawnPortal(_room10, new(23, 12), _room11, new(1, 12));
        r.SpawnPortal(_room10, new(1, 12), _room9, new(23, 12));
        r.Summon(_room10,
            new MonsterSpawn(121, new(15, 8)),
            new MonsterSpawn(118, new(14, 10)),
            new MonsterSpawn(118, new(12, 8)),
            new MonsterSpawn(121, new(11, 10), IsBonus: true),
            new MonsterSpawn(115, new(10, 8)),
            new MonsterSpawn(115, new(9, 10)),
            new MonsterSpawn(121, new(8, 8)),
            new MonsterSpawn(115, new(6, 9), IsBonus: true));
        r.SummonNpcs(_room10, new NpcSpawn(310, new(5, 14)), new NpcSpawn(310, new(4, 10)), new NpcSpawn(337, new(3, 14)), new NpcSpawn(312, new(3, 9)));
        r.SpawnPortal(_room11, new(23, 12), _room12, new(1, 12));
        r.SpawnPortal(_room11, new(1, 12), _room10, new(23, 12));
        r.Summon(_room11,
            new MonsterSpawn(121, new(15, 8), IsBonus: true),
            new MonsterSpawn(118, new(14, 10)),
            new MonsterSpawn(118, new(12, 8)),
            new MonsterSpawn(121, new(11, 10)),
            new MonsterSpawn(115, new(10, 8)),
            new MonsterSpawn(115, new(9, 10)),
            new MonsterSpawn(121, new(8, 8)),
            new MonsterSpawn(115, new(6, 9)));
        r.SpawnPortal(_room12, new(23, 12), _room13, new(0, 9));
        r.SpawnPortal(_room12, new(1, 12), _room11, new(23, 12));
        r.Summon(_room12,
            new MonsterSpawn(121, new(15, 8)),
            new MonsterSpawn(118, new(14, 10)),
            new MonsterSpawn(118, new(12, 11)),
            new MonsterSpawn(121, new(11, 10)),
            new MonsterSpawn(115, new(10, 8)),
            new MonsterSpawn(115, new(9, 10)),
            new MonsterSpawn(121, new(8, 8)),
            new MonsterSpawn(115, new(6, 9)));
        r.SpawnPortal(_room13, new(8, 0), _room14, new(8, 17), locked: false);
        r.SpawnPortal(_room13, new(8, 17), _room29, new(8, 0));
        r.SpawnPortal(_room13, new(0, 9), _room12, new(23, 12), locked: false);
        r.PlaceButton(_room13, 1, new(10, 16), 1048, 1049, kind: ButtonKind.Sculpture);
        r.SpawnPortal(_room14, new(17, 9), _room15, new(0, 9), locked: false);
        r.SpawnPortal(_room14, new(8, 17), _room13, new(8, 0));
        r.SpawnPortal(_room15, new(17, 9), _room16, new(0, 9), locked: false);
        r.SpawnPortal(_room15, new(8, 17), _room30, new(8, 0), locked: false);
        r.SpawnPortal(_room15, new(0, 9), _room14, new(17, 9), locked: false);
        r.SpawnPortal(_room16, new(8, 0), _room17, new(8, 17), locked: false);
        r.SpawnPortal(_room16, new(8, 17), _room21, new(8, 0), locked: false);
        r.SpawnPortal(_room16, new(0, 9), _room15, new(17, 9), locked: false);
        r.SpawnPortal(_room17, new(8, 17), _room16, new(8, 0), locked: false);
        r.SpawnPortal(_room17, new(0, 9), _room18, new(17, 9), locked: false);
        r.SpawnPortal(_room18, new(8, 0), _room20, new(8, 17), locked: false);
        r.SpawnPortal(_room18, new(17, 9), _room17, new(0, 9), locked: false);
        r.SpawnPortal(_room18, new(0, 9), _room19, new(17, 9), locked: false);
        r.SpawnPortal(_room19, new(17, 9), _room18, new(0, 9), locked: false);
        r.SpawnPortal(_room20, new(8, 17), _room18, new(8, 0), locked: false);
        r.SpawnPortal(_room21, new(8, 0), _room16, new(8, 17), locked: false);
        r.SpawnPortal(_room21, new(8, 17), _room22, new(8, 0), locked: false);
        r.SpawnPortal(_room22, new(8, 0), _room21, new(8, 17), locked: false);
        r.SpawnPortal(_room22, new(8, 17), _room23, new(8, 0), locked: false);
        r.SpawnPortal(_room23, new(8, 0), _room22, new(8, 17), locked: false);
        r.SpawnPortal(_room23, new(8, 17), _room24, new(8, 0), locked: false);
        r.SpawnPortal(_room23, new(0, 9), _room26, new(17, 9), locked: false);
        r.SpawnPortal(_room24, new(8, 0), _room23, new(8, 17), locked: false);
        r.SpawnPortal(_room24, new(0, 9), _room25, new(17, 9), locked: false);
        r.SpawnPortal(_room25, new(17, 9), _room24, new(0, 9), locked: false);
        r.SpawnPortal(_room26, new(17, 9), _room23, new(0, 9), locked: false);
        r.SpawnPortal(_room26, new(0, 9), _room27, new(17, 9), locked: false);
        r.SpawnPortal(_room27, new(8, 0), _room29, new(8, 17), locked: false);
        r.SpawnPortal(_room27, new(17, 9), _room26, new(0, 9), locked: false);
        r.SpawnPortal(_room27, new(8, 17), _room28, new(8, 0), locked: false);
        r.SpawnPortal(_room28, new(8, 0), _room27, new(8, 17), locked: false);
        r.SpawnPortal(_room29, new(8, 0), _room13, new(8, 17));
        r.SpawnPortal(_room29, new(8, 17), _room27, new(8, 0));
        r.SpawnExit(_room29, new(0, 9));
        r.Summon(_room29,
            new MonsterSpawn(115, new(7, 14), OnDeath: r =>
            {
                r.Summon(_room29, new MonsterSpawn(115, new(7, 3)));
            }),
            new MonsterSpawn(115, new(6, 11)),
            new MonsterSpawn(115, new(3, 8), OnDeath: r =>
            {
                r.Summon(_room29, new MonsterSpawn(115, new(8, 2), OnDeath: r =>
                {
                    r.Summon(_room29,
                    new MonsterSpawn(121, new(9, 11)),
                    new MonsterSpawn(118, new(9, 15), OnDeath: r =>
                    {
                        r.Summon(_room29,
                        new MonsterSpawn(121, new(8, 5)),
                        new MonsterSpawn(118, new(8, 8), OnDeath: r =>
                        {
                            r.Summon(_room29, new MonsterSpawn(121, new(5, 9)));
                        }));
                    }));
                }));
            }));
        r.SpawnPortal(_room30, new(8, 0), _room15, new(8, 17), locked: false);
        r.SpawnPortal(_room30, new(8, 17), _room31, new(8, 0), locked: false);
        r.SpawnPortal(_room31, new(8, 0), _room30, new(8, 17), locked: false);
    }

    public override void OnRoomEntered(IInstanceRun r, int room)
    {
        if (room == _room1)
        {
            r.Message(Game18NConstString.MissionStartWhenEnterFirstRoom);
        }
        else if (room == _room2)
        {
            r.Dialog(_room2, 6195);
        }
        else if (room == _room6)
        {
            r.Dialog(_room6, 6199);
        }
        else if (room == _room8)
        {
            r.StartRoomClock(_room8, TimeSpan.FromSeconds(60));
            r.Message(Game18NConstString.EnemiesReinforcements);
            r.Dialog(_room8, 6201);
            r.Objective(_room8, LanguageKey.TIMESPACE_GOAL_SURVIVE_KOARENS);
            r.Summon(_room8,
                new MonsterSpawn(118, new(3, 10)),
                new MonsterSpawn(115, new(4, 13)),
                new MonsterSpawn(116, new(6, 10)),
                new MonsterSpawn(115, new(7, 12)),
                new MonsterSpawn(115, new(7, 15)),
                new MonsterSpawn(118, new(8, 10)),
                new MonsterSpawn(118, new(10, 13)),
                new MonsterSpawn(116, new(10, 16)));
            r.At(r.Elapsed + TimeSpan.FromSeconds(30), r =>
            {
                r.Summon(_room8,
                    new MonsterSpawn(118, new(3, 10)),
                    new MonsterSpawn(115, new(4, 13)),
                    new MonsterSpawn(116, new(6, 10)),
                    new MonsterSpawn(115, new(7, 12)),
                    new MonsterSpawn(115, new(7, 15)),
                    new MonsterSpawn(118, new(8, 10)),
                    new MonsterSpawn(118, new(10, 13)),
                    new MonsterSpawn(116, new(10, 16)));
            });
            r.At(r.Elapsed + TimeSpan.FromSeconds(60), r =>
            {
                r.Dismiss(_room8, 115);
                r.Dismiss(_room8, 116);
                r.Dismiss(_room8, 118);
                r.Dismiss(_room8, 313);
                r.UnlockPortal(_room8, _room9);
                r.UnlockPortal(_room8, _room7);
                r.Message(Game18NConstString.ADoorHasOpened);
                r.Dialog(_room8, 6202);
                r.StopRoomClock(_room8);
            });
        }
        else if (room == _room9)
        {
            r.Dialog(_room9, 6203);
        }
        else if (room == _room10)
        {
            r.Dialog(_room10, 6204);
            r.Message(LanguageKey.TIMESPACE_ESCAPE_WITH_THE_CAPTAIN);
            r.Objective(_room10, LanguageKey.TIMESPACE_GOAL_KILL_ALL_ENEMIES);
        }
        else if (room == _room11)
        {
            r.Dialog(_room11, 6204);
            r.Objective(_room11, LanguageKey.TIMESPACE_GOAL_KILL_ALL_ENEMIES);
        }
        else if (room == _room12)
        {
            r.Dialog(_room12, 6204);
            r.Objective(_room12, LanguageKey.TIMESPACE_GOAL_KILL_ALL_ENEMIES);
        }
        else if (room == _room13)
        {
            r.Dialog(_room13, 6205);
        }
        else if (room == _room14)
        {
            r.Message(LanguageKey.TIMESPACE_LONG_WAY_AHEAD);
        }
        else if (room == _room29)
        {
            r.Dialog(_room29, 6206);
        }
    }

    public override void OnRoomCleared(IInstanceRun r, int room)
    {
        if (room == _room3)
        {
            r.UnlockPortal(_room3, _room4);
            r.UnlockPortal(_room3, _room2);
            r.Message(Game18NConstString.ADoorHasOpened);
            r.Dialog(_room3, 6196);
        }
        else if (room == _room4)
        {
            r.UnlockPortal(_room4, _room5);
            r.UnlockPortal(_room4, _room3);
            r.Message(Game18NConstString.ADoorHasOpened);
            r.Dialog(_room4, 6197);
        }
        else if (room == _room5)
        {
            r.UnlockPortal(_room5, _room4);
            r.UnlockPortal(_room5, _room6);
            r.Message(Game18NConstString.ADoorHasOpened);
            r.Dialog(_room5, 6198);
        }
        else if (room == _room7)
        {
            r.UnlockPortal(_room7, _room6);
            r.UnlockPortal(_room7, _room8);
            r.Message(Game18NConstString.ADoorHasOpened);
            r.Dialog(_room7, 6200);
        }
        else if (room == _room10)
        {
            r.UnlockPortal(_room10, _room11);
            r.UnlockPortal(_room10, _room9);
            r.Message(Game18NConstString.ADoorHasOpened);
            r.Message(Game18NConstString.QuickMissionComplete);
        }
        else if (room == _room11)
        {
            r.UnlockPortal(_room11, _room10);
            r.UnlockPortal(_room11, _room12);
            r.Message(Game18NConstString.ADoorHasOpened);
            r.Message(Game18NConstString.QuickMissionComplete);
        }
        else if (room == _room12)
        {
            r.UnlockPortal(_room12, _room11);
            r.UnlockPortal(_room12, _room13);
            r.Message(Game18NConstString.ADoorHasOpened);
            r.Message(Game18NConstString.QuickMissionComplete);
        }
        else if (room == _room29)
        {
            r.UnlockExit(_room29);
            r.Message(Game18NConstString.ADoorHasOpened);
            r.Dialog(_room29, 6207);
        }
    }

    public override void OnButtonUsed(IInstanceRun r, int room, int button)
    {
        if (room == _room13 && button == 1)
        {
            r.Message(Game18NConstString.ObjectHasBeenMoved);
            r.Objective(_room13, LanguageKey.TIMESPACE_GOAL_KILL_ALL_ENEMIES);
            r.UnlockPortal(_room13, _room29);
        }
    }

    public override void OnExitReached(IInstanceRun r, int room) => r.Succeed();
}

return new TimeSpace23();
