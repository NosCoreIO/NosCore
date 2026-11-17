public class TimeSpace626 : InstanceScript
{
    private readonly int _room1;
    private readonly int _room2;
    private readonly int _room3;

    public TimeSpace626()
    {
        Definition = InstanceDefinitionBuilder.Named(626, "Gate 3", "Gate 3")
            .ForLevels(30, 99)
            .StartingAt(11, 11)
            .WithLives(1)
            .WithObjective(ObjectiveKind.KillMonster, 355, 1)
            .WithRoom(4201, out _room1, 3, 11)
            .WithRoom(4206, out _room2, 3, 10)
            .WithRoom(4203, out _room3, 3, 9)
            .Build();
    }

    public override void OnFirstEnter(IInstanceRun r)
    {
        r.StartClock(TimeSpan.FromSeconds(300));
        r.SpawnPortal(_room1, new(14, 1), _room2, new(14, 28), locked: false);
        r.SpawnPortal(_room2, new(14, 1), _room3, new(14, 28));
        r.SpawnPortal(_room2, new(14, 28), _room1, new(14, 1));
        r.Summon(_room2,
            new MonsterSpawn(381, new(18, 8)),
            new MonsterSpawn(381, new(10, 8)));
        r.SpawnPortal(_room3, new(14, 28), _room2, new(14, 1));
        r.Summon(_room3,
            new MonsterSpawn(381, new(21, 12), IsBonus: true, OnDeath: r =>
            {
                r.Summon(_room3, new MonsterSpawn(355, new(15, 9), IsBonus: true));
            }),
            new MonsterSpawn(381, new(18, 12), IsBonus: true),
            new MonsterSpawn(381, new(12, 12)),
            new MonsterSpawn(381, new(9, 12), IsBonus: true));
    }

    public override void OnRoomEntered(IInstanceRun r, int room)
    {
        if (room == _room1)
        {
            r.Message(Game18NConstString.MissionStartWhenEnterFirstRoom);
        }
        else if (room == _room2)
        {
            r.StartRoomClock(_room2, TimeSpan.FromSeconds(190));
            r.Message(Game18NConstString.EnemiesReinforcements);
            r.Summon(_room2,
                new MonsterSpawn(381, new(11, 14)),
                new MonsterSpawn(381, new(11, 14)));
            r.At(r.Elapsed + TimeSpan.FromSeconds(30), r =>
            {
                r.Summon(_room2,
                    new MonsterSpawn(381, new(11, 14)),
                    new MonsterSpawn(381, new(11, 14)));
            });
            r.At(r.Elapsed + TimeSpan.FromSeconds(60), r =>
            {
                r.Summon(_room2,
                    new MonsterSpawn(381, new(11, 14)),
                    new MonsterSpawn(381, new(11, 14)),
                    new MonsterSpawn(381, new(15, 10)));
            });
            r.At(r.Elapsed + TimeSpan.FromSeconds(90), r =>
            {
                r.Summon(_room2,
                    new MonsterSpawn(381, new(11, 14)),
                    new MonsterSpawn(381, new(11, 14)),
                    new MonsterSpawn(381, new(15, 10)));
            });
            r.At(r.Elapsed + TimeSpan.FromSeconds(120), r =>
            {
                r.Summon(_room2,
                    new MonsterSpawn(381, new(11, 14)),
                    new MonsterSpawn(381, new(11, 14)),
                    new MonsterSpawn(381, new(15, 10)),
                    new MonsterSpawn(381, new(19, 14)),
                    new MonsterSpawn(381, new(19, 14)),
                    new MonsterSpawn(381, new(19, 14)));
            });
            r.At(r.Elapsed + TimeSpan.FromSeconds(150), r =>
            {
                r.Summon(_room2,
                    new MonsterSpawn(381, new(15, 10)),
                    new MonsterSpawn(381, new(19, 14)),
                    new MonsterSpawn(381, new(19, 14)),
                    new MonsterSpawn(381, new(19, 14)));
            });
            r.At(r.Elapsed + TimeSpan.FromSeconds(180), r =>
            {
                r.Summon(_room2,
                    new MonsterSpawn(381, new(15, 10)),
                    new MonsterSpawn(381, new(19, 14)),
                    new MonsterSpawn(381, new(19, 14)),
                    new MonsterSpawn(381, new(19, 14)));
            });
            r.At(r.Elapsed + TimeSpan.FromSeconds(190), r =>
            {
                r.Dismiss(_room2, 381);
                r.UnlockPortal(_room2, _room3);
                r.UnlockPortal(_room2, _room1);
                r.Message(Game18NConstString.ADoorHasOpened);
                r.Message(Game18NConstString.QuickMissionComplete);
                r.Message(Game18NConstString.NewGoal);
                r.StopRoomClock(_room2);
            });
        }
    }

    public override void OnRoomCleared(IInstanceRun r, int room)
    {
        if (room == _room3)
        {
            r.Message(Game18NConstString.QuickMissionComplete);
            r.Message(Game18NConstString.NewGoal);
            r.Message(Game18NConstString.QuestComplete);
            r.Succeed();
        }
    }

    public override void OnExitReached(IInstanceRun r, int room) => r.Succeed();
}

return new TimeSpace626();
