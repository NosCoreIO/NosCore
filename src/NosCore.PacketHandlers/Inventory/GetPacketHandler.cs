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
using NosCore.GameObject.ComponentEntities.Extensions;
using NosCore.GameObject.Networking.ClientSession;
using NosCore.GameObject.Services.MapItemGenerationService;
using NosCore.Packets.ClientPackets.Drops;
using NosCore.Packets.Enumerations;
using NosCore.PathFinder.Interfaces;
using NosCore.Shared.Enumerations;
using Serilog;
using System;
using System.Threading.Tasks;
using NodaTime;
using NosCore.Shared.I18N;
using NosCore.Packets.ServerPackets.Chats;

namespace NosCore.PacketHandlers.Inventory
{
    public class GetPacketHandler : PacketHandler<GetPacket>, IWorldPacketHandler
    {
        private readonly ILogger _logger;
        private readonly IHeuristic _distanceCalculator;
        private readonly IClock _clock;
        private readonly ILogLanguageLocalizer<LogLanguageKey> _logLanguage;

        public GetPacketHandler(ILogger logger, IHeuristic distanceCalculator, IClock clock, ILogLanguageLocalizer<LogLanguageKey> logLanguage)
        {
            _logger = logger;
            _distanceCalculator = distanceCalculator;
            _clock = clock;
            _logLanguage = logLanguage;
        }

        public override async Task ExecuteAsync(GetPacket getPacket, ClientSession clientSession)
        {
            if (!clientSession.Character.MapInstance.MapItems.ContainsKey(getPacket.VisualId))
            {
                return;
            }

            var mapItem = clientSession.Character.MapInstance.MapItems[getPacket.VisualId];

            bool canpick;
            switch (getPacket.PickerType)
            {
                case VisualType.Player:
                    canpick = _distanceCalculator.GetDistance((clientSession.Character.PositionX, clientSession.Character.PositionY),
                        (mapItem.PositionX, mapItem.PositionY)) < 8;
                    break;

                case VisualType.Npc:
                    return;

                default:
                    _logger.Error(_logLanguage[LogLanguageKey.UNKNOWN_PICKERTYPE]);
                    return;
            }

            if (!canpick)
            {
                return;
            }

            //TODO add group drops
            if ((mapItem.OwnerId != null) && (mapItem.DroppedAt.Plus(Duration.FromSeconds(30)) > _clock.GetCurrentInstant()) &&
                (mapItem.OwnerId != clientSession.Character.CharacterId))
            {
                await clientSession.SendPacketAsync(new SayiPacket
                {
                    VisualType = VisualType.Player,
                    VisualId = clientSession.Character.CharacterId,
                    Type = SayColorType.Yellow,
                    Message = Game18NConstString.UnableToPickUp
                }).ConfigureAwait(false);
                return;
            }

            mapItem.Requests[typeof(IGetMapItemEventHandler)].OnNext(new RequestData<Tuple<MapItem, GetPacket>>(clientSession,
                new Tuple<MapItem, GetPacket>(mapItem, getPacket)));

            await Task.WhenAll(mapItem.HandlerTasks).ConfigureAwait(false);
        }
    }
}