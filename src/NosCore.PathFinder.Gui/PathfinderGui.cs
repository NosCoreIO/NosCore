using log4net;
using log4net.Config;
using log4net.Repository;
using Microsoft.Extensions.Configuration;
using NosCore.DAL;
using NosCore.Shared.Logger;
using NosCore.GameObject;
using NosCore.GameObject.Map;
using OpenTK.Graphics;
using System;
using System.Data.SqlClient;
using System.IO;
using System.Reflection;
using System.Threading;
using NosCore.Database;
using NosCore.Configuration;

namespace NosCore.PathFinder.Gui
{
    public static class PathFinderGui
    {
        private const string _configurationPath = @"..\..\..\configuration";
        private static readonly SqlConnectionConfiguration _databaseConfiguration = new SqlConnectionConfiguration();
        private static GuiWindow guiWindow;

        private static void InitializeConfiguration()
        {
            var builder = new ConfigurationBuilder();
            builder.SetBasePath(Directory.GetCurrentDirectory() + _configurationPath);
            builder.AddJsonFile("pathfinder.json", false);
            builder.Build().Bind(_databaseConfiguration);
        }

        private static void InitializeLogger()
        {
            // LOGGER
            ILoggerRepository logRepository = LogManager.GetRepository(Assembly.GetEntryAssembly());
            XmlConfigurator.Configure(logRepository, new FileInfo("../../configuration/log4net.config"));
            Logger.InitializeLogger(LogManager.GetLogger(typeof(PathFinderGui)));
        }

        private static void PrintHeader()
        {
            Console.Title = "NosCore - Pathfinder GUI";
            const string text = "PATHFINDER GUI - 0Lucifer0";
            int offset = (Console.WindowWidth / 2) + (text.Length / 2);
            string separator = new string('=', Console.WindowWidth);
            Console.WriteLine(separator + string.Format("{0," + offset + "}\n", text) + separator);
        }

        public static void Main()
        {
            PrintHeader();
            InitializeLogger();
            InitializeConfiguration();
            DAOFactory.RegisterMapping(typeof(Character).Assembly);
            if (DataAccessHelper.Instance.Initialize(_databaseConfiguration))
            {
                do
                {
                    Logger.Log.Info(LogLanguage.Instance.GetMessageFromKey(LogLanguageKey.SELECT_MAPID));
                    string input = Console.ReadLine();
                    if (input == null || !double.TryParse(input, out double askMapId))
                    {
                        Logger.Log.Error(LogLanguage.Instance.GetMessageFromKey(LogLanguageKey.WRONG_SELECTED_MAPID));
                        continue;
                    }
                    Map map = (Map)DAOFactory.MapDAO.FirstOrDefault(m => m.MapId == askMapId);

                    if (map != null && map.XLength > 0 && map.YLength > 0)
                    {
                        map.Initialize();

                        if (guiWindow?.Exists == true)
                        {
                            guiWindow.Exit();
                        }

                        new Thread(() =>
                        {
                            guiWindow = new GuiWindow(map, 4, map.XLength, map.YLength, GraphicsMode.Default, $"NosCore Pathfinder GUI - Map {map.MapId}");
                            guiWindow.Run(30);
                        }).Start();
                    }
                } while (true);
            }
        }
    }
}
