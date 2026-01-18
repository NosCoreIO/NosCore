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

using Arch.Core;
using NosCore.Data.Enumerations.I18N;
using NosCore.GameObject;
using NosCore.GameObject.Services.BroadcastService;
using NosCore.GameObject.Ecs.Systems;
using NosCore.Packets.ClientPackets.Battle;
using NosCore.Shared.Enumerations;
using NosCore.Shared.I18N;
using Serilog;
using System.Threading.Tasks;
using NosCore.GameObject.Networking;

namespace NosCore.PacketHandlers.Game
{
    public class NcifPacketHandler(ILogger logger, ILogLanguageLocalizer<LogLanguageKey> logLanguage,
            ISessionRegistry sessionRegistry, IStatsSystem statsSystem)
        : PacketHandler<NcifPacket>, IWorldPacketHandler
    {
        public override async Task ExecuteAsync(NcifPacket ncifPacket, ClientSession session)
        {
            var world = session.Player.MapInstance.EcsWorld.World;

            switch (ncifPacket.Type)
            {
                case VisualType.Player:
                    var character = sessionRegistry.GetPlayer(s => s.VisualId == ncifPacket.TargetId);
                    if (character.HasValue)
                    {
                        await session.SendPacketAsync(statsSystem.GenerateStatPacket(character.Value)).ConfigureAwait(false);
                    }
                    break;
                case VisualType.Monster:
                    var monster = session.Player.MapInstance.GetMonster((int)ncifPacket.TargetId);
                    if (monster != null)
                    {
                        await session.SendPacketAsync(statsSystem.GenerateStatPacket(world, monster.Value)).ConfigureAwait(false);
                    }
                    break;
                case VisualType.Npc:
                    var npc = session.Player.MapInstance.GetNpc((int)ncifPacket.TargetId);
                    if (npc != null)
                    {
                        await session.SendPacketAsync(statsSystem.GenerateStatPacket(world, npc.Value)).ConfigureAwait(false);
                    }
                    break;
                default:
                    logger.Error(logLanguage[LogLanguageKey.VISUALTYPE_UNKNOWN],
                        ncifPacket.Type);
                    return;
            }
        }
    }
}