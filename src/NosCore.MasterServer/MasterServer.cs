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
using System.Diagnostics;
using System.Linq;
using System.Reactive.Linq;
using System.Runtime.InteropServices;
using NosCore.Core;
using NosCore.Core.I18N;
using NosCore.Core.Networking;
using NosCore.Data.Enumerations.I18N;
using Serilog;

namespace NosCore.MasterServer
{
    public class MasterServer
    {
        private readonly ILogger _logger;
        private readonly MasterConfiguration _masterConfiguration;

        public MasterServer(MasterConfiguration masterConfiguration, ILogger logger)
        {
            _masterConfiguration = masterConfiguration;
            _logger = logger;
        }

        public void Run()
        {
            if (_masterConfiguration == null)
            {
                return;
            }

            if (!Debugger.IsAttached)
            {
                Observable.Interval(TimeSpan.FromSeconds(2)).Subscribe(_ => MasterClientListSingleton.Instance.Channels
                    .Where(s =>
                        (s.LastPing.AddSeconds(10) < SystemTime.Now()) && (s.WebApi != null)).Select(s => s.Id).ToList()
                    .ForEach(id =>
                    {
                        _logger.Warning(LogLanguage.Instance.GetMessageFromKey(LogLanguageKey.CONNECTION_LOST),
                            id.ToString());
                        MasterClientListSingleton.Instance.Channels.RemoveAll(s => s.Id == id);
                    }));
            }

            _logger.Information(LogLanguage.Instance.GetMessageFromKey(LogLanguageKey.SUCCESSFULLY_LOADED));
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                Console.Title += $@" - WebApi : {_masterConfiguration.WebApi}";
            }
        }
    }
}