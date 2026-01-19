//  __  _  __    __   ___ __  ___ ___
// |  \| |/__\ /' _/ / _//__\| _ \ __|
// | | ' | \/ |`._`.| \_| \/ | v / _|
// |_|\__|\__/ |___/ \__/\__/|_|_\___|
//

using NosCore.GameObject.ComponentEntities.Extensions;
using NosCore.GameObject.Networking.ClientSession;
using NosCore.Networking;
using NosCore.Packets.ClientPackets.UI;
using NosCore.Packets.Enumerations;
using System;
using System.Threading.Tasks;


namespace NosCore.GameObject.Services.GuriRunnerService.Handlers
{
    public class EmoticonEventHandler : IGuriEventHandler
    {
        public bool Condition(GuriPacket packet)
        {
            return (packet.Type == GuriPacketType.TextInput) && (packet.Data >= 973) && (packet.Data <= 999);
        }

        public Task ExecuteAsync(RequestData<GuriPacket> requestData)
        {
            if (requestData.ClientSession.Character.EmoticonsBlocked)
            {
                return Task.CompletedTask;
            }

            if (requestData.Data.VisualId.GetValueOrDefault() == requestData.ClientSession.Character.CharacterId)
            {
                return requestData.ClientSession.Character.MapInstance.SendPacketAsync(
                    requestData.ClientSession.Character.GenerateEff(Convert.ToInt32(requestData.Data.Data) +
                        4099)); //TODO , ReceiverType.AllNoEmoBlocked
            }

            return Task.CompletedTask;
        }
    }
}
