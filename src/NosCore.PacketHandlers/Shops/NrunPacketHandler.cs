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

using NosCore.Core.I18N;
using NosCore.Data.Enumerations.I18N;
using NosCore.GameObject;
using NosCore.GameObject.ComponentEntities.Interfaces;
using NosCore.GameObject.Networking;
using NosCore.GameObject.Networking.ClientSession;
using NosCore.GameObject.Services.NRunService;
using NosCore.Packets.ClientPackets.Npcs;
using NosCore.Shared.Enumerations;
using Serilog;
using System;
using System.Threading.Tasks;

namespace NosCore.PacketHandlers.Shops
{
    public class NrunPacketHandler : PacketHandler<NrunPacket>, IWorldPacketHandler
    {
        private readonly ILogger _logger;
        private readonly INrunService _nRunRunnerService;

        public NrunPacketHandler(ILogger logger, INrunService nRunRunnerService)
        {
            _logger = logger;
            _nRunRunnerService = nRunRunnerService;
        }

        public override async Task ExecuteAsync(NrunPacket nRunPacket, ClientSession clientSession)
        {
            var forceNull = false;
            IAliveEntity? aliveEntity;
            switch (nRunPacket.VisualType)
            {
                case VisualType.Player:
                    aliveEntity = Broadcaster.Instance.GetCharacter(s => s.VisualId == nRunPacket.VisualId);
                    break;
                case VisualType.Npc:
                    aliveEntity = clientSession.Character.MapInstance.Npcs.Find(s => s.VisualId == nRunPacket.VisualId);
                    break;
                case null:
                    aliveEntity = null;
                    forceNull = true;
                    break;

                default:
                    _logger.Error(LogLanguage.Instance.GetMessageFromKey(LogLanguageKey.VISUALTYPE_UNKNOWN),
                        nRunPacket.Type);
                    return;
            }

            if ((aliveEntity == null) && !forceNull)
            {
                _logger.Error(LogLanguage.Instance.GetMessageFromKey(LogLanguageKey.VISUALENTITY_DOES_NOT_EXIST));
                return;
            }

            await _nRunRunnerService.NRunLaunchAsync(clientSession, new Tuple<IAliveEntity, NrunPacket>(aliveEntity!, nRunPacket)).ConfigureAwait(false);
        }
    }
}