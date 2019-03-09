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
using System.IO;
using System.Threading;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using NosCore.Configuration;
using NosCore.Database;
using NosCore.DAL;
using NosCore.GameObject.Map;
using NosCore.Shared.I18N;
using OpenTK.Graphics;
using Serilog;
using NosCore.Data.StaticEntities;

namespace NosCore.PathFinder.Gui
{
    public static class PathFinderGui
    {
        private const string ConfigurationPath = "../../../configuration";
        private const string Title = "NosCore - Pathfinder GUI";
        private const string ConsoleText = "PATHFINDER GUI - NosCoreIO";
        private static readonly PathfinderGuiConfiguration DatabaseConfiguration = new PathfinderGuiConfiguration();
        private static GuiWindow _guiWindow;
        private static readonly ILogger Logger = Shared.I18N.Logger.GetLoggerConfiguration().CreateLogger();

        private static void InitializeConfiguration()
        {
            var builder = new ConfigurationBuilder();
            builder.SetBasePath(Directory.GetCurrentDirectory() + ConfigurationPath);
            builder.AddJsonFile("pathfinder.json", false);
            builder.Build().Bind(DatabaseConfiguration);
        }

        public static void Main()
        {
            Console.Title = Title;
            Shared.I18N.Logger.PrintHeader(ConsoleText);
            InitializeConfiguration();
            LogLanguage.Language = DatabaseConfiguration.Language;
            try
            {
                var optionsBuilder = new DbContextOptionsBuilder<NosCoreContext>();
                optionsBuilder.UseNpgsql(DatabaseConfiguration.Database.ConnectionString);
                DataAccessHelper.Instance.Initialize(optionsBuilder.Options);

                while (true)
                {
                    Logger.Information(LogLanguage.Instance.GetMessageFromKey(LogLanguageKey.SELECT_MAPID));
                    var input = Console.ReadLine();
                    if (input == null || !int.TryParse(input, out var askMapId))
                    {
                        Logger.Error(LogLanguage.Instance.GetMessageFromKey(LogLanguageKey.WRONG_SELECTED_MAPID));
                        continue;
                    }

                    var map = (Map) DaoFactory.GetGenericDao<MapDto>().FirstOrDefault(m => m.MapId == askMapId);

                    if (map?.XLength > 0 && map.YLength > 0)
                    {
                        if (_guiWindow?.Exists ?? false)
                        {
                            _guiWindow.Exit();
                        }

                        new Thread(() =>
                        {
                            _guiWindow = new GuiWindow(map, 4, map.XLength, map.YLength, GraphicsMode.Default,
                                $"NosCore Pathfinder GUI - Map {map.MapId}");
                            _guiWindow.Run(30);
                        }).Start();
                    }
                }
            }
            catch
            {
                Console.ReadKey();
            }
        }
    }
}