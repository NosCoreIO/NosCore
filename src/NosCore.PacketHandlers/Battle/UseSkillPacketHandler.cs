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

using NosCore.GameObject.Networking.ClientSession;
using System.Threading.Tasks;
using NosCore.Packets.ClientPackets.Battle;
using NosCore.Packets.Enumerations;
using NosCore.Data.Enumerations.I18N;
using NosCore.Packets.ServerPackets.Battle;
using NosCore.GameObject.ComponentEntities.Extensions;
using NosCore.GameObject.ComponentEntities.Interfaces;
using NosCore.GameObject.Infastructure;
using NosCore.GameObject.Services.BattleService;
using NosCore.Shared.Enumerations;
using NosCore.Shared.I18N;
using Serilog;
using NosCore.GameObject.Services.BroadcastService;

namespace NosCore.PacketHandlers.Battle
{
    public class UseSkillPacketHandler(ILogger logger, ILogLanguageLocalizer<LogLanguageKey> logLanguage, IBattleService battleService, ISessionRegistry sessionRegistry)
        : PacketHandler<UseSkillPacket>, IWorldPacketHandler
    {
        public override async Task ExecuteAsync(UseSkillPacket packet, ClientSession clientSession)
        {

            if (clientSession.Character.CanFight)
            {
                if (clientSession.Character.IsSitting)
                {
                    await clientSession.Character.RestAsync();
                }
                if (clientSession.Character.IsVehicled)
                {
                    await clientSession.SendPacketAsync(new CancelPacket()
                    {
                        Type = CancelPacketType.CancelAutoAttack
                    });
                    return;
                }

                IAliveEntity? requestableEntity;
                switch (packet.TargetVisualType)
                {
                    case VisualType.Player:
                        requestableEntity = sessionRegistry.GetCharacter(s => s.VisualId == packet.TargetId);
                        break;
                    case VisualType.Npc:
                        requestableEntity =
                            clientSession.Character.MapInstance.Npcs.Find(s => s.VisualId == packet.TargetId);
                        break;
                    case VisualType.Monster:
                        requestableEntity =
                            clientSession.Character.MapInstance.Monsters.Find(s => s.VisualId == packet.TargetId);
                        break;
                    default:
                        logger.Error(logLanguage[LogLanguageKey.VISUALTYPE_UNKNOWN],
                            packet.TargetVisualType);
                        return;
                }

                if (requestableEntity == null)
                {
                    logger.Error(logLanguage[LogLanguageKey.VISUALENTITY_DOES_NOT_EXIST]);
                    return;
                }

                await battleService.Hit(clientSession.Character, requestableEntity, new HitArguments()
                {
                    SkillId = packet.CastId,
                    MapX = packet.MapX,
                    MapY = packet.MapY,
                });
            }
            else
            {
                await clientSession.SendPacketAsync(new CancelPacket()
                {
                    Type = CancelPacketType.CancelAutoAttack
                });
            }
        }
    }
}