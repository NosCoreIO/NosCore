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

using System;
using System.Threading.Tasks;
using NosCore.Packets.Enumerations;
using JetBrains.Annotations;
using NosCore.Core;
using NosCore.Core.I18N;
using NosCore.Data.Enumerations.I18N;
using NosCore.GameObject.ComponentEntities.Extensions;
using NosCore.GameObject.Networking;
using Serilog;

namespace NosCore.GameObject.Event
{
    [UsedImplicitly]
    public class RemoveTimeoutStaticBonuses : IGlobalEvent
    {
        private readonly ILogger _logger = Logger.GetLoggerConfiguration().CreateLogger();

        public TimeSpan Delay { get; set; } = TimeSpan.FromMinutes(5);

        public void Execution()
        {
            try
            {
                Parallel.ForEach(Broadcaster.Instance.GetCharacters(), session =>
                {
                    if (session.StaticBonusList.RemoveAll(s => s.DateEnd!= null && s.DateEnd < SystemTime.Now()) > 0)
                    {
                        session.SendPacketAsync(session.GenerateSay(
                            GameLanguage.Instance.GetMessageFromKey(LanguageKey.ITEM_TIMEOUT, session.AccountLanguage),
                            SayColorType.Yellow));
                    }
                });
            }
            catch (Exception e)
            {
                _logger.Error(e.Message, e);
            }
        }
    }
}