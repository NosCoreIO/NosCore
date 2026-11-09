public class TimeSpace01 : InstanceScript
{
    private readonly int _hall;
    private readonly int _lair;

    public TimeSpace01()
    {
        Definition = InstanceDefinitionBuilder.Named(1, "timespace-01", "timespace-01")
            .ForLevels(1, 99)
            .StartingAt(20, 33)
            .WithRoom(2550, out _hall, 0, 0)
            .WithRoom(2550, out _lair, 1, 0)
            .Build();
    }

    public override void OnFirstEnter(IInstanceRun run)
    {
        run.StartClock(TimeSpan.FromMinutes(10));
        run.Message(Game18NConstString.EliminateAllMonsters);

        run.Summon(_hall,
            new MonsterSpawn(58, new Spot(24, 31)),
            new MonsterSpawn(58, new Spot(25, 35)),
            new MonsterSpawn(59, new Spot(27, 33)));
        run.SetMonsterLocker(_hall, 3);
        run.SpawnPortal(_hall, new Spot(30, 33), _lair, new Spot(20, 33));
    }

    public override void OnRoomCleared(IInstanceRun run, int room)
    {
        if (room == _hall)
        {
            run.UnlockPortal(_hall, _lair);
            run.Summon(_lair, new MonsterSpawn(200, new Spot(26, 33)));
            run.SetMonsterLocker(_lair, 1);
            return;
        }

        run.Message(Game18NConstString.TimeSpaceCompleted);
        run.Succeed();
    }

    public override void OnTimeout(IInstanceRun run) => run.Fail();
}

return new TimeSpace01();
