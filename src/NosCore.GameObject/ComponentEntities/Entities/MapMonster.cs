//  __  _  __    __   ___ __  ___ ___
// |  \| |/__\ /' _/ / _//__\| _ \ __|
// | | ' | \/ |`._`.| \_| \/ | v / _|
// |_|\__|\__/ |___/ \__/\__/|_|_\___|
// 
// Copyright (C) 2019 - NosCore
// 
// NosCore is a free software: you can redistribute it and/or modify
// it under the terms of the GNU General Public License as published by
// the Free Software Foundation, either version 3 of the License, or any later version.
// 
// This program is distributed in the hope that it will be useful,
// but WITHOUT ANY WARRANTY; without even the implied warranty of
// MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
// GNU General Public License for more details.
// 
// You should have received a copy of the GNU General Public License
// along with this program.  If not, see <http://www.gnu.org/licenses/>.

using NosCore.Data.Dto;
using NosCore.Data.StaticEntities;
using NosCore.GameObject.ComponentEntities.Extensions;
using NosCore.GameObject.ComponentEntities.Interfaces;
using NosCore.GameObject.Services.MapInstanceGenerationService;
using NosCore.GameObject.Services.ShopService;
using NosCore.PathFinder.Interfaces;
using NosCore.Shared.Enumerations;
using Serilog;
using System;
using System.Reactive.Linq;
using System.Threading.Tasks;
using NodaTime;
using NosCore.GameObject.Services.SpeedCalculationService;
using System.Threading;
using System.Collections.Concurrent;

namespace NosCore.GameObject.ComponentEntities.Entities
{
    public class MapMonster(ILogger logger, IHeuristic distanceCalculator, IClock clock,
            ISpeedCalculationService speedCalculationService)
        : MapMonsterDto, INonPlayableEntity
    {
        public NpcMonsterDto NpcMonster { get; private set; } = null!;

        public IDisposable? Life { get; private set; }
        public ConcurrentDictionary<IAliveEntity, int> HitList => new();

        public void Initialize(NpcMonsterDto npcMonster)
        {
            NpcMonster = npcMonster;
            Mp = NpcMonster?.MaxMp ?? 0;
            Hp = NpcMonster?.MaxHp ?? 0;
            PositionX = MapX;
            PositionY = MapY;
            IsAlive = true;
            Level = NpcMonster?.Level ?? 0;
        }

        public bool IsSitting { get; set; }
        public byte Speed => speedCalculationService.CalculateSpeed(this);
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
        public Shop? Shop => null;

        public byte Level { get; set; }

        public byte HeroLevel { get; set; }

        internal void StopLife()
        {
            Life?.Dispose();
            Life = null;
        }

        public Task StartLifeAsync()
        {
            async Task LifeAsync()
            {
                try
                {
                    if (!MapInstance.IsSleeping)
                    {
                        await MonsterLifeAsync().ConfigureAwait(false);
                    }
                }
                catch (Exception e)
                {
                    logger.Error(e.Message, e);
                }

            }
            Life = Observable.Interval(TimeSpan.FromMilliseconds(400)).Select(_ => LifeAsync()).Subscribe();
            return Task.CompletedTask;
        }

        private Task MonsterLifeAsync()
        {
            return this.MoveAsync(distanceCalculator, clock);
        }
    }
}