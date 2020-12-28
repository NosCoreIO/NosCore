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

using JetBrains.Annotations;
using NosCore.Core;
using NosCore.Core.I18N;
using NosCore.Data.Enumerations.I18N;
using NosCore.GameObject.ComponentEntities.Extensions;
using NosCore.GameObject.Networking;
using NosCore.GameObject.Networking.ClientSession;
using NosCore.Packets.Enumerations;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace NosCore.GameObject.Services.EventLoaderService.Handlers
{
    [UsedImplicitly]
    public class RemoveTimeoutStaticBonuses : ITimedEventHandler
    {
        private DateTime _lastRun = SystemTime.Now();
        public bool Condition(Clock condition) => condition.LastTick - _lastRun >= TimeSpan.FromMinutes(5);

        public Task ExecuteAsync(RequestData<DateTime> runTime)
        {
            return Task.WhenAll(Broadcaster.Instance.GetCharacters().Select(session =>
            {
                if (session.StaticBonusList.RemoveAll(s => s.DateEnd != null && s.DateEnd < SystemTime.Now()) > 0)
                {
                    return session.SendPacketAsync(session.GenerateSay(
                        GameLanguage.Instance.GetMessageFromKey(LanguageKey.ITEM_TIMEOUT, session.AccountLanguage),
                        SayColorType.Yellow));
                }
                _lastRun = runTime.Data;
                return Task.CompletedTask;
            }));
        }
    }
}