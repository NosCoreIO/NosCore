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
using System.Linq;
using System.Reactive.Linq;
using NosCore.Configuration;
using NosCore.Core;
using NosCore.Core.Networking;
using NosCore.Shared.I18N;
using Serilog;

namespace NosCore.MasterServer
{
    public class MasterServer
    {
        private static readonly ILogger _logger = Logger.GetLoggerConfiguration().CreateLogger();
        private readonly MasterConfiguration _masterConfiguration;

        public MasterServer(MasterConfiguration masterConfiguration)
        {
            _masterConfiguration = masterConfiguration;
        }

        public void Run()
        {
            if (_masterConfiguration == null)
            {
                return;
            }
            if (!System.Diagnostics.Debugger.IsAttached)
            {
                Observable.Interval(TimeSpan.FromSeconds(2)).Subscribe(_ => MasterClientListSingleton.Instance.Channels.Where(s =>
                              s.LastPing.AddSeconds(10) < SystemTime.Now() && s.WebApi != null).Select(s => s.Id).ToList().ForEach(_id =>
                              {
                                  _logger.Warning(LogLanguage.Instance.GetMessageFromKey(LogLanguageKey.CONNECTION_LOST), _id.ToString());
                                  MasterClientListSingleton.Instance.Channels.RemoveAll(s => s.Id == _id);
                              }));
            }

            _logger.Information(LogLanguage.Instance.GetMessageFromKey(LogLanguageKey.SUCCESSFULLY_LOADED));
            try
            {
                Console.Title += $" - WebApi : {_masterConfiguration.WebApi}";
            }
            catch
            {
                Console.ReadKey();
            }
        }
    }
}