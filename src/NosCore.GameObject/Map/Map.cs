using System;
using NosCore.Data.StaticEntities;

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
    }
}