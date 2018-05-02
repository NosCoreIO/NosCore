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

        public PathFinderGui(Map map, byte gridsize, int width, int height, GraphicsMode mode, string title) : base(width * gridsize, height * gridsize, mode, title)
        {
            VSync = VSyncMode.On;
            _map = map;
            _gridsize = gridsize;
        }

        protected override void OnLoad(EventArgs e)
        {
            base.OnLoad(e);
            GL.ClearColor(0.1f, 0.2f, 0.5f, 0.0f);
        }

        protected override void OnResize(EventArgs e)
        {
            base.OnResize(e);
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
            PrintMap(_map);
        }

        private void PrintMap(Map map)
        {
            GL.Clear(ClearBufferMask.ColorBufferBit | ClearBufferMask.DepthBufferBit);
            for (short i = 0; i < _map.YLength; i++)
            {
                for (short t = 0; t < _map.XLength; t++)
                {
                    if (_map[t, i] == 1)//TODO iswalkable
                    {
                        DrawPixel(t, i, Color.Aqua);
                    }
                }
            }
            GL.Flush();
            SwapBuffers();
        }

        private void DrawPixel(short x, short y, Color color)
        {
            var pixelsizex = _gridsize * 2 / (double)(ClientRectangle.Width);
            var pixelsizey = _gridsize * 2 / (double)(ClientRectangle.Height);


            GL.Begin(PrimitiveType.Quads);
            GL.Color3(color);
            GL.Vertex2(-1 + (x + 1) * pixelsizex, 1 - (y + 1) * pixelsizey);
            GL.Vertex2(-1 + (x + 1) * pixelsizex, 1 - y * pixelsizey);
            GL.Vertex2(-1 + x * pixelsizex, 1 - y * pixelsizey);
            GL.Vertex2(-1 + x * pixelsizex, 1 - (y + 1) * pixelsizey);

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
                Map map = (Map)DAOFactory.MapDAO.FirstOrDefault(m => m.MapId == 1);
                map.Initialize();
                using (PathFinderGui game = new PathFinderGui(map, 5, map.XLength, map.YLength, GraphicsMode.Default, $"NosCore Pathfinder GUI - Map {map.MapId}"))
                {
                    game.Run(30);
                }
                Thread.Sleep(10000);
            }
        }
    }
}
