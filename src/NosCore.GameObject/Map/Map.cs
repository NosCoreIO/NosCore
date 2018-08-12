using System;
using System.Collections.Generic;
using System.Linq;
using NosCore.Data.StaticEntities;
using NosCore.GameObject.Networking;
using NosCore.PathFinder;

namespace NosCore.GameObject.Map
{
    public class Map : MapDTO
    {
        private short _xLength;

        private short _yLength;

        public short XLength
        {
            get
            {
                if (_xLength == 0)
                {
                    _xLength = BitConverter.ToInt16(Data.AsSpan().Slice(0, 2).ToArray(), 0);
                }

                return _xLength;
            }
        }

        public short YLength
        {
            get
            {
                if (_yLength == 0)
                {
                    _yLength = BitConverter.ToInt16(Data.AsSpan().Slice(2, 2).ToArray(), 0);
                }

                return _yLength;
            }
        }

        public byte this[short x, short y] => Data.AsSpan().Slice(4 + y * XLength + x, 1)[0];

        public void Initialize()
        {
        }
        internal bool GetFreePosition(ref short firstX, ref short firstY, byte xpoint, byte ypoint)
        {
            var minX = (short)(-xpoint + firstX);
            var maxX = (short)(xpoint + firstX);

            var minY = (short)(-ypoint + firstY);
            var maxY = (short)(ypoint + firstY);

            var cells = new List<MapCell>();
            for (var y = minY; y <= maxY; y++)
            {
                for (var x = minX; x <= maxX; x++)
                {
                    if (x != firstX || y != firstY)
                    {
                        cells.Add(new MapCell { X = x, Y = y });
                    }
                }
            }
            foreach (var cell in cells.OrderBy(mapCell => ServerManager.Instance.RandomNumber(0, int.MaxValue)))
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

        private bool IsBlockedZone(short firstX, short firstY, short mapX, short mapY)
        {
            for (var i = 1; i <= Math.Abs(mapX - firstX); i++)
            {
                if (!IsWalkable(this[(short)(firstX + Math.Sign(mapX - firstX) * i), firstY]))
                {
                    return true;
                }
            }

            for (var i = 1; i <= Math.Abs(mapY - firstY); i++)
            {
                if (!IsWalkable(this[firstX, (short)(firstY + Math.Sign(mapY - firstY) * i)]))
                {
                    return true;
                }
            }
            return false;
        }

        public bool IsWalkable(byte value)
        {
            return value == 0 || value == 2 || value >= 16 && value <= 19;
        }
    }
}