public class TimeSpace6 : InstanceScript
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

    public TimeSpace6()
    {
        Definition = InstanceDefinitionBuilder.Named(6, "The Labyrinth", "The Labyrinth")
            .ForLevels(15, 99)
            .StartingAt(9, 10)
            .Requiring(1012, 1)
            .WithLives(2)
            .WithDraw(17, 1)
            .WithDraw(716, 1)
            .WithSpecialReward(2038, 5)
            .WithReward(2046, 1)
            .WithRoom(4100, out _room1, 0, 8)
            .WithRoom(4101, out _room2, 1, 8)
            .WithRoom(4102, out _room3, 2, 8)
            .WithRoom(4103, out _room4, 3, 8)
            .WithRoom(4103, out _room5, 4, 8)
            .WithRoom(4109, out _room6, 4, 9)
            .WithRoom(4101, out _room7, 3, 9)
            .WithRoom(4100, out _room8, 2, 9)
            .WithRoom(4111, out _room9, 1, 9)
            .WithRoom(4111, out _room10, 0, 9)
            .WithRoom(4109, out _room11, 0, 10)
            .WithRoom(4106, out _room12, 1, 10)
            .WithRoom(4106, out _room13, 2, 10)
            .WithRoom(4105, out _room14, 3, 10)
            .WithRoom(4105, out _room15, 4, 10)
            .Build();
    }

    public override void OnFirstEnter(IInstanceRun r)
    {
        r.StartClock(TimeSpan.FromSeconds(300));
        r.SpawnPortal(_room1, new(28, 15), _room2, new(1, 14), locked: false);
        r.SpawnPortal(_room2, new(28, 15), _room3, new(1, 14), locked: false);
        r.SpawnPortal(_room2, new(14, 28), _room9, new(14, 1), locked: false);
        r.SpawnPortal(_room2, new(1, 14), _room1, new(28, 15), locked: false);
        r.SpawnPortal(_room3, new(28, 14), _room4, new(1, 15), locked: false);
        r.SpawnPortal(_room3, new(14, 28), _room8, new(14, 1), locked: false);
        r.SpawnPortal(_room3, new(1, 14), _room2, new(28, 15), locked: false);
        r.Summon(_room3,
            new MonsterSpawn(33, new(17, 15)),
            new MonsterSpawn(33, new(14, 13), OnDeath: r =>
            {
                r.Summon(_room3, new MonsterSpawn(63, new(21, 14), IsBonus: true));
            }),
            new MonsterSpawn(33, new(5, 14), OnDeath: r =>
            {
                r.Summon(_room3, new MonsterSpawn(63, new(14, 19)));
            }));
        r.SpawnPortal(_room4, new(28, 15), _room5, new(1, 15), locked: false);
        r.SpawnPortal(_room4, new(14, 28), _room7, new(14, 1), locked: false);
        r.SpawnPortal(_room4, new(1, 15), _room3, new(28, 14), locked: false);
        r.Summon(_room4,
            new MonsterSpawn(64, new(20, 14)),
            new MonsterSpawn(28, new(20, 15), OnDeath: r =>
            {
                r.Summon(_room4, new MonsterSpawn(64, new(13, 11)));
            }),
            new MonsterSpawn(64, new(17, 20), OnDeath: r =>
            {
                r.Summon(_room4,
                new MonsterSpawn(64, new(10, 12)),
                new MonsterSpawn(64, new(12, 16)));
            }),
            new MonsterSpawn(64, new(15, 11)),
            new MonsterSpawn(28, new(11, 18)));
        r.SpawnPortal(_room5, new(14, 28), _room6, new(14, 1), locked: false);
        r.SpawnPortal(_room5, new(1, 15), _room4, new(28, 15), locked: false);
        r.SpawnPortal(_room6, new(14, 1), _room5, new(14, 28));
        r.SpawnPortal(_room6, new(14, 28), _room15, new(14, 1));
        r.SpawnPortal(_room6, new(1, 15), _room7, new(28, 15));
        r.Summon(_room6,
            new MonsterSpawn(18, new(20, 19)),
            new MonsterSpawn(130, new(12, 18)),
            new MonsterSpawn(97, new(9, 12)),
            new MonsterSpawn(18, new(8, 19)));
        r.SpawnPortal(_room7, new(14, 1), _room4, new(14, 28), locked: false);
        r.SpawnPortal(_room7, new(28, 15), _room6, new(1, 15), locked: false);
        r.SpawnPortal(_room7, new(1, 14), _room8, new(28, 15), locked: false);
        r.SpawnPortal(_room8, new(14, 1), _room3, new(14, 28), locked: false);
        r.SpawnPortal(_room8, new(28, 15), _room7, new(1, 14), locked: false);
        r.SpawnPortal(_room8, new(14, 28), _room13, new(14, 1), locked: false);
        r.SpawnPortal(_room8, new(1, 15), _room9, new(28, 15), locked: false);
        r.PlaceButton(_room8, 1, new(8, 6), 1057, 1057, kind: ButtonKind.Key);
        r.Summon(_room8,
            new MonsterSpawn(130, new(15, 20), OnDeath: r =>
            {
                r.Summon(_room8,
                new MonsterSpawn(130, new(18, 19), OnDeath: r =>
                {
                    r.Summon(_room8, new MonsterSpawn(130, new(20, 12)));
                }),
                new MonsterSpawn(130, new(20, 18)));
            }),
            new MonsterSpawn(130, new(12, 15), IsBonus: true));
        r.SpawnPortal(_room9, new(14, 1), _room2, new(14, 28), locked: false);
        r.SpawnPortal(_room9, new(28, 15), _room8, new(1, 15), locked: false);
        r.SpawnPortal(_room9, new(14, 28), _room12, new(14, 1));
        r.SpawnPortal(_room9, new(1, 15), _room10, new(28, 15), locked: false);
        r.Summon(_room9,
            new MonsterSpawn(98, new(23, 13), OnDeath: r =>
            {
                r.Summon(_room9,
                new MonsterSpawn(63, new(10, 18)),
                new MonsterSpawn(63, new(21, 19)));
            }),
            new MonsterSpawn(98, new(13, 11)),
            new MonsterSpawn(98, new(11, 17), IsBonus: true));
        r.SpawnPortal(_room10, new(28, 15), _room9, new(1, 15), locked: false);
        r.SpawnPortal(_room10, new(14, 28), _room11, new(14, 1), locked: false);
        r.Summon(_room10,
            new MonsterSpawn(19, new(25, 13)),
            new MonsterSpawn(57, new(22, 15)),
            new MonsterSpawn(19, new(22, 10)),
            new MonsterSpawn(57, new(26, 12)),
            new MonsterSpawn(19, new(19, 7)));
        r.SpawnPortal(_room11, new(14, 1), _room10, new(14, 28));
        r.SpawnPortal(_room11, new(28, 15), _room12, new(1, 15));
        r.Summon(_room11,
            new MonsterSpawn(10, new(12, 19), IsBonus: true),
            new MonsterSpawn(10, new(10, 11), OnDeath: r =>
            {
                r.Summon(_room11, new MonsterSpawn(98, new(9, 14), OnDeath: r =>
                {
                    r.Summon(_room11,
                    new MonsterSpawn(98, new(15, 8)),
                    new MonsterSpawn(10, new(19, 19)));
                }));
            }));
        r.SpawnPortal(_room12, new(14, 1), _room9, new(14, 28), locked: false);
        r.SpawnPortal(_room12, new(28, 15), _room13, new(1, 15), locked: false);
        r.SpawnPortal(_room12, new(1, 15), _room11, new(28, 15), locked: false);
        r.Summon(_room12, new MonsterSpawn(10, new(18, 16), OnDeath: r =>
            {
                r.Summon(_room12,
                new MonsterSpawn(55, new(8, 15)),
                new MonsterSpawn(55, new(10, 11)),
                new MonsterSpawn(55, new(13, 14)),
                new MonsterSpawn(55, new(13, 19)),
                new MonsterSpawn(55, new(14, 8)),
                new MonsterSpawn(55, new(18, 11)),
                new MonsterSpawn(55, new(20, 15)),
                new MonsterSpawn(55, new(23, 16)));
            }));
        r.SpawnPortal(_room13, new(14, 1), _room8, new(14, 28), locked: false);
        r.SpawnPortal(_room13, new(28, 15), _room14, new(1, 14));
        r.SpawnPortal(_room13, new(1, 15), _room12, new(28, 15), locked: false);
        r.PlaceButton(_room13, 1, new(14, 15), 1000, 1045);
        r.Summon(_room13,
            new MonsterSpawn(27, new(10, 20)),
            new MonsterSpawn(27, new(10, 11), OnDeath: r =>
            {
                r.Summon(_room13, new MonsterSpawn(0, new(18, 16)));
            }));
        r.SpawnPortal(_room14, new(28, 15), _room15, new(1, 14), locked: false);
        r.SpawnExit(_room14, new(14, 28));
        r.SpawnPortal(_room14, new(1, 14), _room13, new(28, 15), locked: false);
        r.PlaceButton(_room14, 1, new(15, 15), 1000, 1045);
        r.Summon(_room14, new MonsterSpawn(33, new(16, 10), OnDeath: r =>
            {
                r.Summon(_room14, new MonsterSpawn(27, new(11, 17), OnDeath: r =>
                {
                    r.Summon(_room14, new MonsterSpawn(55, new(21, 18), OnDeath: r =>
                    {
                        r.Summon(_room14, new MonsterSpawn(55, new(12, 9), OnDeath: r =>
                        {
                            r.Summon(_room14, new MonsterSpawn(27, new(16, 19)));
                        }));
                    }));
                }));
            }));
        r.SpawnPortal(_room15, new(14, 1), _room6, new(14, 28), locked: false);
        r.SpawnPortal(_room15, new(1, 14), _room14, new(28, 15), locked: false);
        r.Summon(_room15, new MonsterSpawn(101, new(12, 17), OnDeath: r =>
            {
                r.Summon(_room15,
                new MonsterSpawn(130, new(13, 16), OnDeath: r =>
                {
                    r.Summon(_room15, new MonsterSpawn(101, new(20, 16)));
                }),
                new MonsterSpawn(130, new(16, 14), OnDeath: r =>
                {
                    r.Summon(_room15, new MonsterSpawn(101, new(18, 10)));
                }));
            }));
    }

    public override void OnRoomEntered(IInstanceRun r, int room)
    {
        if (room == _room1)
        {
            r.Message(Game18NConstString.MissionStartWhenEnterFirstRoom);
        }
        else if (room == _room6)
        {
            r.StartRoomClock(_room6, TimeSpan.FromSeconds(60));
            r.At(r.Elapsed + TimeSpan.FromSeconds(60), r =>
            {
                r.Dismiss(_room6, 18);
                r.Dismiss(_room6, 97);
                r.Dismiss(_room6, 130);
                r.UnlockPortal(_room6, _room5);
                r.UnlockPortal(_room6, _room7);
                r.UnlockPortal(_room6, _room15);
                r.AddClockTime(TimeSpan.FromSeconds(150));
                r.Message(Game18NConstString.ADoorHasOpened);
                r.StopRoomClock(_room6);
            });
        }
        else if (room == _room14)
        {
            r.Dialog(_room14, 6028);
        }
    }

    public override void OnRoomCleared(IInstanceRun r, int room)
    {
        if (room == _room11)
        {
            r.UnlockPortal(_room11, _room12);
            r.UnlockPortal(_room11, _room10);
            r.AddClockTime(TimeSpan.FromSeconds(150));
            r.Message(Game18NConstString.ADoorHasOpened);
        }
        else if (room == _room14)
        {
            r.UnlockExit(_room14);
            r.Message(Game18NConstString.ADoorHasOpened);
            r.Dialog(_room14, 6029);
        }
    }

    public override void OnButtonUsed(IInstanceRun r, int room, int button)
    {
        if (room == _room8 && button == 1)
        {
            r.AddClockTime(TimeSpan.FromSeconds(91));
            r.Message(Game18NConstString.DoorOpenedSomewhere);
            r.Message(Game18NConstString.FindKey);
        }
        else if (room == _room13 && button == 1)
        {
            r.UnlockPortal(_room13, _room14);
            r.Message(Game18NConstString.ADoorHasOpened);
        }
    }

    public override void OnExitReached(IInstanceRun r, int room) => r.Succeed();
}

return new TimeSpace6();
