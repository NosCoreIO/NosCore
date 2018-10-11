using System;
using System.IO;
using System.Reflection;
using System.Threading;
using log4net;
using log4net.Config;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using NosCore.Configuration;
using NosCore.DAL;
using NosCore.Database;
using NosCore.GameObject.Map;
using NosCore.Shared.I18N;
using OpenTK.Graphics;

namespace NosCore.PathFinder.Gui
{
    public static class PathFinderGui
    {
        private const string ConfigurationPath = @"../../../configuration";
        private const string Title = "NosCore - Pathfinder GUI";
        private static readonly PathfinderGUIConfiguration DatabaseConfiguration = new PathfinderGUIConfiguration();
        private static GuiWindow _guiWindow;

        private static void InitializeConfiguration()
        {
            var builder = new ConfigurationBuilder();
            builder.SetBasePath(Directory.GetCurrentDirectory() + ConfigurationPath);
            builder.AddJsonFile("pathfinder.json", false);
            builder.Build().Bind(DatabaseConfiguration);
        }

        private static void InitializeLogger()
        {
            // LOGGER
            var logRepository = LogManager.GetRepository(Assembly.GetEntryAssembly());
            XmlConfigurator.Configure(logRepository, new FileInfo("../../configuration/log4net.config"));
            Logger.InitializeLogger(LogManager.GetLogger(typeof(PathFinderGui)));
        }

        private static void PrintHeader()
        {
            Console.Title = Title;
            const string text = "PATHFINDER GUI - 0Lucifer0";
            var offset = (Console.WindowWidth / 2) + (text.Length / 2);
            var separator = new string('=', Console.WindowWidth);
            Console.WriteLine(separator + string.Format("{0," + offset + "}\n", text) + separator);
        }

        public static void Main()
        {
            PrintHeader();
            InitializeLogger();
            InitializeConfiguration();
            LogLanguage.Language = DatabaseConfiguration.Language;
            try
            {
                var optionsBuilder = new DbContextOptionsBuilder<NosCoreContext>();
                optionsBuilder.UseNpgsql(DatabaseConfiguration.Database.ConnectionString);
                DataAccessHelper.Instance.Initialize(optionsBuilder.Options);

                while (true)
                {
                    Logger.Log.Info(LogLanguage.Instance.GetMessageFromKey(LanguageKey.SELECT_MAPID));
                    var input = Console.ReadLine();
                    if (input == null || !int.TryParse(input, out var askMapId))
                    {
                        Logger.Log.Error(LogLanguage.Instance.GetMessageFromKey(LanguageKey.WRONG_SELECTED_MAPID));
                        continue;
                    }

                    var map = (Map) DAOFactory.MapDAO.FirstOrDefault(m => m.MapId == askMapId);

                    if (map?.XLength > 0 && map.YLength > 0)
                    {
                        map.Initialize();

                        if (_guiWindow?.Exists == true)
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