using log4net;
using log4net.Config;
using log4net.Repository;
using Microsoft.Extensions.Configuration;
using NosCore.Core.Logger;
using NosCore.DAL;
using NosCore.GameObject.Map;
using NosCore.Mapping;
using OpenTK;
using OpenTK.Graphics;
using OpenTK.Graphics.OpenGL;
using OpenTK.Input;
using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.IO;
using System.Reflection;
using System.Threading;

namespace NosCore.PathFinder.Gui
{
    public class GuiWindow : GameWindow
    {
        private readonly Map _map;
        private double _gridsizeX;
        private double _gridsizeY;
        private int originalWidth;
        private int originalHeight;
        private byte _gridsize;
        List<Tuple<short, short, byte>> walls = new List<Tuple<short, short, byte>>();
        public GuiWindow(Map map, byte gridsize, int width, int height, GraphicsMode mode, string title) : base(width * gridsize, height * gridsize, mode, title)
        {
            originalWidth = width * gridsize;
            originalHeight = height * gridsize;
            _map = map;
            _gridsizeX = gridsize;
            _gridsizeY = gridsize;
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
            _gridsizeX = _gridsize * (ClientRectangle.Width / (double)originalWidth);
            _gridsizeY = _gridsize * (ClientRectangle.Height / (double)originalHeight);
            Matrix4 world = Matrix4.CreateOrthographicOffCenter(0, ClientRectangle.Width, ClientRectangle.Height, 0, 0, 1);
            GL.LoadMatrix(ref world);
            //walls.ForEach(w => DrawPixel(w.Item1, w.Item2, Color.Blue));//TODO iswalkable
            foreach(Tuple<short, short, byte> wall in walls)
            {
                DrawPixel(wall.Item1, wall.Item2, Color.Blue);//TODO iswalkable
            }
            GL.Flush();
            SwapBuffers();
            Thread.Sleep(32);
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
            GL.Vertex2(x * _gridsizeX, y * _gridsizeY);
            GL.Vertex2(_gridsizeX * (x + 1), y * _gridsizeY);
            GL.Vertex2(_gridsizeX * (x + 1), _gridsizeY * (y + 1));
            GL.Vertex2(x * _gridsizeX, _gridsizeY * (y + 1));
            GL.End();
        }
    }
}
