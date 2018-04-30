using NosCore.Data.StaticEntities;
using NosCore.PathFinder;
using System;
using System.IO;

namespace NosCore.GameObject.Map
{
    public class Map : MapDTO
    {
        public void Initialize()
        {
            LoadZone();
        }

        public short XLength { get; private set; }

        public short YLength { get; private set; }

        public byte[,] MapGrid { get; private set; }

        private void LoadZone()
        {
            using (Stream stream = new MemoryStream(Data))
            {
                const int numBytesToRead = 1;
                const int numBytesRead = 0;
                byte[] bytes = new byte[numBytesToRead];

                byte[] xlength = new byte[2];
                byte[] ylength = new byte[2];
                stream.Read(bytes, numBytesRead, numBytesToRead);
                xlength[0] = bytes[0];
                stream.Read(bytes, numBytesRead, numBytesToRead);
                xlength[1] = bytes[0];
                stream.Read(bytes, numBytesRead, numBytesToRead);
                ylength[0] = bytes[0];
                stream.Read(bytes, numBytesRead, numBytesToRead);
                ylength[1] = bytes[0];
                YLength = BitConverter.ToInt16(ylength, 0);
                XLength = BitConverter.ToInt16(xlength, 0);
                MapGrid = new byte[XLength, YLength];
                for (short i = 0; i < YLength; ++i)
                {
                    for (short t = 0; t < XLength; ++t)
                    {
                        stream.Read(bytes, numBytesRead, numBytesToRead);
                        MapGrid[t, i] = bytes[0];
                    }
                }
            }
        }
    }
}