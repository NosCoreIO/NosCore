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
using System.Drawing;
using System.Linq;
using System.Threading.Tasks;
using NosCore.Packets.ClientPackets.Drops;
using Mapster;
using NosCore.Core.I18N;
using NosCore.Dao.Interfaces;
using NosCore.Data.Dto;
using NosCore.Data.Enumerations.Map;
using NosCore.Data.StaticEntities;
using NosCore.Database.Entities;
using NosCore.GameObject;
using NosCore.GameObject.Providers.MapInstanceProvider;
using NosCore.GameObject.Providers.MapInstanceProvider.Handlers;
using NosCore.GameObject.Providers.MapItemProvider;
using Serilog;
using Map = NosCore.GameObject.Map.Map;
using MapMonster = NosCore.Database.Entities.MapMonster;
using MapNpc = NosCore.Database.Entities.MapNpc;
using NosCore.Dao;
using NosCore.Database;
using OpenToolkit.Mathematics;
using OpenToolkit.Windowing.Desktop;
using OpenToolkit.Windowing.Common;
using OpenToolkit.Windowing.Common.Input;
using OpenToolkit.Graphics.OpenGL;

namespace NosCore.PathFinder.Gui
{
    public class GuiWindow : GameWindow
    {
        private static readonly ILogger Logger = Core.I18N.Logger.GetLoggerConfiguration().CreateLogger();
        private readonly byte _gridsize;
        private readonly Map _map;

        private readonly List<GameObject.MapMonster> _monsters;

        private readonly List<GameObject.MapNpc> _npcs;
        private readonly int _originalHeight;
        private readonly int _originalWidth;
        private readonly List<Tuple<short, short, byte>> _walls = new List<Tuple<short, short, byte>>();
        private double _gridsizeX;
        private double _gridsizeY;

        public GuiWindow(Map map, byte gridsize, int width, int height, string title, DataAccessHelper dbContextBuilder) : base(
                new GameWindowSettings
                {
                    IsMultiThreaded = true,
                    RenderFrequency = 30,
                    UpdateFrequency = 30
                },
                new NativeWindowSettings
                {
                    Size = new Vector2i(width * gridsize, height * gridsize),
                    Title = title,
                    API = ContextAPI.OpenGL,
                    APIVersion = new Version(3,3)
                })
        {
            var dbContextBuilder1 = dbContextBuilder;
            IDao<MapMonsterDto, int> mapMonsterDao = new Dao<MapMonster, MapMonsterDto, int>(Logger, dbContextBuilder1);
            IDao<MapNpcDto, int> mapNpcDao = new Dao<MapNpc, MapNpcDto, int>(Logger, dbContextBuilder1);
            IDao<NpcMonsterDto, short> npcMonsterDao = new Dao<NpcMonster, NpcMonsterDto, short>(Logger, dbContextBuilder1);
            _originalWidth = width * gridsize;
            _originalHeight = height * gridsize;
            _map = map;
            _gridsizeX = gridsize;
            _gridsizeY = gridsize;
            _gridsize = gridsize;
            _monsters = mapMonsterDao.Where(s => s.MapId == map.MapId)
                ?.Adapt<List<GameObject.MapMonster>>() ?? new List<GameObject.MapMonster>();
            var npcMonsters = npcMonsterDao.LoadAll().ToList();
            using var mapInstance =
                new MapInstance(map, new Guid(), false, MapInstanceType.BaseMapInstance,
                    new MapItemProvider(new List<IEventHandler<MapItem, Tuple<MapItem, GetPacket>>>()),
                    Logger, new List<IMapInstanceEventHandler>())
                {
                    IsSleeping = false
                };
            foreach (var mapMonster in _monsters)
            {
                mapMonster.PositionX = mapMonster.MapX;
                mapMonster.PositionY = mapMonster.MapY;
                mapMonster.MapInstance = mapInstance;
                mapMonster.MapInstanceId = mapInstance.MapInstanceId;
                mapMonster.Mp = 100;
                mapMonster.Hp = 100;
                mapMonster.Speed = npcMonsters.Find(s => s.NpcMonsterVNum == mapMonster.MapId)?.Speed ?? 0;
                mapMonster.IsAlive = true;
            }

            _npcs = mapNpcDao.Where(s => s.MapId == map.MapId)?.Adapt<List<GameObject.MapNpc>>() ?? new List<GameObject.MapNpc>();
            foreach (var mapNpc in _npcs)
            {
                mapNpc.PositionX = mapNpc.MapX;
                mapNpc.PositionY = mapNpc.MapY;
                mapNpc.MapInstance = mapInstance;
                mapNpc.MapInstanceId = mapInstance.MapInstanceId;
                mapNpc.Mp = 100;
                mapNpc.Hp = 100;
                mapNpc.Speed = npcMonsters.Find(s => s.NpcMonsterVNum == mapNpc.MapId)?.Speed ?? 0;
                mapNpc.IsAlive = true;
            }

            Parallel.ForEach(_monsters.Where(s => s.Life == null), monster => monster.StartLifeAsync());
            Parallel.ForEach(_npcs.Where(s => s.Life == null), npc => npc.StartLifeAsync());
            GetMap();
        }

        protected override void OnUpdateFrame(FrameEventArgs e)
        {
            base.OnUpdateFrame(e);

            var keyboardState = new KeyboardState();
            var lastKeyboardState = new KeyboardState();
            bool KeyPress(Key key)
            {
                return (keyboardState[key] && (keyboardState[key] != lastKeyboardState[key]));
            }

            if (KeyPress(Key.Escape))
            {
                Close();
            }
        }

        protected override void OnRenderFrame(FrameEventArgs e)
        {
            base.OnRenderFrame(e);
            GL.Clear(ClearBufferMask.ColorBufferBit | ClearBufferMask.DepthBufferBit);
            GL.ClearColor(Color.LightSkyBlue.A, Color.LightSkyBlue.R, Color.LightSkyBlue.G, Color.LightSkyBlue.B);
            _gridsizeX = _gridsize * (ClientRectangle.Size.X / (double)_originalWidth);
            _gridsizeY = _gridsize * (ClientRectangle.Size.Y / (double)_originalHeight);
            var world = Matrix4.CreateOrthographicOffCenter(0, ClientRectangle.Size.X, ClientRectangle.Size.Y, 0, 0, 1);
            GL.LoadMatrix(ref world);
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

            SwapBuffers();
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