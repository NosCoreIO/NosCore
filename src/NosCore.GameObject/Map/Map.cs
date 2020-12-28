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

using NosCore.Data.StaticEntities;
using NosCore.PathFinder.Interfaces;
using NosCore.Shared.Helpers;
using System;
using System.Collections.Generic;
using System.Linq;

namespace NosCore.GameObject.Map
{
    public class Map : MapDto, IMapGrid
    {
        private short _width;

        private short _height;

        public short Width
        {
            get
            {
                if (_width == 0)
                {
                    _width = BitConverter.ToInt16(Data.AsSpan().Slice(0, 2).ToArray(), 0);
                }

                return _width;
            }
        }

        public short Height
        {
            get
            {
                if (_height == 0)
                {
                    _height = BitConverter.ToInt16(Data.AsSpan().Slice(2, 2).ToArray(), 0);
                }

                return _height;
            }
        }

        public byte this[short x, short y] => Data.AsSpan().Slice(4 + y * Width + x, 1)[0];

        internal bool GetFreePosition(ref short firstX, ref short firstY, byte xpoint, byte ypoint)
        {
            var minX = (short)(-xpoint + firstX);
            var maxX = (short)(xpoint + firstX);

            var minY = (short)(-ypoint + firstY);
            var maxY = (short)(ypoint + firstY);

            var cells = new List<(short X, short Y)>();
            for (var y = minY; y <= maxY; y++)
            {
                for (var x = minX; x <= maxX; x++)
                {
                    if ((x != firstX) || (y != firstY))
                    {
                        cells.Add((x, y));
                    }
                }
            }

            foreach (var cell in cells.OrderBy(_ => RandomHelper.Instance.RandomNumber(0, int.MaxValue)))
            {
                if (IsBlockedZone(firstX, firstY, cell.X, cell.Y))
                {
                    continue;
                }

                firstX = cell.X;
                firstY = cell.Y;
                return true;
            }

            return false;
        }

        public bool IsBlockedZone(short firstX, short firstY, short mapX, short mapY)
        {
            var posX = (short)Math.Abs(mapX - firstX);
            var posY = (short)Math.Abs(mapY - firstY);

            var positiveX = mapX > firstX;
            var positiveY = mapY > firstY;

            for (var i = 0; i <= posX; i++)
            {
                if (!IsWalkable((short)((positiveX ? i : -i) + firstX), firstY))
                {
                    return true;
                }
            }

            for (var i = 0; i <= posY; i++)
            {
                if (!IsWalkable(firstX, (short)((positiveY ? i : -i) + firstY)))
                {
                    return true;
                }
            }

            return false;
        }

        public bool IsWalkable(short mapX, short mapY)
        {
            if ((mapX > Width) || (mapX < 0) || (mapY > Height) || (mapY < 0))
            {
                return false;
            }

            return IsWalkable(this[mapX, mapY]);
        }

        private static bool IsWalkable(byte value)
        {
            return (value == 0) || (value == 2) || ((value >= 16) && (value <= 19));
        }
    }
}