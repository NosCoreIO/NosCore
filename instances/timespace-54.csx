// The Koarens' Plan
public class TimeSpace54 : InstanceScript
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

    public TimeSpace54()
    {
        Definition = InstanceDefinitionBuilder.Named(54, "Without the Koarens' consent, Kenel follows you to get a look at the plan. Kenel is a powerful soldier... there's no telling how much longer we can hold him back. Our only hope is to find the Whitethorn witches as fast as possible and give the plan to Cenil.", "The Koarens' Plan")
            .ForLevels(60, 99)
            .StartingAt(7, 13)
            .WithLives(2)
            .Requiring(1012, 6)
            .WithDraw(720, 1)
            .WithSpecialReward(1027, 99)
            .WithReward(2003, 4)
            .WithReward(1087, 1)
            .WithRoom(4814, out _room1, 0, 7)
            .WithRoom(4809, out _room2, 0, 7)
            .WithRoom(4802, out _room3, 1, 7)
            .WithRoom(4808, out _room4, 1, 8)
            .WithRoom(4802, out _room5, 1, 9)
            .WithRoom(4802, out _room6, 2, 8)
            .WithRoom(4810, out _room7, 1, 10)
            .WithRoom(4812, out _room8, 2, 10)
            .WithRoom(4800, out _room9, 2, 9)
            .WithRoom(4811, out _room10, 3, 9)
            .WithRoom(4810, out _room11, 3, 10)
            .WithRoom(4804, out _room12, 4, 10)
            .WithRoom(4800, out _room13, 4, 9)
            .WithRoom(4806, out _room14, 3, 8)
            .WithRoom(4809, out _room15, 4, 8)
            .WithRoom(4805, out _room16, 4, 7)
            .WithRoom(4811, out _room17, 3, 7)
            .Build();
    }

    public override void OnFirstEnter(IInstanceRun r)
    {
        r.SpawnPortal(_room1, new(29, 14), _room2, new(1, 14), locked: false);
        r.SpawnPortal(_room2, new(1, 14), _room1, new(29, 14));
        r.SpawnPortal(_room2, new(15, 28), _room3, new(15, 0));
        r.Summon(_room2,
            new MonsterSpawn(315, new(14, 12), IsBonus: true, OnDeath: r =>
            {
                r.Message(Game18NConstString.ADoorHasOpened);
                r.UnlockPortal(_room2, _room1);
                r.UnlockPortal(_room2, _room3);
            }),
            new MonsterSpawn(116, new(14, 9)),
            new MonsterSpawn(119, new(16, 7)),
            new MonsterSpawn(116, new(17, 14)),
            new MonsterSpawn(119, new(19, 12), OnDeath: r =>
            {
                r.Summon(_room2, new MonsterSpawn(122, new(17, 10)));
            }),
            new MonsterSpawn(119, new(21, 19), OnDeath: r =>
            {
                r.Summon(_room2, new MonsterSpawn(122, new(17, 16), OnDeath: r =>
                {
                    r.Summon(_room2, new MonsterSpawn(122, new(14, 7)));
                }));
            }));
        r.SpawnPortal(_room3, new(15, 28), _room4, new(15, 0), locked: false);
        r.SpawnPortal(_room3, new(15, 0), _room2, new(15, 28), locked: false);
        r.SpawnPortal(_room4, new(15, 0), _room5, new(15, 28));
        r.Summon(_room4,
            new MonsterSpawn(315, new(14, 12), OnDeath: r =>
            {
                r.Summon(_room4, new MonsterSpawn(122, new(17, 16)));
            }),
            new MonsterSpawn(116, new(14, 9)),
            new MonsterSpawn(119, new(16, 7)),
            new MonsterSpawn(116, new(17, 14), IsBonus: true, OnDeath: r =>
            {
                r.AddClockTime(TimeSpan.FromSeconds(200));
                r.Message(Game18NConstString.ADoorHasOpened);
                r.UnlockPortal(_room4, _room5);
                r.Dismiss(_room4, 116);
                r.Dismiss(_room4, 119);
                r.Dismiss(_room4, 122);
                r.Dismiss(_room4, 315);
                r.Summon(_room4, new MonsterSpawn(122, new(17, 10)));
            }),
            new MonsterSpawn(119, new(19, 12)),
            new MonsterSpawn(119, new(21, 19)));
        r.SpawnPortal(_room5, new(15, 28), _room4, new(15, 0), locked: false);
        r.SpawnPortal(_room5, new(29, 14), _room5, new(29, 14), locked: false);
        r.SpawnPortal(_room6, new(15, 28), _room6, new(15, 28), locked: false);
        r.SpawnPortal(_room6, new(29, 14), _room5, new(1, 0), locked: false);
        r.SpawnPortal(_room7, new(1, 14), _room6, new(29, 0), locked: false);
        r.SpawnPortal(_room7, new(15, 0), _room8, new(15, 28), locked: false);
        r.SpawnPortal(_room8, new(15, 28), _room7, new(15, 0), locked: false);
        r.SpawnPortal(_room8, new(29, 14), _room9, new(1, 14), locked: false);
        r.SpawnPortal(_room9, new(1, 14), _room8, new(29, 14));
        r.SpawnPortal(_room9, new(15, 28), _room10, new(15, 0));
        r.Summon(_room9,
            new MonsterSpawn(122, new(17, 16), OnDeath: r =>
            {
                r.Summon(_room9, new MonsterSpawn(122, new(17, 10)));
            }),
            new MonsterSpawn(315, new(14, 12), IsBonus: true, OnDeath: r =>
            {
                r.AddClockTime(TimeSpan.FromSeconds(200));
                r.Message(Game18NConstString.ADoorHasOpened);
                r.UnlockPortal(_room9, _room8);
                r.UnlockPortal(_room9, _room10);
            }),
            new MonsterSpawn(116, new(14, 9)),
            new MonsterSpawn(119, new(16, 7), IsBonus: true),
            new MonsterSpawn(116, new(17, 14), IsBonus: true),
            new MonsterSpawn(119, new(19, 12)),
            new MonsterSpawn(119, new(21, 19), OnDeath: r =>
            {
                r.Summon(_room9, new MonsterSpawn(122, new(14, 7)));
            }));
        r.SpawnPortal(_room10, new(29, 14), _room11, new(1, 14), locked: false);
        r.SpawnPortal(_room10, new(15, 0), _room9, new(15, 28), locked: false);
        r.SpawnPortal(_room11, new(1, 14), _room10, new(29, 14), locked: false);
        r.SpawnPortal(_room11, new(15, 0), _room12, new(15, 28), locked: false);
        r.SpawnPortal(_room12, new(1, 14), _room13, new(28, 14), locked: false);
        r.SpawnPortal(_room12, new(15, 28), _room11, new(15, 0), locked: false);
        r.SpawnPortal(_room13, new(28, 14), _room12, new(1, 14), locked: false);
        r.SpawnPortal(_room13, new(15, 1), _room13, new(15, 1), locked: false);
        r.SpawnPortal(_room14, new(1, 14), _room14, new(1, 14), locked: false);
        r.SpawnPortal(_room14, new(29, 14), _room13, new(1, 0));
        r.SpawnPortal(_room15, new(1, 14), _room16, new(28, 14), locked: false);
        r.SpawnPortal(_room15, new(15, 28), _room14, new(15, 0), locked: false);
        r.SpawnPortal(_room16, new(1, 14), _room17, new(29, 14), locked: false);
        r.SpawnPortal(_room16, new(28, 14), _room15, new(1, 14), locked: false);
        r.SpawnPortal(_room17, new(29, 14), _room16, new(1, 14));
        r.SpawnExit(_room17, new(15, 0));
        r.Summon(_room17,
            new MonsterSpawn(315, new(14, 12), IsBonus: true, OnDeath: r =>
            {
                r.Dialog(_room17, 6275);
                r.Message(Game18NConstString.ADoorHasOpened);
                r.UnlockPortal(_room17, _room16);
                r.UnlockExit(_room17);
            }),
            new MonsterSpawn(119, new(13, 10), IsBonus: true, OnDeath: r =>
            {
                r.Summon(_room17,
                new MonsterSpawn(116, new(21, 18), OnDeath: r =>
                {
                    r.Summon(_room17, new MonsterSpawn(122, new(12, 20)));
                }),
                new MonsterSpawn(116, new(21, 14)));
            }),
            new MonsterSpawn(116, new(18, 20), OnDeath: r =>
            {
                r.Summon(_room17, new MonsterSpawn(122, new(17, 17), OnDeath: r =>
                {
                    r.Summon(_room17, new MonsterSpawn(122, new(12, 7)));
                }));
            }),
            new MonsterSpawn(116, new(22, 10), IsBonus: true, OnDeath: r =>
            {
                r.Summon(_room17, new MonsterSpawn(116, new(19, 6)));
            }));
        r.SummonNpcs(_room17, new NpcSpawn(335, new(18, 13)), new NpcSpawn(247, new(19, 10)), new NpcSpawn(247, new(19, 15)));
    }

    public override void OnRoomEntered(IInstanceRun r, int room)
    {
        if (room == _room1)
        {
            r.Message(Game18NConstString.MissionStartWhenEnterFirstRoom);
        }
        else if (room == _room2)
        {
            r.StartClock(TimeSpan.FromSeconds(300));
            r.Dialog(_room2, 6270);
        }
        else if (room == _room4)
        {
            r.Dialog(_room4, 6271);
        }
        else if (room == _room9)
        {
            r.Dialog(_room9, 6272);
        }
        else if (room == _room17)
        {
            r.Dialog(_room17, 6274);
        }
    }

    public override void OnExitReached(IInstanceRun r, int room) => r.Succeed();

    public override void OnTimeout(IInstanceRun r) => r.Fail();
}

return new TimeSpace54();
