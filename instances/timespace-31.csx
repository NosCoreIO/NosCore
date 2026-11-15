// Two Koaren Heroes
public class TimeSpace31 : InstanceScript
{
    private readonly int _room1;
    private readonly int _room2;
    private readonly int _room3;
    private readonly int _room4;
    private readonly int _room5;
    private readonly int _room6;
    private readonly int _room7;

    public TimeSpace31()
    {
        Definition = InstanceDefinitionBuilder.Named(31, "Korrkoaren and Kenel can't be far. It's only a matter of time before we find them. Korrkoaren said something strange... he called our adventurers lackeys of the demons. Is something going on between the demons and Lord Cuarry? Does Korrkoaren know? Let's catch them and find out.", "Two Koaren Heroes")
            .ForLevels(41, 99)
            .StartingAt(7, 13)
            .WithLives(1)
            .Requiring(1012, 4)
            .WithDraw(712, 1)
            .WithDraw(1028, 2)
            .WithSpecialReward(802, 1)
            .WithReward(2134, 1)
            .WithReward(1087, 1)
            .WithRoom(4202, out _room1, 0, 8)
            .WithRoom(4205, out _room2, 0, 8)
            .WithRoom(4209, out _room3, 1, 8)
            .WithRoom(4207, out _room4, 2, 8)
            .WithRoom(4205, out _room5, 2, 9)
            .WithRoom(4205, out _room6, 3, 9)
            .WithRoom(4205, out _room7, 4, 9)
            .Build();
    }

