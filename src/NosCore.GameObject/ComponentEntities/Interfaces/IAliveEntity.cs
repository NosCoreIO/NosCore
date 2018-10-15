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
namespace NosCore.GameObject.ComponentEntities.Interfaces
{
    public interface IAliveEntity : IVisualEntity
    {
        bool IsSitting { get; set; }

        byte Class { get; set; }

        byte Speed { get; set; }

        int Mp { get; set; }

        int Hp { get; set; }

        byte Morph { get; set; }

        byte MorphUpgrade { get; set; }

        byte MorphDesign { get; set; }

        byte MorphBonus { get; set; }

        bool NoAttack { get; set; }

        bool NoMove { get; set; }

        bool IsAlive { get; set; }

        short MapX { get; set; }

        short MapY { get; set; }

        int MaxHp { get; }

        int MaxMp { get; }

        byte Level { get; set; }

        byte HeroLevel { get; set; }
    }
}