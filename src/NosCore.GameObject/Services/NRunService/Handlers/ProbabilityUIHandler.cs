using NosCore.GameObject.ComponentEntities.Interfaces;
using NosCore.GameObject.Networking.ClientSession;
using NosCore.Packets.ClientPackets.Npcs;
using NosCore.Packets.Enumerations;
using NosCore.Packets.ServerPackets.UI;
using System;
using System.Threading.Tasks;

namespace NosCore.GameObject.Services.NRunService.Handlers
{
    /// <summary>
    /// Handler for ProbabilityUIs nrun type
    /// </summary>
    public class ProbabilityUIHandler : INrunEventHandler
    {
        /// <summary>
        /// Conditions to execute packet sending
        /// </summary>
        /// <param name="datas">Data of request</param>
        /// <returns>true if condition is respected else false</returns>
        public bool Condition(Tuple<IAliveEntity, NrunPacket> datas)
        {
            return datas.Item2.Runner == NrunRunnerType.ProbabilityUIs;
        }

        /// <summary>
        /// Method called when event will be handled
        /// </summary>
        /// <param name="requestData">Data of request</param>
        /// <returns>Task to execute</returns>
        public Task ExecuteAsync(RequestData<Tuple<IAliveEntity, NrunPacket>> requestData)
        {
            return requestData.ClientSession.SendPacketAsync(
                new WopenPacket
                {
                    Type = (WindowType)(requestData.Data.Item2.Type ?? 0)
                });
        }
    }
}
