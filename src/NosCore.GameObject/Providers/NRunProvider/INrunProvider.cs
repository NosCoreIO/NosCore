using System;
using NosCore.GameObject.ComponentEntities.Interfaces;
using NosCore.GameObject.Networking.ClientSession;
using NosCore.Packets.ClientPackets;

namespace NosCore.GameObject.Providers.NRunProvider
{
    public interface INrunAccessService
    {
        void NRunLaunch(ClientSession clientSession, Tuple<IAliveEntity, NrunPacket> data);
    }
}