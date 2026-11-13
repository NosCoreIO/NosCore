//  __  _  __    __   ___ __  ___ ___
// |  \| |/__\ /' _/ / _//__\| _ \ __|
// | | ' | \/ |`._`.| \_| \/ | v / _|
// |_|\__|\__/ |___/ \__/\__/|_|_\___|
//

using System;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using NosCore.Data.Enumerations;
using NosCore.Data.Enumerations.Character;
using NosCore.Data.Enumerations.Items;
using NosCore.Database;
using NosCore.Database.Entities;
using NosCore.Packets.Enumerations;
using NosCore.Shared.Enumerations;
using FamilyAuthority = NosCore.Data.Enumerations.Family.FamilyAuthority;

namespace NosCore.EndToEnd.Tests.Infrastructure
{
    // The smallest world a server will run: no client files, only what the scenarios stand on.
    // Two open maps joined by a portal, a miniland, a shopkeeper with one item to sell, a broker
    // to talk to, a chest and a weapon to hand out, what a new adventurer is given, and creatures
    // to hit. Characters are added by the scenarios.
    public static class WorldData
    {
        public const string Password = "test";
        public const short Town = 1;
        public const short Meadow = 2;
        public const short MinilandMap = 20001;
        public const short Seed = 2024;
        public const long SeedPrice = 100;
        public const short Chest = 3000;
        public const short Blade = 4000;
        public const int ChestSlots = 4;
        public const int Shopkeeper = 1;
        private const short ShopkeeperVNum = 1;
        public const int WeakMonster = 100;
        public const int SturdyMonster = 101;
        private const short WeakVNum = 2;
        private const short SturdyVNum = 3;

        // Falls to any hit and takes ten minutes to come back: only one scenario may kill it.
        public const int SlowMonster = 102;
        private const short SlowVNum = 4;
        public const byte FamilySize = 50;

        // Has something to say, so talking to it is answered, and stands away from the shopkeeper.
        public const int Broker = 2;
        public const short BrokerDialog = 1;
        private const short BrokerVNum = 5;

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
            context.Add(new Item
            {
                VNum = Seed,
                Name = "seed",
                Type = NoscorePocketType.Etc,
                ItemType = ItemType.Main,
                Price = SeedPrice,
                IsDroppable = true,
                IsSoldable = true,
                IsTradable = true
            });
            // Kept in the miniland pocket, a chest is a warehouse of as many slots as its points.
            context.Add(new Item
            {
                VNum = Chest, Name = "chest", Type = NoscorePocketType.Miniland, ItemType = ItemType.House,
                IsWarehouse = true, MinilandObjectPoint = ChestSlots
            });
            // A weapon that may change hands, unlike what an adventurer starts with.
            context.Add(new Item
            {
                VNum = Blade, Name = "blade", Type = NoscorePocketType.Equipment, ItemType = ItemType.Weapon,
                EquipmentSlot = EquipmentType.MainWeapon, Price = 1000, IsSoldable = true, IsTradable = true
            });
            // What a new adventurer is given: its club, its catapult, its clothes, and its three skills.
            context.Add(Worn(1, EquipmentType.MainWeapon, ItemType.Weapon));
            context.Add(Worn(8, EquipmentType.SecondaryWeapon, ItemType.Weapon));
            context.Add(Worn(12, EquipmentType.Armor, ItemType.Armor));
            context.Add(new Item { VNum = 2081, Name = "item2081", Type = NoscorePocketType.Etc, ItemType = ItemType.Main });
            // As the client data has them: skill type 1 is an active skill.
            context.Add(new Skill
            {
                SkillVNum = 200, Name = "skill200", CastId = 0, SkillType = 1, Type = 0, Range = 1, CastTime = 2, Cooldown = 6,
                Effect = 200, AttackAnimation = 11, CastAnimation = -1, CastEffect = -1
            });
            context.Add(new Skill
            {
                SkillVNum = 201, Name = "skill201", CastId = 1, SkillType = 1, Type = 1, Range = 7, CastTime = 6, Cooldown = 6,
                Effect = 201, AttackAnimation = 23, CastAnimation = 26, CastEffect = -1
            });
            context.Add(new Skill
            {
                SkillVNum = 209, Name = "skill209", CastId = 16, SkillType = 1, CastTime = 6, Cooldown = 150, MpCost = 10,
                UpgradeSkill = 3, LevelMinimum = 1, Effect = -1, AttackAnimation = -1, CastAnimation = -1, CastEffect = -1
            });

