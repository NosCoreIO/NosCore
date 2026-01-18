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
using NosCore.GameObject.Ecs;
using NosCore.GameObject.Ecs.Systems;
using NosCore.GameObject.Networking;
using NosCore.GameObject.Services.BattleService;
using NosCore.GameObject.Services.BroadcastService;
using NosCore.Packets.ClientPackets.Battle;
using NosCore.Packets.Enumerations;
using NosCore.Packets.ServerPackets.Battle;
using NosCore.Shared.Enumerations;
using NosCore.Shared.I18N;
using Serilog;
using System.Threading.Tasks;

namespace NosCore.PacketHandlers.Battle
{
    public class UseSkillPacketHandler(ILogger logger, ILogLanguageLocalizer<LogLanguageKey> logLanguage, IBattleService battleService, ISessionRegistry sessionRegistry, IRestSystem restSystem)
        : PacketHandler<UseSkillPacket>, IWorldPacketHandler
    {
        public override async Task ExecuteAsync(UseSkillPacket packet, ClientSession clientSession)
        {
            var player = clientSession.Player;

            if (player.CanFight)
            {
                if (player.IsSitting)
                {
                    await restSystem.ToggleRestAsync(player.World.World, player.Entity, player.MapInstance);
                }
                if (player.IsVehicled)
                {
                    await clientSession.SendPacketAsync(new CancelPacket()
                    {
                        Type = CancelPacketType.CancelAutoAttack
                    });
                    return;
                }

                Entity? targetEntity;
                switch (packet.TargetVisualType)
                {
                    case VisualType.Player:
                        var targetPlayer = sessionRegistry.GetPlayer(s => s.VisualId == packet.TargetId);
                        if (targetPlayer == null)
                        {
                            logger.Error(logLanguage[LogLanguageKey.VISUALENTITY_DOES_NOT_EXIST]);
                            return;
                        }
                        targetEntity = targetPlayer.Value.Entity;
                        break;
                    case VisualType.Npc:
                        targetEntity = player.MapInstance.GetNpc((int)packet.TargetId);
                        break;
                    case VisualType.Monster:
                        targetEntity = player.MapInstance.GetMonster((int)packet.TargetId);
                        break;
                    default:
                        logger.Error(logLanguage[LogLanguageKey.VISUALTYPE_UNKNOWN],
                            packet.TargetVisualType);
                        return;
                }

                if (targetEntity == null)
                {
                    logger.Error(logLanguage[LogLanguageKey.VISUALENTITY_DOES_NOT_EXIST]);
                    return;
                }

                await battleService.Hit(player, targetEntity.Value, new HitArguments()
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