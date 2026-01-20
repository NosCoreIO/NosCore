//  __  _  __    __   ___ __  ___ ___
// |  \| |/__\ /' _/ / _//__\| _ \ __|
// | | ' | \/ |`._`.| \_| \/ | v / _|
// |_|\__|\__/ |___/ \__/\__/|_|_\___|
//

using NodaTime;
using NosCore.Data.Dto;
using NosCore.Data.StaticEntities;
using NosCore.GameObject.ComponentEntities.Interfaces;
using NosCore.GameObject.Services.MapInstanceGenerationService;
using NosCore.GameObject.Services.ShopService;
using NosCore.GameObject.Services.SpeedCalculationService;
using NosCore.Shared.Enumerations;
using System;
using System.Collections.Concurrent;
using System.Threading;

namespace NosCore.GameObject.ComponentEntities.Entities
{
    public class MapMonster(ISpeedCalculationService speedCalculationService)
        : MapMonsterDto, INonPlayableEntity
    {
        public NpcMonsterDto NpcMonster { get; set; } = null!;

        public IDisposable? Life { get; set; }
        public ConcurrentDictionary<IAliveEntity, int> HitList => new();

        public bool IsSitting { get; set; }
        public byte Speed
        {
            get => speedCalculationService.CalculateSpeed(this);
            set { }
        }
        public byte Size { get; set; } = 10;
        public int Mp { get; set; }
        public int Hp { get; set; }
        public short Morph { get; set; }
        public byte MorphUpgrade { get; set; }
        public short MorphDesign { get; set; }
        public byte MorphBonus { get; set; }
        public bool NoAttack { get; set; }
        public bool NoMove { get; set; }
        public VisualType VisualType => VisualType.Monster;
        public SemaphoreSlim HitSemaphore { get; } = new SemaphoreSlim(1, 1);

        public long VisualId => MapMonsterId;

        public Guid MapInstanceId { get; set; }
        public short PositionX { get; set; }
        public short PositionY { get; set; }

        public short Effect { get; set; }
        public short EffectDelay { get; set; }
        public MapInstance MapInstance { get; set; } = null!;
        public Instant LastMove { get; set; }
        public bool IsAlive { get; set; }
        public int MaxHp => NpcMonster.MaxHp;

        public int MaxMp => NpcMonster.MaxMp;

        public short Race => NpcMonster.Race;
        public Shop? Shop { get; set; }

        public byte Level { get; set; }

        public byte HeroLevel { get; set; }
    }
}
