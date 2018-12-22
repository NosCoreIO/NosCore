using System;
using System.Collections.Generic;
using System.Text;

namespace NosCore.GameObject.Networking.ClientSession
{
    public class RequestData<T> : RequestData
    {
        public T Data { get; }
        public RequestData(ClientSession clientSession, T data) : base(clientSession)
        {
            Data = data;
        }
    }

    public class RequestData
    {
        public ClientSession ClientSession { get; }
        public RequestData(ClientSession clientSession)
        {
            ClientSession = clientSession;
        }
    }
}