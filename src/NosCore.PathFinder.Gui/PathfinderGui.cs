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
using ChickenAPI.Packets.ClientPackets.Inventory;
using Mapster;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Moq;
using NosCore.Configuration;
using NosCore.Core;
using NosCore.Core.I18N;
using NosCore.Data;
using NosCore.Data.Dto;
using NosCore.Data.Enumerations.I18N;
using NosCore.Data.StaticEntities;
using NosCore.Database;
using NosCore.Database.DAL;
using NosCore.Database.Entities;
using NosCore.GameObject;
using NosCore.GameObject.Mapping;
using NosCore.GameObject.Providers.ItemProvider;
using OpenTK.Graphics;
using Serilog;
using InventoryItemInstance = NosCore.GameObject.Providers.InventoryService.InventoryItemInstance;
using Item = NosCore.GameObject.Providers.ItemProvider.Item.Item;

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
        private static readonly IGenericDao<MapDto> _mapDao = new GenericDao<Map, MapDto, short>(Logger);
        private static readonly IGenericDao<NpcMonsterDto> _npcMonsterDao = new GenericDao<NpcMonster, NpcMonsterDto, long>(Logger);

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
                DataAccessHelper.Instance.Initialize(optionsBuilder.Options);

                var npcMonsters = _npcMonsterDao.LoadAll().ToList();
                TypeAdapterConfig<MapMonsterDto, GameObject.MapMonster>.NewConfig().ConstructUsing(src => new GameObject.MapMonster(npcMonsters, Logger));
                TypeAdapterConfig<MapNpcDto, GameObject.MapNpc>.NewConfig().ConstructUsing(src => new GameObject.MapNpc(new Mock<IItemProvider>().Object, new Mock<IGenericDao<ShopDto>>().Object, new Mock<IGenericDao<ShopItemDto>>().Object, npcMonsters, Logger));
                while (true)
                {
                    Logger.Information(LogLanguage.Instance.GetMessageFromKey(LogLanguageKey.SELECT_MAPID));
                    var input = Console.ReadLine();
                    if ((input == null) || !int.TryParse(input, out var askMapId))
                    {
                        Logger.Error(LogLanguage.Instance.GetMessageFromKey(LogLanguageKey.WRONG_SELECTED_MAPID));
                        continue;
                    }

                    var map = _mapDao.FirstOrDefault(m => m.MapId == askMapId)?.Adapt<GameObject.Map.Map>();

                    if ((map?.XLength > 0) && (map.YLength > 0))
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