            // Creatures that stand still on the meadow: one that falls to any hit, one that does
            // not, and one that stays dead for a while.
            context.Add(new NpcMonster { NpcMonsterVNum = WeakVNum, Name = "weak", Level = 1, MaxHp = 1, Xp = 40, JobXp = 10 });
            context.Add(new NpcMonster { NpcMonsterVNum = SturdyVNum, Name = "sturdy", Level = 1, MaxHp = 100000 });
            context.Add(new NpcMonster { NpcMonsterVNum = SlowVNum, Name = "slow", Level = 1, MaxHp = 1, RespawnTime = 6000 });
            await context.SaveChangesAsync();
            context.Add(new MapMonster { MapMonsterId = WeakMonster, MapId = Meadow, MapX = 30, MapY = 30, VNum = WeakVNum });
            context.Add(new MapMonster { MapMonsterId = SturdyMonster, MapId = Meadow, MapX = 60, MapY = 60, VNum = SturdyVNum });
            context.Add(new MapMonster { MapMonsterId = SlowMonster, MapId = Meadow, MapX = 45, MapY = 45, VNum = SlowVNum });

            context.Add(new NpcMonster { NpcMonsterVNum = ShopkeeperVNum, Name = "shopkeeper", Level = 1, MaxHp = 100 });
            await context.SaveChangesAsync();

            var shopkeeper = new MapNpc { MapNpcId = Shopkeeper, MapId = Town, MapX = 84, MapY = 118, VNum = ShopkeeperVNum };
            context.Add(shopkeeper);
            var shop = new Shop { MapNpcId = Shopkeeper, MenuType = 0, ShopType = 0 };
            context.Add(shop);
            await context.SaveChangesAsync();
            context.Add(new ShopItem { ShopId = shop.ShopId, ItemVNum = Seed, Slot = 0, Type = 0 });
            context.Add(new NpcTalk { DialogId = BrokerDialog, Name = "broker" });
            context.Add(new NpcMonster { NpcMonsterVNum = BrokerVNum, Name = "broker", Level = 1, MaxHp = 100 });
            await context.SaveChangesAsync();
            context.Add(new MapNpc { MapNpcId = Broker, MapId = Town, MapX = 76, MapY = 118, VNum = BrokerVNum, Dialog = BrokerDialog });
            await context.SaveChangesAsync();
        }

        private static Item Worn(short vnum, EquipmentType slot, ItemType type) => new()
        {
            VNum = vnum, Name = "item" + vnum, Type = NoscorePocketType.Equipment, ItemType = type, EquipmentSlot = slot
        };

        // What a character carries has to be there before it is selected: the bag is read then.
        public static Task GiveAsync(NosCoreContext context, long characterId, short vnum, short amount, short slot,
            NoscorePocketType pocket = NoscorePocketType.Etc) =>
            GiveAsync(context, characterId, new ItemInstance { ItemVNum = vnum, Amount = amount }, slot, pocket);

        public static async Task GiveAsync(NosCoreContext context, long characterId, ItemInstance item, short slot, NoscorePocketType pocket)
        {
            item.Id = Guid.NewGuid();
            item.CharacterId = characterId;
            context.Add(item);
            context.Add(new InventoryItemInstance
            {
                Id = Guid.NewGuid(), CharacterId = characterId, ItemInstanceId = item.Id, Slot = slot, Type = pocket
            });
            await context.SaveChangesAsync();
        }

        // An account of its own for every character, so that scenarios never meet on one.
        public static async Task<Player> AddPlayerAsync(NosCoreContext context, string name, short mapId, short x, short y,
            Action<Character>? configure = null)
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
            configure?.Invoke(character);
            context.Add(character);
            await context.SaveChangesAsync();
            foreach (var skill in new short[] { 200, 201, 209 })
            {
                context.Add(new CharacterSkill { Id = Guid.NewGuid(), CharacterId = character.CharacterId, SkillVNum = skill });
            }

            await context.SaveChangesAsync();
            return new Player(account.Name, name, character.CharacterId, character.Slot);
        }

        public static async Task<FoundedFamily> AddFamilyAsync(NosCoreContext context, string name, Action<Family>? configure,
            Player head, params Player[] members)
        {
            var family = new Family { Name = name, FamilyLevel = 1, MaxSize = FamilySize, FamilyMessage = string.Empty };
            configure?.Invoke(family);
            context.Add(family);
            await context.SaveChangesAsync();
            context.Add(new FamilyCharacter
            {
                FamilyId = family.FamilyId, CharacterId = head.CharacterId, Authority = FamilyAuthority.Head, DailyMessage = string.Empty
            });
            foreach (var member in members)
            {
                context.Add(new FamilyCharacter
                {
                    FamilyId = family.FamilyId, CharacterId = member.CharacterId, Authority = FamilyAuthority.Member,
                    DailyMessage = string.Empty
                });
            }

            await context.SaveChangesAsync();
            return new FoundedFamily(family.FamilyId, name);
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

    public sealed record FoundedFamily(long FamilyId, string Name);
}
