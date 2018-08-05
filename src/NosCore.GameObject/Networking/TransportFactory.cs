using System;
using System.Collections.Generic;
using System.Text;

namespace NosCore.GameObject.Networking
{
    public class TransportFactory
    {
        private static TransportFactory _instance;
        private long _lastTransportId = 100000;

        private TransportFactory()
        {
        }

        public static TransportFactory Instance => _instance ?? (_instance = new TransportFactory());

        public long GenerateTransportId()
        {
            _lastTransportId++;

            if (_lastTransportId >= long.MaxValue)
            {
                _lastTransportId = 0;
            }

            return _lastTransportId;
        }
    }
}
