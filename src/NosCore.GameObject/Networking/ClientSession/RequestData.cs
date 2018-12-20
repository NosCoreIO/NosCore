using System;
using System.Collections.Generic;
using System.Text;

namespace NosCore.GameObject.Networking.ClientSession
{
    public class RequestData<T>
    {
        public ClientSession ClientSession { get; }
        public T Data { get; }
        public RequestData(ClientSession clientSession, T data)
        {
            Data = data;
            ClientSession = clientSession;
        }
    }
}