    public override void OnFirstEnter(IInstanceRun r)
    {
        r.SpawnPortal(_room1, new(28, 15), _room2, new(1, 15), locked: false);
        r.SpawnPortal(_room2, new(1, 15), _room1, new(28, 15));
        r.SpawnPortal(_room2, new(28, 15), _room3, new(1, 15));
        r.Summon(_room2, new MonsterSpawn(329, new(17, 13)));
        r.SpawnPortal(_room3, new(1, 15), _room2, new(28, 15));
        r.SpawnPortal(_room3, new(14, 28), _room4, new(14, 1));
        r.Summon(_room3, new MonsterSpawn(315, new(16, 13), IsBonus: true, OnDeath: r =>
            {
                r.Message(Game18NConstString.QuickMissionComplete);
                r.Message(Game18NConstString.ADoorHasOpened);
                r.UnlockPortal(_room3, _room4);
            }));
        r.SpawnPortal(_room4, new(28, 15), _room5, new(1, 15));
        r.SpawnPortal(_room4, new(14, 1), _room3, new(14, 28));
        r.Summon(_room4, new MonsterSpawn(315, new(13, 15), IsBonus: true, OnDeath: r =>
            {
                r.Message(Game18NConstString.QuickMissionComplete);
                r.Message(Game18NConstString.ADoorHasOpened);
                r.UnlockPortal(_room4, _room5);
            }));
        r.SpawnPortal(_room5, new(1, 15), _room4, new(28, 15));
        r.SpawnPortal(_room5, new(28, 15), _room6, new(1, 15));
        r.Summon(_room5,
            new MonsterSpawn(67, new(10, 12)),
            new MonsterSpawn(67, new(15, 6)),
            new MonsterSpawn(67, new(19, 11), IsBonus: true, OnDeath: r =>
            {
                r.Summon(_room5,
                new MonsterSpawn(67, new(18, 18), IsBonus: true, OnDeath: r =>
                {
                    r.Summon(_room5, new MonsterSpawn(143, new(14, 19), IsBonus: true, OnDeath: r =>
                    {
                        r.Message(Game18NConstString.QuickMissionComplete);
                        r.Message(Game18NConstString.ADoorHasOpened);
                        r.UnlockPortal(_room5, _room6);
                    }));
                }),
                new MonsterSpawn(67, new(15, 21), OnDeath: r =>
                {
                    r.Summon(_room5, new MonsterSpawn(143, new(14, 14)));
                }),
                new MonsterSpawn(67, new(11, 18), OnDeath: r =>
                {
                    r.Summon(_room5, new MonsterSpawn(143, new(15, 10)));
                }));
            }));
        r.SpawnPortal(_room6, new(1, 15), _room5, new(28, 15));
        r.SpawnPortal(_room6, new(28, 15), _room7, new(1, 15));
        r.Summon(_room6,
            new MonsterSpawn(67, new(12, 5), IsBonus: true, OnDeath: r =>
            {
                r.Summon(_room6, new MonsterSpawn(84, new(19, 19), OnDeath: r =>
                {
                    r.Summon(_room6, new MonsterSpawn(84, new(18, 11), IsBonus: true, OnDeath: r =>
                    {
                        r.Dialog(_room6, 6142);
                        r.Message(Game18NConstString.ADoorHasOpened);
                        r.UnlockPortal(_room6, _room7);
                    }));
                }));
            }),
            new MonsterSpawn(67, new(13, 22), OnDeath: r =>
            {
                r.Summon(_room6, new MonsterSpawn(67, new(12, 19)));
            }),
            new MonsterSpawn(67, new(14, 12), OnDeath: r =>
            {
                r.Summon(_room6, new MonsterSpawn(67, new(14, 8), OnDeath: r =>
                {
                    r.Summon(_room6, new MonsterSpawn(67, new(13, 25), IsBonus: true, OnDeath: r =>
                    {
                        r.Summon(_room6, new MonsterSpawn(84, new(21, 15), IsBonus: true));
                    }));
                }));
            }));
        r.SpawnPortal(_room7, new(1, 15), _room6, new(28, 15));
        r.SpawnExit(_room7, new(28, 15));
        r.Summon(_room7,
            new MonsterSpawn(137, new(8, 15)),
            new MonsterSpawn(137, new(9, 11)),
            new MonsterSpawn(137, new(9, 20), IsBonus: true),
            new MonsterSpawn(137, new(13, 10)),
            new MonsterSpawn(137, new(14, 21), OnDeath: r =>
            {
                r.Summon(_room7,
                new MonsterSpawn(144, new(20, 18), OnDeath: r =>
                {
                    r.Summon(_room7,
                    new MonsterSpawn(61, new(17, 14), IsBonus: true, OnDeath: r =>
                    {
                        r.Summon(_room7, new MonsterSpawn(61, new(11, 13), IsBonus: true));
                    }),
                    new MonsterSpawn(61, new(16, 18), OnDeath: r =>
                    {
                        r.Summon(_room7, new MonsterSpawn(61, new(12, 17), IsBonus: true));
                    }));
                }),
                new MonsterSpawn(144, new(19, 22)));
            }),
            new MonsterSpawn(137, new(17, 11), IsBonus: true),
            new MonsterSpawn(137, new(17, 20), OnDeath: r =>
            {
                r.Summon(_room7, new MonsterSpawn(144, new(21, 12), IsBonus: true, OnDeath: r =>
                {
                    r.Dialog(_room7, 6144);
                    r.Message(Game18NConstString.ADoorHasOpened);
                    r.UnlockExit(_room7);
                }));
            }),
            new MonsterSpawn(137, new(19, 16), OnDeath: r =>
            {
                r.Summon(_room7, new MonsterSpawn(144, new(19, 8), IsBonus: true, OnDeath: r =>
                {
                    r.Summon(_room7, new MonsterSpawn(61, new(14, 12)));
                }));
            }));
        r.SummonNpcs(_room7, new NpcSpawn(315, new(7, 11)), new NpcSpawn(307, new(14, 15)));
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
            r.Dialog(_room2, 6139);
            r.Message(Game18NConstString.ADoorHasOpened);
            r.UnlockPortal(_room2, _room3);
        }
        else if (room == _room3)
        {
            r.Dialog(_room3, 6140);
        }
        else if (room == _room4)
        {
            r.Dialog(_room4, 6141);
        }
        else if (room == _room7)
        {
            r.Dialog(_room7, 6143);
        }
    }

    public override void OnExitReached(IInstanceRun r, int room) => r.Succeed();

    public override void OnTimeout(IInstanceRun r) => r.Fail();
}

return new TimeSpace31();
