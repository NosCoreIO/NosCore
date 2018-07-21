using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using NosCore.Data.AliveEntities;
using NosCore.Data.StaticEntities;
using NosCore.DAL;
using NosCore.GameObject;
using NosCore.GameObject.Map;
using OpenTK;
using OpenTK.Graphics;
using OpenTK.Graphics.OpenGL;
using OpenTK.Input;

namespace NosCore.PathFinder.Gui
{
    public class GuiWindow : GameWindow
    {
        private readonly byte _gridsize;
        private readonly Map _map;
        private readonly int _originalHeight;
        private readonly int _originalWidth;
        private readonly List<Tuple<short, short, byte>> _walls = new List<Tuple<short, short, byte>>();
        private double _gridsizeX;
        private double _gridsizeY;
        private List<MapMonster> _monsters;
        private List<MapNpc> _npcs;

        public GuiWindow(Map map, byte gridsize, int width, int height, GraphicsMode mode, string title) : base(
            width * gridsize, height * gridsize, mode, title)
        {
            _originalWidth = width * gridsize;
            _originalHeight = height * gridsize;
            _map = map;
            _gridsizeX = gridsize;
            _gridsizeY = gridsize;
            _gridsize = gridsize;
            _monsters = DAOFactory.MapMonsterDAO.Where(s=>s.MapId == map.MapId).Cast<MapMonster>().ToList();
            foreach (var mapMonster in _monsters)
            {
                mapMonster.PositionX = mapMonster.MapX;
                mapMonster.PositionY = mapMonster.MapY;
            }
            _npcs = DAOFactory.MapNpcDAO.Where(s => s.MapId == map.MapId).Cast<MapNpc>().ToList();
            foreach (var mapMonster in _npcs)
            {
                mapMonster.PositionX = mapMonster.MapX;
                mapMonster.PositionY = mapMonster.MapY;
            }
            GetMap();
        }

        protected override void OnUpdateFrame(FrameEventArgs e)
        {
            base.OnUpdateFrame(e);

            if (Keyboard[Key.Escape])
            {
                Exit();
            }
        }

        protected override void OnRenderFrame(FrameEventArgs e)
        {
            base.OnRenderFrame(e);

            GL.ClearColor(Color.LightSkyBlue);
            GL.Clear(ClearBufferMask.ColorBufferBit | ClearBufferMask.DepthBufferBit);
            _gridsizeX = _gridsize * (ClientRectangle.Width / (double) _originalWidth);
            _gridsizeY = _gridsize * (ClientRectangle.Height / (double) _originalHeight);
            var world = Matrix4.CreateOrthographicOffCenter(0, ClientRectangle.Width, ClientRectangle.Height, 0, 0, 1);
            GL.LoadMatrix(ref world);
            //walls.ForEach(w => DrawPixel(w.Item1, w.Item2, Color.Blue));//TODO iswalkable
            foreach (var wall in _walls)
            {
                DrawPixel(wall.Item1, wall.Item2, Color.Blue); //TODO iswalkable
            }

            foreach (var monster in _monsters)
            {
                DrawPixel(monster.PositionX, monster.PositionY, Color.Red);
            }

            foreach (var npc in _npcs)
            {
                DrawPixel(npc.PositionX, npc.PositionY, Color.Yellow);
            }

            GL.Flush();
            SwapBuffers();
            Thread.Sleep(32);
        }

        private void GetMap()
        {
            for (short i = 0; i < _map.YLength; i++)
            {
                for (short t = 0; t < _map.XLength; t++)
                {
                    var value = _map[t, i];
                    if (_map[t, i] > 0)
                    {
                        _walls.Add(new Tuple<short, short, byte>(t, i, value));
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