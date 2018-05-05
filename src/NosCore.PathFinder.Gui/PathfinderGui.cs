using System;
using System.IO;
using NosCore.GameObject.Map;
using OpenTK;
using OpenTK.Graphics;
using OpenTK.Graphics.OpenGL;
using OpenTK.Input;
using NosCore.Mapping;
using NosCore.DAL;
using Microsoft.Extensions.Configuration;
using System.Data.SqlClient;
using log4net.Repository;
using log4net.Config;
using NosCore.Core.Logger;
using log4net;
using System.Reflection;
using System.Linq;
using System.Collections.Generic;
using System.Threading;

namespace NosCore.PathFinder.Gui
{
    public class PathFinderGui : GameWindow
    {
        private readonly Map _map;
        private readonly byte _gridsize;
        List<Tuple<short, short, byte>> walls = new List<Tuple<short, short, byte>>();
        public PathFinderGui(Map map, byte gridsize, int width, int height, GraphicsMode mode, string title) : base(width * gridsize, height * gridsize, mode, title)
        {
            VSync = VSyncMode.On;
            _map = map;
            _gridsize = gridsize;
            GetMap(_map);
        }

        protected override void OnUpdateFrame(FrameEventArgs e)
        {
            base.OnUpdateFrame(e);

            if (Keyboard[Key.Escape])
                Exit();
        }

        protected override void OnRenderFrame(FrameEventArgs e)
        {
            base.OnRenderFrame(e);

            GL.ClearColor(Color.LightSkyBlue);
            GL.Clear(ClearBufferMask.ColorBufferBit | ClearBufferMask.DepthBufferBit);
            Matrix4 world = Matrix4.CreateOrthographicOffCenter(0, ClientRectangle.Width, ClientRectangle.Height, 0, 0, 1);
            GL.LoadMatrix(ref world);
            walls.ForEach(w=>DrawPixel(w.Item1, w.Item2, Color.Blue));//TODO iswalkable
            GL.Flush();
            SwapBuffers();

        }

        private void GetMap(Map map)
        {
            for (short i = 0; i < _map.YLength; i++)
            {
                for (short t = 0; t < _map.XLength; t++)
                {
                    var value = _map[t, i];
                    if (_map[t, i] > 0)
                    {
                        walls.Add(new Tuple<short, short, byte>(t, i, value));
                    }
                }
            }
        }

        private void DrawPixel(short x, short y, Color color)
        {
            GL.Begin(PrimitiveType.Quads);
            GL.Color3(color);
            GL.Vertex2(x * _gridsize, y * _gridsize);
            GL.Vertex2(_gridsize * (x + 1), y * _gridsize);
            GL.Vertex2(_gridsize * (x + 1), _gridsize * (y + 1));
            GL.Vertex2(x * _gridsize, _gridsize * (y + 1));
            GL.End();
        }

        private const string _configurationPath = @"..\..\..\configuration";
        private static readonly SqlConnectionStringBuilder _databaseConfiguration = new SqlConnectionStringBuilder();

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

        [STAThread]
        public static void Main()
        {
            PrintHeader();
            InitializeLogger();
            InitializeConfiguration();
            Mapper.InitializeMapping();
            if (DataAccessHelper.Instance.Initialize(_databaseConfiguration))
            {
                while (true)
                {
                    Logger.Log.Info(LogLanguage.Instance.GetMessageFromKey(LogLanguageKey.SELECT_MAPID));
                    string input = Console.ReadLine();
                    double askMapId;
                    if (String.IsNullOrEmpty(input) || !double.TryParse(input, out askMapId))
                    {
                        Logger.Log.Error(LogLanguage.Instance.GetMessageFromKey(LogLanguageKey.WRONG_SELECTED_MAPID));
                        continue;
                    }
                    Map map = (Map)DAOFactory.MapDAO.FirstOrDefault(m => m.MapId == askMapId);

                    if (map.MapId == askMapId && map.XLength > 0 && map.YLength > 0)
                    {
                        map.Initialize();

                        var task = new Thread(() =>
                        {
                            using (PathFinderGui game = new PathFinderGui(map, 5, map.XLength, map.YLength, GraphicsMode.Default, $"NosCore Pathfinder GUI - Map {map.MapId}"))
                            {
                                game.Run(60);
                            //game.Exit(); exec if map change
                        }
                        });
                        task.Start();
                    }
                }
            }
        }
    }
}
