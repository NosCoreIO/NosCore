//  __  _  __    __   ___ __  ___ ___  
// |  \| |/__\ /' _/ / _//__\| _ \ __| 
// | | ' | \/ |`._`.| \_| \/ | v / _|  
// |_|\__|\__/ |___/ \__/\__/|_|_\___| 
// 
// Copyright (C) 2018 - NosCore
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

using System;
using JetBrains.Annotations;
using NosCore.Configuration;
using NosCore.GameObject.ComponentEntities.Extensions;
using NosCore.GameObject.ComponentEntities.Interfaces;
using NosCore.GameObject.Networking;
using NosCore.GameObject.Networking.ClientSession;
using NosCore.GameObject.Services.NRunAccess;
using NosCore.Packets.ClientPackets;
using NosCore.Shared.Enumerations;
using NosCore.Shared.I18N;
using Serilog;
using MapNpc = NosCore.GameObject.MapNpc;

namespace NosCore.Controllers
{
    public class NpcPacketController : PacketController
    {
        private readonly ILogger _logger = Logger.GetLoggerConfiguration().CreateLogger();
        private readonly WorldConfiguration _worldConfiguration;
        private readonly NrunAccessService _nRunAccessService;

        [UsedImplicitly]
        public NpcPacketController()
        {
        }

        public NpcPacketController(WorldConfiguration worldConfiguration, NrunAccessService nRunAccessService)
        {
            _worldConfiguration = worldConfiguration;
            _nRunAccessService = nRunAccessService;
        }

        /// <summary>
        /// npc_req packet
        /// </summary>
        /// <param name="requestNpcPacket"></param>
        public void ShowShop(RequestNpcPacket requestNpcPacket)
        {
            IRequestableEntity requestableEntity;
            switch (requestNpcPacket.Type)
            {
                case VisualType.Player:
                    requestableEntity = Broadcaster.Instance.GetCharacter(s => s.VisualId == requestNpcPacket.TargetId);
                    break;
                case VisualType.Npc:
                    requestableEntity = Session.Character.MapInstance.Npcs.Find(s => s.VisualId == requestNpcPacket.TargetId);
                    break;

                default:
                    _logger.Error(LogLanguage.Instance.GetMessageFromKey(LanguageKey.VISUALTYPE_UNKNOWN), requestNpcPacket.Type);
                    return;
            }
            if (requestableEntity == null)
            {
                _logger.Error(LogLanguage.Instance.GetMessageFromKey(LanguageKey.VISUALENTITY_DOES_NOT_EXIST));
                return;
            }

            requestableEntity.Requests.OnNext(new RequestData(Session));
        }

        /// <summary>
        /// nRunPacket packet
        /// </summary>
        /// <param name="nRunPacket"></param>
        public void NRun(NrunPacket nRunPacket)
        {
            MapNpc requestableEntity = Session.Character.MapInstance.Npcs.Find(s => s.VisualId == nRunPacket.NpcId);

            if (requestableEntity == null)
            {
                _logger.Error(LogLanguage.Instance.GetMessageFromKey(LanguageKey.VISUALENTITY_DOES_NOT_EXIST));
                return;
            }
            _nRunAccessService.NRunLaunch(Session, new Tuple<MapNpc, NrunPacket>(requestableEntity, nRunPacket));
        }

        /// <summary>
        /// shopping packet
        /// </summary>
        /// <param name="shoppingPacket"></param>
        public void Shopping(ShoppingPacket shoppingPacket)
        {
            if (Session.Character.InExchangeOrTrade)
            {
                //TODO log
                return;
            }

            IAliveEntity aliveEntity;
            switch (shoppingPacket.Type)
            {
                case VisualType.Player:
                    aliveEntity = Broadcaster.Instance.GetCharacter(s => s.VisualId == shoppingPacket.TargetId);
                    break;
                case VisualType.Npc:
                    aliveEntity = Session.Character.MapInstance.Npcs.Find(s => s.VisualId == shoppingPacket.TargetId);
                    break;

                default:
                    _logger.Error(LogLanguage.Instance.GetMessageFromKey(LanguageKey.VISUALTYPE_UNKNOWN), shoppingPacket.Type);
                    return;
            }
            if (aliveEntity == null)
            {
                _logger.Error(LogLanguage.Instance.GetMessageFromKey(LanguageKey.VISUALENTITY_DOES_NOT_EXIST));
                return;
            }
            byte shopKind = 100;
            var percent = 1.0;
            switch (Session.Character.GetDignityIco())
            {
                case 3:
                    percent = 1.1;
                    shopKind = 110;
                    break;

                case 4:
                    percent = 1.2;
                    shopKind = 120;
                    break;

                case 5:
                    percent = 1.5;
                    shopKind = 150;
                    break;

                case 6:
                    percent = 1.5;
                    shopKind = 150;
                    break;

                default:
                    break;
            }
            Session.SendPacket(aliveEntity.GenerateNInv(percent, shoppingPacket.ShopType, shopKind));
        }
    }
}