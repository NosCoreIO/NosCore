//  __  _  __    __   ___ __  ___ ___
// |  \| |/__\ /' _/ / _//__\| _ \ __|
// | | ' | \/ |`._`.| \_| \/ | v / _|
// |_|\__|\__/ |___/ \__/\__/|_|_\___|
// 
// Copyright (C) 2019 - NosCore
// 
// NosCore is a free software: you can redistribute it and/or modify
// it under the terms of the GNU General Public License as published by
// the Free Software Foundation, either version 3 of the License, or any later version.
// 
// This program is distributed in the hope that it will be useful,
// but WITHOUT ANY WARRANTY; without even the implied warranty of
// MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
// GNU General Public License for more details.
// 
// You should have received a copy of the GNU General Public License
// along with this program.  If not, see <http://www.gnu.org/licenses/>.

using NosCore.Data.Enumerations.I18N;
using NosCore.GameObject;
using NosCore.GameObject.Networking;
using NosCore.GameObject.Services.NRunService;
using NosCore.GameObject.Ecs;
using NosCore.Packets.ClientPackets.Npcs;
using NosCore.Shared.Enumerations;
using NosCore.Shared.I18N;
using Serilog;
using System.Threading.Tasks;
using NosCore.GameObject.Services.BroadcastService;

namespace NosCore.PacketHandlers.Shops
{
    public class RequestNpcPacketHandler(ILogger logger, ILogLanguageLocalizer<LogLanguageKey> logLanguage,
            ISessionRegistry sessionRegistry)
        : PacketHandler<RequestNpcPacket>, IWorldPacketHandler
    {
        public override Task ExecuteAsync(RequestNpcPacket requestNpcPacket, ClientSession clientSession)
        {
            switch (requestNpcPacket.Type)
            {
                case VisualType.Player:
                    var character = sessionRegistry.GetPlayer(s => s.VisualId == requestNpcPacket.TargetId);
                    if (character is not { } player)
                    {
                        logger.Error(logLanguage[LogLanguageKey.VISUALENTITY_DOES_NOT_EXIST]);
                        return Task.CompletedTask;
                    }
                    player.Requests[typeof(INrunEventHandler)].OnNext(new RequestData(clientSession));
                    return Task.CompletedTask;

                case VisualType.Npc:
                    var npcEntity = clientSession.Player.MapInstance.GetNpc((int)requestNpcPacket.TargetId);
                    if (npcEntity == null)
                    {
                        logger.Error(logLanguage[LogLanguageKey.VISUALENTITY_DOES_NOT_EXIST]);
                        return Task.CompletedTask;
                    }
                    var world = clientSession.Player.MapInstance.EcsWorld;
                    var dialog = npcEntity.Value.GetDialog(world);
                    return clientSession.SendPacketAsync(new RequestNpcPacket
                    {
                        Type = npcEntity.Value.GetVisualType(world),
                        TargetId = npcEntity.Value.GetVisualId(world),
                        Data = dialog ?? 0
                    });

                default:
                    logger.Error(logLanguage[LogLanguageKey.VISUALTYPE_UNKNOWN],
                        requestNpcPacket.Type);
                    return Task.CompletedTask;
            }
        }
    }
}