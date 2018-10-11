using System;
using System.Reactive.Linq;
using NosCore.Data.AliveEntities;
using NosCore.Data.StaticEntities;
using NosCore.GameObject.ComponentEntities.Extensions;
using NosCore.GameObject.ComponentEntities.Interfaces;
using NosCore.GameObject.Services.MapInstanceAccess;
using NosCore.Shared.Enumerations;
using NosCore.Shared.I18N;

namespace NosCore.GameObject
{
    public class MapMonster : MapMonsterDTO, INonPlayableEntity
    {
        public bool IsSitting { get; set; }
        public byte Class { get; set; }
        public byte Speed { get; set; }
        public int Mp { get; set; }
        public int Hp { get; set; }
        public byte Morph { get; set; }
        public byte MorphUpgrade { get; set; }
        public byte MorphDesign { get; set; }
        public byte MorphBonus { get; set; }
        public bool NoAttack { get; set; }
        public bool NoMove { get; set; }

        public VisualType VisualType => VisualType.Monster;

        public long VisualId => MapMonsterId;

        public Guid MapInstanceId { get; set; }
        public short PositionX { get; set; }
        public short PositionY { get; set; }
        public short Effect { get; set; }
        public short EffectDelay { get; set; }
        public string Name { get; set; }
        public NpcMonsterDTO NpcMonster { get; set; }
        public MapInstance MapInstance { get; set; }
        public DateTime LastMove { get; set; }
        public bool IsAlive { get; set; }
        public IDisposable Life { get; private set; }

        internal void Initialize(NpcMonsterDTO npcMonster)
        {
            NpcMonster = npcMonster;
            Mp = NpcMonster.MaxMP;
            Hp = NpcMonster.MaxHP;
            PositionX = MapX;
            PositionY = MapY;
            Speed = NpcMonster.Speed;
            IsAlive = true;
        }

        internal void StopLife()
        {
            Life?.Dispose();
            Life = null;
        }

        public void StartLife()
        {
            Life = Observable.Interval(TimeSpan.FromMilliseconds(400)).Subscribe(x =>
            {
                try
                {
                    if (!MapInstance.IsSleeping)
                    {
                        MonsterLife();
                    }
                }
                catch (Exception e)
                {
                    Logger.Error(e);
                }
            });
        }

        private void MonsterLife()
        {
            this.Move();
        }
    }
}