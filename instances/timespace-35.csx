// The Investigation
public class TimeSpace35 : InstanceScript
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

    public TimeSpace35()
    {
        Definition = InstanceDefinitionBuilder.Named(35, "After the clash between Korrkoaren and Kenko, the Kovolts are in despair. The war is not over, and small groups of Kovolts keep fighting Lord Cuarry's troops. Most of the adventurers involved are tired of this war and feel used. Lord Cuarry made an alliance with the demons to win the mining rights, and Kenko betrayed the Koarens to start a war in NosVille. What can the demons be after?", "The Investigation")
            .ForLevels(45, 99)
            .StartingAt(14, 7)
            .Requiring(1012, 4)
            .WithDraw(102, 1)
            .WithDraw(115, 1)
            .WithDraw(128, 1)
            .WithSpecialReward(803, 1)
            .WithReward(2099, 2)
            .WithReward(1087, 1)
            .WithRoom(5301, out _room1, 4, 11)
            .WithRoom(5302, out _room2, 4, 11)
            .WithRoom(5302, out _room3, 4, 10)
            .WithRoom(5301, out _room4, 4, 9)
            .WithRoom(5307, out _room5, 4, 8)
            .WithRoom(5301, out _room6, 3, 8)
            .WithRoom(5301, out _room7, 5, 8)
            .WithRoom(5303, out _room8, 4, 7)
            .WithRoom(5304, out _room9, 4, 6)
            .WithRoom(5302, out _room10, 5, 6)
            .WithRoom(5303, out _room11, 5, 5)
            .WithRoom(5306, out _room12, 5, 4)
            .Build();
    }

    public override void OnFirstEnter(IInstanceRun r)
    {
        r.SpawnPortal(_room1, new(14, 1), _room2, new(15, 28), locked: false);
        r.SpawnPortal(_room2, new(15, 28), _room1, new(14, 1), locked: false);
        r.SpawnPortal(_room2, new(15, 2), _room3, new(15, 28), locked: false);
        r.SpawnPortal(_room3, new(15, 28), _room2, new(15, 2));
        r.SpawnPortal(_room3, new(15, 2), _room4, new(14, 28));
        r.Summon(_room3,
            new MonsterSpawn(83, new(16, 17), IsBonus: true),
            new MonsterSpawn(83, new(18, 13), IsBonus: true, OnDeath: r =>
            {
                r.Summon(_room3, new MonsterSpawn(83, new(11, 13), IsBonus: true, OnDeath: r =>
                {
                    r.Summon(_room3,
                    new MonsterSpawn(83, new(22, 14)),
                    new MonsterSpawn(83, new(12, 20), IsBonus: true));
                }));
            }));
        r.SpawnPortal(_room4, new(1, 15), _room5, new(28, 16), locked: false);
        r.SpawnPortal(_room4, new(14, 28), _room3, new(15, 2), locked: false);
        r.SpawnPortal(_room5, new(28, 16), _room6, new(1, 15));
        r.Summon(_room5,
            new MonsterSpawn(144, new(17, 19), OnDeath: r =>
            {
                r.Summon(_room5,
                new MonsterSpawn(139, new(11, 19), IsBonus: true),
                new MonsterSpawn(139, new(11, 14)));
            }),
            new MonsterSpawn(144, new(14, 8), IsBonus: true, OnDeath: r =>
            {
                r.Summon(_room5, new MonsterSpawn(139, new(10, 10), IsBonus: true));
            }),
            new MonsterSpawn(144, new(21, 13), IsBonus: true));
        r.SpawnPortal(_room6, new(1, 15), _room5, new(28, 16), locked: false);
        r.SpawnPortal(_room6, new(28, 15), _room6, new(28, 15), locked: false);
        r.SpawnPortal(_room7, new(28, 15), _room6, new(2, 0), locked: false);
        r.SpawnPortal(_room7, new(14, 1), _room7, new(14, 1), locked: false);
        r.SpawnPortal(_room8, new(14, 28), _room7, new(15, 0), locked: false);
        r.SpawnPortal(_room8, new(28, 15), _room9, new(1, 15), locked: false);
        r.SpawnPortal(_room9, new(1, 15), _room8, new(28, 15));
        r.SpawnPortal(_room9, new(14, 1), _room10, new(15, 28));
        r.Summon(_room9,
            new MonsterSpawn(143, new(14, 9), IsBonus: true),
            new MonsterSpawn(143, new(17, 9), IsBonus: true, OnDeath: r =>
            {
                r.Summon(_room9,
                new MonsterSpawn(60, new(21, 17), IsBonus: true, OnDeath: r =>
                {
                    r.Summon(_room9,
                    new MonsterSpawn(61, new(20, 13), IsBonus: true),
                    new MonsterSpawn(61, new(10, 13), IsBonus: true));
                }),
                new MonsterSpawn(60, new(15, 16), OnDeath: r =>
                {
                    r.Summon(_room9, new MonsterSpawn(60, new(8, 15), IsBonus: true));
                }));
            }));
        r.SpawnPortal(_room10, new(15, 28), _room9, new(14, 1), locked: false);
        r.SpawnPortal(_room10, new(15, 2), _room11, new(14, 28), locked: false);
        r.SpawnPortal(_room11, new(14, 28), _room10, new(15, 2));
        r.SpawnPortal(_room11, new(28, 15), _room12, new(2, 16));
        r.Summon(_room11,
            new MonsterSpawn(122, new(6, 16)),
            new MonsterSpawn(122, new(8, 10)),
            new MonsterSpawn(116, new(10, 11)),
            new MonsterSpawn(122, new(10, 18)),
            new MonsterSpawn(116, new(11, 17)),
            new MonsterSpawn(122, new(12, 8)),
            new MonsterSpawn(119, new(12, 14)),
            new MonsterSpawn(116, new(12, 12)),
            new MonsterSpawn(116, new(15, 17)),
            new MonsterSpawn(119, new(17, 14)),
            new MonsterSpawn(116, new(18, 11)),
            new MonsterSpawn(116, new(19, 17)),
            new MonsterSpawn(122, new(19, 20)),
            new MonsterSpawn(122, new(20, 8)),
            new MonsterSpawn(119, new(21, 11)),
            new MonsterSpawn(119, new(23, 14)),
            new MonsterSpawn(119, new(21, 19)));
        r.SpawnPortal(_room12, new(2, 16), _room11, new(28, 15), locked: false);
        r.SpawnExit(_room12, new(15, 28), locked: false);
    }

    public override void OnRoomEntered(IInstanceRun r, int room)
    {
        if (room == _room1)
        {
            r.Message(Game18NConstString.MissionStartWhenEnterFirstRoom);
        }
        else if (room == _room2)
        {
            r.StartClock(TimeSpan.FromSeconds(150));
        }
        else if (room == _room3)
        {
            r.Dialog(_room3, 6145);
        }
        else if (room == _room5)
        {
            r.Dialog(_room5, 6146);
        }
        else if (room == _room7)
        {
            r.Message(Game18NConstString.QuickMissionComplete);
            r.Message(Game18NConstString.ADoorHasOpened);
        }
        else if (room == _room9)
        {
            r.Dialog(_room9, 6148);
        }
        else if (room == _room11)
        {
            r.StartRoomClock(_room11, TimeSpan.FromSeconds(30));
        }
        else if (room == _room12)
        {
            r.Dialog(_room12, 6150);
        }
    }

    public override void OnRoomCleared(IInstanceRun r, int room)
    {
        if (room == _room3)
        {
            r.AddClockTime(TimeSpan.FromSeconds(150));
            r.Message(Game18NConstString.ADoorHasOpened);
            r.UnlockPortal(_room3, _room2);
            r.UnlockPortal(_room3, _room4);
        }
        else if (room == _room5)
        {
            r.AddClockTime(TimeSpan.FromSeconds(100));
            r.Message(Game18NConstString.ADoorHasOpened);
            r.UnlockPortal(_room5, _room6);
        }
        else if (room == _room9)
        {
            r.AddClockTime(TimeSpan.FromSeconds(140));
            r.Message(Game18NConstString.ADoorHasOpened);
            r.UnlockPortal(_room9, _room8);
            r.UnlockPortal(_room9, _room10);
        }
        else if (room == _room11)
        {
            r.StopRoomClock(_room11);
            r.Dialog(_room11, 6149);
            r.Message(Game18NConstString.ADoorHasOpened);
            r.UnlockPortal(_room11, _room10);
            r.UnlockPortal(_room11, _room12);
        }
    }

    public override void OnExitReached(IInstanceRun r, int room) => r.Succeed();

    public override void OnTimeout(IInstanceRun r) => r.Fail();
}

return new TimeSpace35();
