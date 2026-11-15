// The Invincible Kenko
public class TimeSpace25 : InstanceScript
{
    private readonly int _room1;
    private readonly int _room2;
    private readonly int _room3;
    private readonly int _room4;
    private readonly int _room5;
    private readonly int _room6;

    public TimeSpace25()
    {
        Definition = InstanceDefinitionBuilder.Named(25, "The Kovolts are not beaten yet and are preparing a new attack. Stop them! With your support the war will soon be over.", "The Invincible Kenko")
            .ForLevels(35, 99)
            .StartingAt(12, 6)
            .Requiring(1012, 3)
            .WithDraw(518, 1)
            .WithDraw(234, 1)
            .WithSpecialReward(2043, 15)
            .WithReward(2099, 2)
            .WithReward(1087, 1)
            .WithRoom(5206, out _room1, 3, 11)
            .WithRoom(5206, out _room2, 3, 11)
            .WithRoom(5206, out _room3, 3, 10)
            .WithRoom(5206, out _room4, 3, 9)
            .WithRoom(5206, out _room5, 3, 8)
            .WithRoom(5206, out _room6, 3, 7)
            .Build();
    }

    public override void OnFirstEnter(IInstanceRun r)
    {
        r.SpawnPortal(_room1, new(12, 1), _room2, new(12, 23), locked: false);
        r.SpawnPortal(_room2, new(12, 23), _room1, new(12, 1));
        r.SpawnPortal(_room2, new(12, 1), _room3, new(12, 23));
        r.Summon(_room2,
            new MonsterSpawn(115, new(9, 15), OnDeath: r =>
            {
                r.Summon(_room2, new MonsterSpawn(118, new(14, 15), IsBonus: true));
            }),
            new MonsterSpawn(115, new(10, 17), OnDeath: r =>
            {
                r.Summon(_room2, new MonsterSpawn(118, new(13, 19)));
            }),
            new MonsterSpawn(118, new(16, 11)));
        r.SpawnPortal(_room3, new(12, 23), _room2, new(12, 1));
        r.SpawnPortal(_room3, new(12, 1), _room4, new(12, 23));
        r.Summon(_room3,
            new MonsterSpawn(118, new(12, 12), IsBonus: true, OnDeath: r =>
            {
                r.Summon(_room3,
                new MonsterSpawn(121, new(13, 15), OnDeath: r =>
                {
                    r.Summon(_room3,
                    new MonsterSpawn(121, new(13, 10)),
                    new MonsterSpawn(121, new(9, 10), IsBonus: true));
                }),
                new MonsterSpawn(121, new(8, 14), IsBonus: true));
            }),
            new MonsterSpawn(118, new(15, 13), IsBonus: true),
            new MonsterSpawn(118, new(15, 18)));
        r.SpawnPortal(_room4, new(12, 23), _room3, new(12, 1));
        r.SpawnPortal(_room4, new(12, 1), _room5, new(12, 23));
        r.Summon(_room4,
            new MonsterSpawn(118, new(11, 18), OnDeath: r =>
            {
                r.Summon(_room4, new MonsterSpawn(118, new(9, 15)));
            }),
            new MonsterSpawn(118, new(12, 16), OnDeath: r =>
            {
                r.Summon(_room4, new MonsterSpawn(121, new(14, 18), IsBonus: true, OnDeath: r =>
                {
                    r.Summon(_room4,
                    new MonsterSpawn(121, new(14, 13), IsBonus: true, OnDeath: r =>
                    {
                        r.Summon(_room4, new MonsterSpawn(121, new(16, 12), IsBonus: true, OnDeath: r =>
                        {
                            r.Summon(_room4, new MonsterSpawn(118, new(14, 16), IsBonus: true));
                        }));
                    }),
                    new MonsterSpawn(121, new(8, 9), OnDeath: r =>
                    {
                        r.Summon(_room4, new MonsterSpawn(121, new(9, 14), IsBonus: true));
                    }));
                }));
            }));
        r.SpawnPortal(_room5, new(12, 23), _room4, new(12, 1));
        r.SpawnPortal(_room5, new(12, 1), _room6, new(12, 23));
        r.Summon(_room5,
            new MonsterSpawn(118, new(9, 11), OnDeath: r =>
            {
                r.Summon(_room5,
                new MonsterSpawn(118, new(12, 15)),
                new MonsterSpawn(115, new(10, 17), OnDeath: r =>
                {
                    r.Summon(_room5, new MonsterSpawn(115, new(14, 17), IsBonus: true));
                }));
            }),
            new MonsterSpawn(118, new(15, 14)),
            new MonsterSpawn(118, new(17, 10), IsBonus: true, OnDeath: r =>
            {
                r.Summon(_room5,
                new MonsterSpawn(115, new(16, 15), IsBonus: true),
                new MonsterSpawn(115, new(13, 17)));
            }));
        r.SpawnPortal(_room6, new(12, 23), _room5, new(12, 1));
        r.SpawnExit(_room6, new(12, 1));
        r.Summon(_room6,
            new MonsterSpawn(115, new(8, 11)),
            new MonsterSpawn(118, new(9, 9)),
            new MonsterSpawn(316, new(15, 18), IsBonus: true),
            new MonsterSpawn(118, new(16, 10)));
    }

    public override void OnRoomEntered(IInstanceRun r, int room)
    {
        if (room == _room1)
        {
            r.Message(Game18NConstString.MissionStartWhenEnterFirstRoom);
        }
        else if (room == _room2)
        {
            r.StartClock(TimeSpan.FromSeconds(250));
        }
        else if (room == _room6)
        {
            r.Dialog(_room6, 6123);
            r.StartRoomClock(_room6, TimeSpan.FromSeconds(120));
        }
    }

    public override void OnRoomCleared(IInstanceRun r, int room)
    {
        if (room == _room2)
        {
            r.Message(Game18NConstString.QuickMissionComplete);
            r.AddClockTime(TimeSpan.FromSeconds(60));
            r.Message(Game18NConstString.ADoorHasOpened);
            r.UnlockPortal(_room2, _room3);
        }
        else if (room == _room3)
        {
            r.Message(Game18NConstString.QuickMissionComplete);
            r.AddClockTime(TimeSpan.FromSeconds(80));
            r.Message(Game18NConstString.ADoorHasOpened);
            r.UnlockPortal(_room3, _room4);
        }
        else if (room == _room4)
        {
            r.Message(Game18NConstString.QuickMissionComplete);
            r.AddClockTime(TimeSpan.FromSeconds(80));
            r.Message(Game18NConstString.ADoorHasOpened);
            r.UnlockPortal(_room4, _room5);
        }
        else if (room == _room5)
        {
            r.Message(Game18NConstString.QuickMissionComplete);
            r.AddClockTime(TimeSpan.FromSeconds(200));
            r.Message(Game18NConstString.ADoorHasOpened);
            r.UnlockPortal(_room5, _room6);
        }
        else if (room == _room6)
        {
            r.StopRoomClock(_room6);
            r.Message(Game18NConstString.QuickMissionComplete);
            r.Message(Game18NConstString.ADoorHasOpened);
            r.UnlockExit(_room6);
        }
    }

    public override void OnExitReached(IInstanceRun r, int room) => r.Succeed();

    public override void OnTimeout(IInstanceRun r) => r.Fail();
}

return new TimeSpace25();
