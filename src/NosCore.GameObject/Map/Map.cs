using NosCore.Data.StaticEntities;
using System;
using System.IO;
using System.Linq;

namespace NosCore.GameObject.Map
{
    public class Map : MapDTO
    {
        public void Initialize()
        {

        }

        private short _xLength;
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

        private short _yLength;
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
    }
}