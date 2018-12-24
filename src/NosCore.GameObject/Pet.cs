using NosCore.Data.AliveEntities;
using NosCore.Data.StaticEntities;
using NosCore.GameObject.ComponentEntities.Interfaces;
using NosCore.GameObject.Networking.ClientSession;
using NosCore.GameObject.Services.MapInstanceAccess;
using NosCore.Shared.Enumerations;
using NosCore.Shared.I18N;
using Serilog;
using System;
using System.Reactive.Subjects;

namespace NosCore.GameObject
{
    public class Pet : MapMonsterDto, INamedEntity //TODO replace MapMonsterDTO by the correct PetDTO
    {
        public IDisposable Life { get; private set; }
        public bool IsSitting { get; set; }
        public byte Speed { get; set; }
        public int Mp { get; set; }
        public int Hp { get; set; }
        public short Morph { get; set; }
        public byte MorphUpgrade { get; set; }
        public short MorphDesign { get; set; }
        public byte MorphBonus { get; set; }
        public bool NoAttack { get; set; }
        public bool NoMove { get; set; }
        public string Name { get; set; }
        public VisualType VisualType => VisualType.Monster;
        public long VisualId => 0;// PetId;

        public Guid MapInstanceId { get; set; }
        public short PositionX { get; set; }
        public short PositionY { get; set; }

        public short Effect { get; set; }
        public short EffectDelay { get; set; }
        public NpcMonsterDto NpcMonster { get; set; }
        public DateTime LastMove { get; set; }
        public bool IsAlive { get; set; }

        public int MaxHp => NpcMonster.MaxHp;

        public int MaxMp => NpcMonster.MaxMp;

        public byte Level { get; set; }

        public byte HeroLevel { get; set; }
        public Group Group { get; set; }
        public long LevelXp { get; set; }

        public MapInstance MapInstance { get; set; }

        public short Race => NpcMonster.Race;

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
