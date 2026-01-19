//  __  _  __    __   ___ __  ___ ___
// |  \| |/__\ /' _/ / _//__\| _ \ __|
// | | ' | \/ |`._`.| \_| \/ | v / _|
// |_|\__|\__/ |___/ \__/\__/|_|_\___|
//

using NodaTime;
using NosCore.Data.Dto;
using NosCore.Data.StaticEntities;
using NosCore.GameObject.ComponentEntities.Interfaces;
using NosCore.GameObject.Services.GroupService;
using NosCore.GameObject.Services.MapInstanceGenerationService;
using NosCore.GameObject.Services.ShopService;
using NosCore.Shared.Enumerations;
using System;
using System.Collections.Concurrent;
using System.Threading;

namespace NosCore.GameObject.ComponentEntities.Entities
{
    public class Pet : MapMonsterDto, INamedEntity //TODO replace MapMonsterDTO by the correct PetDTO
    {
        public IDisposable? Life { get; private set; }
        public NpcMonsterDto NpcMonster { get; private set; } = null!;
        public short Effect { get; set; }
        public short EffectDelay { get; set; }
        public Instant LastMove { get; set; }
        public bool IsSitting { get; set; }
        public byte Speed { get; set; }
        public byte Size { get; set; }
        public int Mp { get; set; }
        public int Hp { get; set; }
        public short Morph { get; set; }
        public byte MorphUpgrade { get; set; }
        public short MorphDesign { get; set; }
        public byte MorphBonus { get; set; }
        public bool NoAttack { get; set; }
        public bool NoMove { get; set; }
        public string? Name { get; set; }
        public VisualType VisualType => VisualType.Monster;
        public long VisualId => 0; // PetId;
        public SemaphoreSlim HitSemaphore { get; } = new SemaphoreSlim(1, 1);

        public ConcurrentDictionary<IAliveEntity, int> HitList => new();

        public Guid MapInstanceId { get; set; }
        public short PositionX { get; set; }
        public short PositionY { get; set; }
        public bool IsAlive { get; set; }

        public int MaxHp => NpcMonster.MaxHp;

        public int MaxMp => NpcMonster.MaxMp;

        public byte Level { get; set; }

        public byte HeroLevel { get; set; }
        public Group? Group { get; set; }
        public long LevelXp { get; set; }

        public MapInstance MapInstance { get; set; } = null!;

        public short Race => NpcMonster.Race;

        public Shop? Shop => null;


        internal void Initialize(NpcMonsterDto npcMonster)
        {
            NpcMonster = npcMonster;
            Mp = NpcMonster.MaxMp;
            Hp = NpcMonster.MaxHp;
            Speed = NpcMonster.Speed;
            IsAlive = true;
        }
    }
}
