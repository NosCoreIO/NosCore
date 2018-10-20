//  __  _  __    __   ___ __  ___ ___  
// |  \| |/__\ /' _/ / _//__\| _ \ __| 
// | | ' | \/ |`._`.| \_| \/ | v / _|  
// |_|\__|\__/ |___/ \__/\__/|_|_\___| 
// 
// Copyright (C) 2018 - NosCore
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
    public class MapNpc : MapNpcDto, INonPlayableEntity
    {
        public IDisposable Life { get; private set; }
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
        public VisualType VisualType => VisualType.Npc;

        public long VisualId => MapNpcId;

        public Guid MapInstanceId { get; set; }
        public short PositionX { get; set; }
        public short PositionY { get; set; }
        public string Name { get; set; }
        public NpcMonsterDto NpcMonster { get; set; }
        public MapInstance MapInstance { get; set; }
        public DateTime LastMove { get; set; }
        public bool IsAlive { get; set; }

        public int MaxHp => NpcMonster.MaxHp;

        public int MaxMp => NpcMonster.MaxMp;

        public byte Level { get; set; }

        public byte HeroLevel { get; set; }

        internal void Initialize(NpcMonsterDto npcMonster)
        {
            NpcMonster = npcMonster;
            Mp = NpcMonster.MaxMp;
            Hp = NpcMonster.MaxHp;
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
            Life = Observable.Interval(TimeSpan.FromMilliseconds(400)).Subscribe(_ =>
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