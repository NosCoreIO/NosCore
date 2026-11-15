public class TimeSpace9 : InstanceScript
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

    public TimeSpace9()
    {
        Definition = InstanceDefinitionBuilder.Named(9, "The Kovolt Riot", "The Kovolt Riot")
            .ForLevels(18, 99)
            .StartingAt(8, 8)
            .Requiring(1012, 1)
            .WithLives(4)
            .WithObjective(ObjectiveKind.KillMonster, 109, 1)
            .WithDraw(162, 1)
            .WithDraw(168, 1)
            .WithDraw(174, 1)
            .WithSpecialReward(1016, 1)
            .WithReward(721, 1)
            .WithRoom(4014, out _room1, 3, 2)
            .WithRoom(4004, out _room2, 4, 2)
            .WithRoom(4004, out _room3, 5, 2)
            .WithRoom(4000, out _room4, 6, 2)
            .WithRoom(4004, out _room5, 7, 2)
            .WithRoom(4016, out _room6, 8, 2)
            .WithRoom(4006, out _room7, 6, 3)
            .WithRoom(4006, out _room8, 6, 4)
            .WithRoom(4012, out _room9, 6, 5)
            .WithRoom(4006, out _room10, 6, 6)
            .WithRoom(4013, out _room11, 6, 7)
            .WithRoom(4005, out _room12, 7, 5)
            .WithRoom(4005, out _room13, 8, 5)
            .WithRoom(4016, out _room14, 9, 5)
            .WithRoom(4005, out _room15, 5, 5)
            .WithRoom(4002, out _room16, 4, 5)
            .WithRoom(4015, out _room17, 4, 4)
            .WithRoom(4000, out _room18, 3, 5)
            .WithRoom(4014, out _room19, 2, 5)
            .WithRoom(4006, out _room20, 3, 6)
            .WithRoom(4010, out _room21, 3, 7)
            .WithRoom(4005, out _room22, 4, 7)
            .WithRoom(2102, out _room23, 5, 7)
            .Build();
    }

    public override void OnFirstEnter(IInstanceRun r)
    {
        r.StartClock(TimeSpan.FromSeconds(500));
        r.SpawnPortal(_room1, new(17, 9), _room2, new(0, 9), locked: false);
        r.SpawnPortal(_room2, new(17, 9), _room3, new(0, 9));
        r.SpawnPortal(_room2, new(0, 9), _room1, new(17, 9));
        r.Summon(_room2,
            new MonsterSpawn(105, new(6, 8)),
            new MonsterSpawn(105, new(3, 10)));
        r.SpawnPortal(_room3, new(17, 9), _room4, new(0, 9), locked: false);
        r.SpawnPortal(_room3, new(0, 9), _room2, new(17, 9), locked: false);
        r.Summon(_room3,
            new MonsterSpawn(105, new(15, 10)),
            new MonsterSpawn(108, new(9, 9)),
            new MonsterSpawn(108, new(1, 8)));
        r.SpawnPortal(_room4, new(17, 9), _room5, new(0, 9));
        r.SpawnPortal(_room4, new(8, 17), _room7, new(8, 0));
        r.SpawnPortal(_room4, new(0, 9), _room3, new(17, 9));
        r.Summon(_room4,
            new MonsterSpawn(105, new(5, 10)),
            new MonsterSpawn(105, new(2, 9)));
        r.SpawnPortal(_room5, new(17, 9), _room6, new(0, 9), locked: false);
        r.SpawnPortal(_room5, new(0, 9), _room4, new(17, 9), locked: false);
        r.Summon(_room5,
            new MonsterSpawn(105, new(7, 7), OnDeath: r =>
            {
                r.Summon(_room5, new MonsterSpawn(105, new(14, 9)));
            }),
            new MonsterSpawn(105, new(3, 9), OnDeath: r =>
            {
                r.Summon(_room5,
                new MonsterSpawn(108, new(10, 9), IsBonus: true),
                new MonsterSpawn(108, new(12, 8)));
            }));
        r.SpawnPortal(_room6, new(0, 9), _room5, new(17, 9));
        r.PlaceButton(_room6, 1, new(9, 9), 1000, 1045);
        r.Summon(_room6,
            new MonsterSpawn(105, new(5, 10)),
            new MonsterSpawn(105, new(4, 8)));
        r.SpawnPortal(_room7, new(8, 0), _room4, new(8, 17), locked: false);
        r.SpawnPortal(_room7, new(8, 17), _room8, new(8, 0), locked: false);
        r.Summon(_room7,
            new MonsterSpawn(108, new(10, 10)),
            new MonsterSpawn(108, new(9, 4)));
        r.SpawnPortal(_room8, new(8, 0), _room7, new(8, 17), locked: false);
        r.SpawnPortal(_room8, new(8, 17), _room9, new(8, 0), locked: false);
        r.Summon(_room8,
            new MonsterSpawn(105, new(10, 5)),
            new MonsterSpawn(105, new(8, 7), IsBonus: true));
        r.SpawnPortal(_room9, new(8, 0), _room8, new(8, 17));
        r.SpawnPortal(_room9, new(17, 9), _room12, new(0, 9));
        r.SpawnPortal(_room9, new(8, 17), _room10, new(8, 0));
        r.SpawnPortal(_room9, new(0, 9), _room15, new(17, 9));
        r.Summon(_room9,
            new MonsterSpawn(105, new(4, 11), OnDeath: r =>
            {
                r.Summon(_room9, new MonsterSpawn(108, new(8, 10)));
            }),
            new MonsterSpawn(105, new(4, 9)));
        r.SpawnPortal(_room10, new(8, 0), _room9, new(8, 17));
        r.SpawnPortal(_room10, new(8, 17), _room11, new(8, 0));
        r.Summon(_room10,
            new MonsterSpawn(105, new(10, 5)),
            new MonsterSpawn(105, new(7, 5), OnDeath: r =>
            {
                r.Summon(_room10, new MonsterSpawn(105, new(8, 10), OnDeath: r =>
                {
                    r.Summon(_room10, new MonsterSpawn(105, new(10, 16)));
                }));
            }));
        r.SpawnPortal(_room11, new(8, 0), _room10, new(8, 17));
        r.PlaceButton(_room11, 1, new(9, 9), 1000, 1045);
        r.Summon(_room11,
            new MonsterSpawn(105, new(10, 5), IsBonus: true),
            new MonsterSpawn(105, new(8, 5), OnDeath: r =>
            {
                r.Summon(_room11, new MonsterSpawn(108, new(10, 8)));
            }));
        r.SpawnPortal(_room12, new(17, 9), _room13, new(0, 9), locked: false);
        r.SpawnPortal(_room12, new(0, 9), _room9, new(17, 9), locked: false);
        r.Summon(_room12, new MonsterSpawn(108, new(1, 9), IsBonus: true));
        r.SpawnPortal(_room13, new(17, 9), _room14, new(0, 9), locked: false);
        r.SpawnPortal(_room13, new(0, 9), _room12, new(17, 9), locked: false);
        r.Summon(_room13,
            new MonsterSpawn(105, new(5, 8), OnDeath: r =>
            {
                r.Summon(_room13,
                new MonsterSpawn(108, new(12, 9)),
                new MonsterSpawn(108, new(16, 8), IsBonus: true));
            }),
            new MonsterSpawn(105, new(3, 10)));
        r.SpawnPortal(_room14, new(0, 9), _room13, new(17, 9));
        r.PlaceButton(_room14, 1, new(9, 9), 1000, 1045);
        r.Summon(_room14,
            new MonsterSpawn(108, new(5, 8)),
            new MonsterSpawn(105, new(4, 10)));
        r.SpawnPortal(_room15, new(17, 9), _room9, new(0, 9), locked: false);
        r.SpawnPortal(_room15, new(0, 9), _room16, new(17, 9), locked: false);
        r.Summon(_room15, new MonsterSpawn(106, new(4, 7), OnDeath: r =>
            {
                r.Summon(_room15, new MonsterSpawn(106, new(3, 9)));
            }));
        r.SpawnPortal(_room16, new(8, 0), _room17, new(8, 17));
        r.SpawnPortal(_room16, new(17, 9), _room15, new(0, 9));
        r.SpawnPortal(_room16, new(0, 9), _room18, new(17, 9));
        r.Summon(_room16,
            new MonsterSpawn(105, new(13, 9)),
            new MonsterSpawn(108, new(8, 7), OnDeath: r =>
            {
                r.Summon(_room16, new MonsterSpawn(105, new(10, 5)));
            }));
        r.SpawnPortal(_room17, new(8, 17), _room16, new(8, 0));
        r.PlaceButton(_room17, 1, new(8, 7), 1000, 1045);
        r.Summon(_room17,
            new MonsterSpawn(105, new(7, 12)),
            new MonsterSpawn(105, new(7, 10)));
        r.SpawnPortal(_room18, new(17, 9), _room16, new(0, 9), locked: false);
        r.SpawnPortal(_room18, new(8, 17), _room20, new(8, 0));
        r.SpawnPortal(_room18, new(0, 9), _room19, new(17, 9), locked: false);
        r.Summon(_room18,
            new MonsterSpawn(105, new(16, 8)),
            new MonsterSpawn(105, new(14, 9), OnDeath: r =>
            {
                r.Summon(_room18, new MonsterSpawn(105, new(3, 9)));
            }));
        r.SpawnPortal(_room19, new(17, 9), _room18, new(0, 9));
        r.PlaceButton(_room19, 1, new(7, 7), 1000, 1045);
        r.Summon(_room19,
            new MonsterSpawn(105, new(10, 9), OnDeath: r =>
            {
                r.Summon(_room19, new MonsterSpawn(105, new(13, 9)));
            }),
            new MonsterSpawn(105, new(8, 8)));
        r.SpawnPortal(_room20, new(8, 0), _room18, new(8, 17), locked: false);
        r.SpawnPortal(_room20, new(8, 17), _room21, new(8, 0), locked: false);
        r.Summon(_room20, new MonsterSpawn(106, new(11, 5), OnDeath: r =>
            {
                r.Summon(_room20, new MonsterSpawn(106, new(8, 10)));
            }));
        r.SpawnPortal(_room21, new(8, 0), _room20, new(8, 17), locked: false);
        r.SpawnPortal(_room21, new(17, 9), _room22, new(0, 9), locked: false);
        r.Summon(_room21,
            new MonsterSpawn(105, new(9, 6)),
            new MonsterSpawn(105, new(9, 2)));
        r.SpawnPortal(_room22, new(17, 9), _room23, new(7, 19), locked: false);
        r.SpawnPortal(_room22, new(0, 9), _room21, new(17, 9), locked: false);
        r.Summon(_room22,
            new MonsterSpawn(105, new(17, 10)),
            new MonsterSpawn(105, new(15, 9)),
            new MonsterSpawn(106, new(13, 8), IsBonus: true),
            new MonsterSpawn(108, new(10, 8)));
        r.SpawnPortal(_room23, new(7, 19), _room22, new(17, 9));
        r.Summon(_room23,
            new MonsterSpawn(105, new(29, 19), IsBonus: true),
            new MonsterSpawn(105, new(26, 15), OnDeath: r =>
            {
                r.Summon(_room23, new MonsterSpawn(109, new(19, 19)));
            }),
            new MonsterSpawn(105, new(25, 13)),
            new MonsterSpawn(105, new(10, 19)),
            new MonsterSpawn(105, new(10, 8)));
    }

    public override void OnRoomEntered(IInstanceRun r, int room)
    {
        if (room == _room1)
        {
            r.Message(Game18NConstString.MissionStartWhenEnterFirstRoom);
        }
        else if (room == _room2)
        {
            r.Dialog(_room2, 6175);
        }
        else if (room == _room4)
        {
            r.Dialog(_room4, 6176);
        }
        else if (room == _room6)
        {
            r.Dialog(_room6, 6177);
        }
        else if (room == _room9)
        {
            r.Dialog(_room9, 6178);
        }
        else if (room == _room10)
        {
            r.Dialog(_room10, 6179);
        }
        else if (room == _room11)
        {
            r.Dialog(_room11, 6180);
        }
        else if (room == _room14)
        {
            r.Dialog(_room14, 6181);
        }
        else if (room == _room16)
        {
            r.Dialog(_room16, 6183);
        }
        else if (room == _room17)
        {
            r.Dialog(_room17, 6184);
        }
        else if (room == _room19)
        {
            r.Dialog(_room19, 6185);
        }
        else if (room == _room23)
        {
            r.Dialog(_room23, 6187);
        }
    }

    public override void OnRoomCleared(IInstanceRun r, int room)
    {
        if (room == _room2)
        {
            r.UnlockPortal(_room2, _room3);
            r.Message(Game18NConstString.ADoorHasOpened);
            r.Message(Game18NConstString.QuickMissionComplete);
        }
        else if (room == _room4)
        {
            r.UnlockPortal(_room4, _room5);
            r.UnlockPortal(_room4, _room3);
            r.Message(Game18NConstString.ADoorHasOpened);
            r.Message(Game18NConstString.QuickMissionComplete);
        }
        else if (room == _room6)
        {
            r.UnlockPortal(_room6, _room5);
            r.AddClockTime(TimeSpan.FromSeconds(200));
            r.Message(Game18NConstString.ADoorHasOpened);
            r.Message(Game18NConstString.QuickMissionComplete);
        }
        else if (room == _room9)
        {
            r.UnlockPortal(_room9, _room8);
            r.UnlockPortal(_room9, _room10);
            r.AddClockTime(TimeSpan.FromSeconds(150));
            r.Message(Game18NConstString.ADoorHasOpened);
            r.Message(Game18NConstString.QuickMissionComplete);
        }
        else if (room == _room10)
        {
            r.UnlockPortal(_room10, _room9);
            r.UnlockPortal(_room10, _room11);
            r.Message(Game18NConstString.ADoorHasOpened);
            r.Message(Game18NConstString.QuickMissionComplete);
        }
        else if (room == _room11)
        {
            r.UnlockPortal(_room11, _room10);
            r.AddClockTime(TimeSpan.FromSeconds(150));
            r.Message(Game18NConstString.ADoorHasOpened);
            r.Message(Game18NConstString.QuickMissionComplete);
        }
        else if (room == _room14)
        {
            r.UnlockPortal(_room14, _room13);
            r.AddClockTime(TimeSpan.FromSeconds(150));
            r.Message(Game18NConstString.ADoorHasOpened);
            r.Dialog(_room14, 6182);
            r.UnlockPortal(_room9, _room15);
        }
        else if (room == _room16)
        {
            r.UnlockPortal(_room16, _room17);
            r.Message(Game18NConstString.ADoorHasOpened);
            r.Message(Game18NConstString.QuickMissionComplete);
        }
        else if (room == _room17)
        {
            r.UnlockPortal(_room17, _room16);
            r.Message(Game18NConstString.ADoorHasOpened);
            r.Message(Game18NConstString.QuickMissionComplete);
        }
        else if (room == _room19)
        {
            r.UnlockPortal(_room19, _room18);
            r.Message(Game18NConstString.ADoorHasOpened);
            r.Message(Game18NConstString.QuickMissionComplete);
        }
        else if (room == _room23)
        {
            r.Message(Game18NConstString.QuickMissionComplete);
            r.Message(Game18NConstString.NewGoal);
            r.Dialog(_room23, 6188);
            r.Succeed();
        }
    }

    public override void OnButtonUsed(IInstanceRun r, int room, int button)
    {
        if (room == _room6 && button == 1)
        {
            r.Message(Game18NConstString.NewGoal);
            r.UnlockPortal(_room4, _room7);
        }
        else if (room == _room11 && button == 1)
        {
            r.Message(Game18NConstString.NewGoal);
            r.UnlockPortal(_room9, _room12);
        }
        else if (room == _room14 && button == 1)
        {
            r.Message(Game18NConstString.NewGoal);
        }
        else if (room == _room17 && button == 1)
        {
            r.AddClockTime(TimeSpan.FromSeconds(167));
            r.Message(Game18NConstString.NewGoal);
            r.UnlockPortal(_room16, _room18);
        }
        else if (room == _room19 && button == 1)
        {
            r.AddClockTime(TimeSpan.FromSeconds(142));
            r.Message(Game18NConstString.NewGoal);
            r.UnlockPortal(_room18, _room20);
        }
    }

    public override void OnExitReached(IInstanceRun r, int room) => r.Succeed();
}

return new TimeSpace9();
