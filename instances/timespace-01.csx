// Time-space 1. Dropped in here; nothing is rebuilt.
using NosCore.Instances.Abstractions;

public class TimeSpace01 : InstanceScript
{
    public override void OnFirstEnter(IInstanceRun run)
    {
        run.StartClock(TimeSpan.FromMinutes(20));
        run.Message("Clear each room to open the way.");

        run.Summon(0,
            new MonsterSpawn(58, new Spot(24, 12)),
            new MonsterSpawn(58, new Spot(28, 14)),
            new MonsterSpawn(59, new Spot(26, 18)));
        run.SetMonsterLocker(0, 3);
        run.SpawnPortal(0, 1, new Spot(30, 20));
    }

    public override void OnRoomCleared(IInstanceRun run, int room)
    {
        if (room == 0)
        {
            run.UnlockPortal(0, 1);
            run.Effect(0, 5000);
            return;
        }

        if (room == 1)
        {
            run.Summon(1, new MonsterSpawn(200, new Spot(20, 20), IsBoss: true));
            run.SetMonsterLocker(1, 1);
        }
    }

    public override void OnMonsterKilled(IInstanceRun run, short vNum)
    {
        if (vNum == 200)
        {
            run.Message("The way out is open.");
            run.Succeed();
        }
    }

    public override void OnTimeout(IInstanceRun run) => run.Fail();
}

return new TimeSpace01();
