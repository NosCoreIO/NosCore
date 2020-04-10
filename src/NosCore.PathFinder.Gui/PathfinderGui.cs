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
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using Mapster;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Moq;
using NosCore.Configuration;
using NosCore.Core.I18N;
using NosCore.Dao;
using NosCore.Dao.Interfaces;
using NosCore.Data.Dto;
using NosCore.Data.Enumerations.I18N;
using NosCore.Data.StaticEntities;
using NosCore.Database;
using NosCore.Database.Entities;
using NosCore.GameObject.Providers.ItemProvider;
using OpenTK.Graphics;
using Serilog;

namespace NosCore.PathFinder.Gui
{
    public static class PathFinderGui
    {
        private const string ConfigurationPath = "../../../configuration";
        private const string Title = "NosCore - Pathfinder GUI";
        private const string ConsoleText = "PATHFINDER GUI - NosCoreIO";
        private static readonly PathfinderGuiConfiguration DatabaseConfiguration = new PathfinderGuiConfiguration();
        private static GuiWindow? _guiWindow;
        private static readonly ILogger Logger = Core.I18N.Logger.GetLoggerConfiguration().CreateLogger();
        private static readonly DataAccessHelper _dbContextBuilder = new DataAccessHelper();
        private static readonly IDao<MapDto, short> _mapDao =new Dao<Map, MapDto, short>(Logger, _dbContextBuilder);
        private static readonly IDao<NpcMonsterDto, short> _npcMonsterDao =new Dao<NpcMonster, NpcMonsterDto, short>(Logger, _dbContextBuilder);

        private static void InitializeConfiguration()
        {
            var builder = new ConfigurationBuilder();
            builder
                .SetBasePath(Directory.GetCurrentDirectory() + ConfigurationPath)
                .AddYamlFile("pathfinder.yml", false)
                .Build()
                .Bind(DatabaseConfiguration);
        }

        public static void Main()
        {
            try { Console.Title = Title; } catch (PlatformNotSupportedException) { }
            Core.I18N.Logger.PrintHeader(ConsoleText);
            InitializeConfiguration();
            TypeAdapterConfig.GlobalSettings.Default.IgnoreAttribute(typeof(I18NFromAttribute));
            LogLanguage.Language = DatabaseConfiguration.Language;
            try
            {
                var optionsBuilder = new DbContextOptionsBuilder<NosCoreContext>();
                optionsBuilder.UseNpgsql(DatabaseConfiguration.Database!.ConnectionString);
                _dbContextBuilder.Initialize(optionsBuilder.Options);

                var npcMonsters = _npcMonsterDao.LoadAll().ToList();
                TypeAdapterConfig<MapMonsterDto, GameObject.MapMonster>.NewConfig().ConstructUsing(src => new GameObject.MapMonster(npcMonsters, Logger));
                TypeAdapterConfig<MapNpcDto, GameObject.MapNpc>.NewConfig().ConstructUsing(src => new GameObject.MapNpc(new Mock<IItemProvider>().Object, new Mock<IDao<ShopDto, int>>().Object, new Mock<IDao<ShopItemDto, int>>().Object, npcMonsters, Logger, new List<NpcTalkDto>()));
                while (true)
                {
                    Logger.Information(LogLanguage.Instance.GetMessageFromKey(LogLanguageKey.SELECT_MAPID));
                    var input = Console.ReadLine();
                    if ((input == null) || !int.TryParse(input, out var askMapId))
                    {
                        Logger.Error(LogLanguage.Instance.GetMessageFromKey(LogLanguageKey.WRONG_SELECTED_MAPID));
                        continue;
                    }

                    var map = _mapDao.FirstOrDefaultAsync(m => m.MapId == askMapId)?.Adapt<GameObject.Map.Map>();

                    if ((!(map?.XLength > 0)) || (map.YLength <= 0))
                    {
                        continue;
                    }

                    if (_guiWindow?.Exists ?? false)
                    {
                        _guiWindow.Exit();
                    }

                    new Thread(() =>
                    {
                        _guiWindow = new GuiWindow(map, 4, map.XLength, map.YLength, GraphicsMode.Default,
                            $"NosCore Pathfinder GUI - Map {map.MapId}", _dbContextBuilder);
                        _guiWindow.Run(30);
                    }).Start();
                }
            }
            catch
            {
                Console.ReadKey();
            }
        }
    }
}