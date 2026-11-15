// The Duel
public class TimeSpace21 : InstanceScript
{
    private readonly int _room1;
    private readonly int _room2;
    private readonly int _room3;
    private readonly int _room4;
    private readonly int _room5;
    private readonly int _room6;

    public TimeSpace21()
    {
        Definition = InstanceDefinitionBuilder.Named(21, "Tom has challenged you to a duel to test your strength! You can't back down. Prove your courage by facing him!", "The Duel")
            .ForLevels(31, 99)
            .StartingAt(10, 8)
            .Requiring(1012, 3)
            .WithDraw(98, 1)
            .WithDraw(111, 1)
            .WithDraw(124, 1)
            .WithSpecialReward(224, 1)
            .WithReward(2131, 1)
            .WithReward(1087, 1)
            .WithRoom(5100, out _room1, 3, 11)
            .WithRoom(5100, out _room2, 3, 11)
            .WithRoom(5100, out _room3, 3, 10)
            .WithRoom(5100, out _room4, 3, 9)
            .WithRoom(5100, out _room5, 3, 8)
            .WithRoom(5100, out _room6, 3, 7)
            .Build();
    }

    public override void OnFirstEnter(IInstanceRun r)
    {
        r.SpawnPortal(_room1, new(14, 1), _room2, new(14, 28), locked: false);
        r.SpawnPortal(_room2, new(14, 28), _room1, new(14, 1), locked: false);
        r.SpawnPortal(_room2, new(14, 1), _room3, new(14, 28), locked: false);
        r.SpawnPortal(_room3, new(14, 28), _room2, new(14, 1), locked: false);
        r.SpawnPortal(_room3, new(14, 1), _room4, new(14, 28));
        r.PlaceButton(_room3, 1, new(11, 12), 1000, 1045);
        r.Summon(_room3,
            new MonsterSpawn(20, new(9, 6)),
            new MonsterSpawn(20, new(9, 14)),
            new MonsterSpawn(20, new(9, 21)),
            new MonsterSpawn(140, new(12, 16)),
            new MonsterSpawn(140, new(15, 10)),
            new MonsterSpawn(140, new(15, 18)),
            new MonsterSpawn(140, new(16, 10)),
            new MonsterSpawn(20, new(20, 6), IsBonus: true),
            new MonsterSpawn(20, new(20, 13), IsBonus: true),
            new MonsterSpawn(20, new(20, 22)));
        r.SpawnPortal(_room4, new(14, 28), _room3, new(14, 1), locked: false);
        r.SpawnPortal(_room4, new(14, 1), _room5, new(14, 28));
        r.PlaceButton(_room4, 1, new(11, 14), 1000, 1045);
        r.Summon(_room4,
            new MonsterSpawn(20, new(6, 13)),
            new MonsterSpawn(20, new(7, 10)),
            new MonsterSpawn(141, new(9, 16)),
            new MonsterSpawn(20, new(10, 7)),
            new MonsterSpawn(141, new(10, 12)),
            new MonsterSpawn(20, new(13, 5)),
            new MonsterSpawn(20, new(17, 6)),
            new MonsterSpawn(141, new(18, 14)),
            new MonsterSpawn(20, new(20, 10), IsBonus: true),
            new MonsterSpawn(20, new(22, 14), IsBonus: true),
            new MonsterSpawn(141, new(22, 17), IsBonus: true));
        r.SpawnPortal(_room5, new(14, 28), _room4, new(14, 1), locked: false);
        r.SpawnPortal(_room5, new(14, 1), _room6, new(14, 28));
        r.PlaceButton(_room5, 1, new(14, 9), 1000, 1045);
        r.Summon(_room5,
            new MonsterSpawn(20, new(5, 17)),
            new MonsterSpawn(20, new(9, 11)),
            new MonsterSpawn(20, new(13, 6), IsBonus: true),
            new MonsterSpawn(20, new(16, 6), IsBonus: true),
            new MonsterSpawn(20, new(19, 11), IsBonus: true),
            new MonsterSpawn(20, new(24, 17)));
        r.SpawnPortal(_room6, new(14, 28), _room5, new(14, 1));
        r.SpawnExit(_room6, new(14, 1), locked: false);
        r.Summon(_room6,
            new MonsterSpawn(318, new(16, 24), IsBonus: true),
            new MonsterSpawn(4, new(10, 13)),
            new MonsterSpawn(4, new(14, 10)),
            new MonsterSpawn(4, new(18, 13)));
    }

    public override void OnRoomEntered(IInstanceRun r, int room)
    {
        if (room == _room1)
        {
            r.Message(Game18NConstString.MissionStartWhenEnterFirstRoom);
        }
        else if (room == _room2)
        {
            r.StartClock(TimeSpan.FromSeconds(200));
            r.Message(Game18NConstString.NewGoal);
            r.Dialog(_room2, 6111);
        }
        else if (room == _room6)
        {
            r.Message(Game18NConstString.NewGoal);
            r.Dialog(_room6, 6112);
        }
    }

    public override void OnRoomCleared(IInstanceRun r, int room)
    {
        if (room == _room6)
        {
            r.Message(Game18NConstString.NewGoal);
            r.Dialog(_room6, 6113);
        }
    }

    public override void OnButtonUsed(IInstanceRun r, int room, int button)
    {
        if (room == _room3 && button == 1)
        {
            r.Message(Game18NConstString.NewGoal);
            r.Message(Game18NConstString.ADoorHasOpened);
            r.UnlockPortal(_room3, _room4);
        }
        else if (room == _room4 && button == 1)
        {
            r.Message(Game18NConstString.NewGoal);
            r.Message(Game18NConstString.ADoorHasOpened);
            r.UnlockPortal(_room4, _room5);
        }
        else if (room == _room5 && button == 1)
        {
            r.Message(Game18NConstString.NewGoal);
            r.Message(Game18NConstString.ADoorHasOpened);
            r.UnlockPortal(_room5, _room6);
        }
    }

    public override void OnExitReached(IInstanceRun r, int room) => r.Succeed();

    public override void OnTimeout(IInstanceRun r) => r.Fail();
}

return new TimeSpace21();
