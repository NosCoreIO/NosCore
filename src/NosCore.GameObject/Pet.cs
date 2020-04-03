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

using System;
using NosCore.Packets.Enumerations;
using NosCore.Data.Dto;
using NosCore.Data.StaticEntities;
using NosCore.GameObject.ComponentEntities.Interfaces;
using NosCore.GameObject.Providers.MapInstanceProvider;

namespace NosCore.GameObject
{
    public class Pet : MapMonsterDto, INamedEntity //TODO replace MapMonsterDTO by the correct PetDTO
    {
        public IDisposable? Life { get; private set; }
        public NpcMonsterDto NpcMonster { get; private set; } = null!;
        public short Effect { get; set; }
        public short EffectDelay { get; set; }
        public DateTime LastMove { get; set; }
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