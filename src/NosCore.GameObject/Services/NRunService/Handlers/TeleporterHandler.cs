//  __  _  __    __   ___ __  ___ ___
// |  \| |/__\ /' _/ / _//__\| _ \ __|
// | | ' | \/ |`._`.| \_| \/ | v / _|
// |_|\__|\__/ |___/ \__/\__/|_|_\___|
//

using NosCore.GameObject.ComponentEntities.Entities;
using NosCore.GameObject.ComponentEntities.Interfaces;
using NosCore.GameObject.Networking.ClientSession;
using NosCore.GameObject.Services.MapChangeService;
using NosCore.Packets.ClientPackets.Npcs;
using NosCore.Packets.Enumerations;
using NosCore.Packets.ServerPackets.Chats;
using NosCore.Shared.Enumerations;
using NosCore.Shared.Helpers;
using System;
using System.Threading.Tasks;

namespace NosCore.GameObject.Services.NRunService.Handlers
{
    public class TeleporterHandler(IMapChangeService mapChangeService) : INrunEventHandler
    {
        public bool Condition(Tuple<IAliveEntity, NrunPacket> item)
        {
            return (item.Item2.Runner == NrunRunnerType.Teleport)
                && item.Item1 is MapNpc mapNpc
                && (((mapNpc.Dialog >= 439) && (mapNpc.Dialog <= 441)) || (mapNpc.Dialog == 11) ||
                    (mapNpc.Dialog == 16) || (mapNpc.Dialog == 9768));
        }

        public Task ExecuteAsync(RequestData<Tuple<IAliveEntity, NrunPacket>> requestData)
        {
            return requestData.Data.Item2.Type switch
            {
                1 => RemoveGoldAndTeleportAsync(requestData.ClientSession, 20, 1000, 7, 11, 90, 94),
                2 => RemoveGoldAndTeleportAsync(requestData.ClientSession, 145, 2000, 11, 15, 108, 112),
                _ => RemoveGoldAndTeleportAsync(requestData.ClientSession, 1, 0, 77, 82, 113, 119),
            };
        }

        private async Task RemoveGoldAndTeleportAsync(ClientSession clientSession, short mapId, long goldToPay, short x1, short x2,
            short y1, short y2)
        {
            if (clientSession.Character.Gold >= goldToPay)
            {
                await clientSession.Character.RemoveGoldAsync(goldToPay);
                await mapChangeService.ChangeMapAsync(clientSession,
                    mapId, (short)RandomHelper.Instance.RandomNumber(x1, x2),
                    (short)RandomHelper.Instance.RandomNumber(y1, y2));
                return;
            }

            await clientSession.SendPacketAsync(new SayiPacket
            {
                VisualType = VisualType.Player,
                VisualId = clientSession.Character.CharacterId,
                Type = SayColorType.Yellow,
                Message = Game18NConstString.NotEnoughGold
            });
        }
    }
}
