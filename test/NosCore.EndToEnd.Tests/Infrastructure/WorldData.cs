//  __  _  __    __   ___ __  ___ ___
// |  \| |/__\ /' _/ / _//__\| _ \ __|
// | | ' | \/ |`._`.| \_| \/ | v / _|
// |_|\__|\__/ |___/ \__/\__/|_|_\___|
//

using System;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using NosCore.Data.Enumerations.Character;
using NosCore.Database;
using NosCore.Database.Entities;
using NosCore.Packets.Enumerations;
using NosCore.Shared.Enumerations;

namespace NosCore.EndToEnd.Tests.Infrastructure
{
    // The smallest world a server will run: no client files, only what the scenarios stand on.
    // Two open maps joined by a portal, and a miniland. Characters are added by the scenarios.
    public static class WorldData
    {
        public const string Password = "test";
        public const short Town = 1;
        public const short Meadow = 2;
        public const short MinilandMap = 20001;

        public static async Task SeedAsync(NosCoreContext context)
        {
            context.Add(OpenMap(Town, 200, 200));
            context.Add(OpenMap(Meadow, 100, 100));
            context.Add(OpenMap(MinilandMap, 40, 40));
            context.Add(new Portal
            {
                SourceMapId = Town, SourceX = 90, SourceY = 116,
                DestinationMapId = Meadow, DestinationX = 10, DestinationY = 10,
                Type = PortalType.Open
            });
            context.Add(new Portal
            {
                SourceMapId = Meadow, SourceX = 8, SourceY = 10,
                DestinationMapId = Town, DestinationX = 88, DestinationY = 116,
                Type = PortalType.Open
            });
            context.Add(new Portal
            {
                SourceMapId = Town, SourceX = 70, SourceY = 116,
                DestinationMapId = MinilandMap, DestinationX = 5, DestinationY = 8,
                Type = PortalType.Miniland
            });
            await context.SaveChangesAsync();
        }

        // An account of its own for every character, so that scenarios never meet on one.
        public static async Task<Player> AddPlayerAsync(NosCoreContext context, string name, short mapId, short x, short y)
        {
            var account = new Account
            {
                Name = name.ToLowerInvariant(),
                Password = Convert.ToHexString(SHA512.HashData(Encoding.Default.GetBytes(Password))).ToLowerInvariant(),
                Authority = AuthorityType.User,
                Language = RegionType.EN
            };
            context.Add(account);
            await context.SaveChangesAsync();

            var character = new Character
            {
                AccountId = account.AccountId,
                Name = name,
                Slot = 0,
                MapId = mapId,
                MapX = x,
                MapY = y,
                Level = 1,
                JobLevel = 1,
                Hp = 221,
                Mp = 60,
                State = CharacterState.Active
            };
            context.Add(character);
            await context.SaveChangesAsync();
            return new Player(account.Name, name, character.CharacterId, character.Slot);
        }

        // Width and height, then one byte a cell; 0 is a cell that can be walked on.
        private static Map OpenMap(short mapId, short width, short height)
        {
            var data = new byte[4 + width * height];
            BitConverter.TryWriteBytes(data.AsSpan(0, 2), width);
            BitConverter.TryWriteBytes(data.AsSpan(2, 2), height);
            return new Map { MapId = mapId, Data = data, Name = "map" + mapId };
        }
    }

    public sealed record Player(string Account, string Name, long CharacterId, byte Slot);
}
