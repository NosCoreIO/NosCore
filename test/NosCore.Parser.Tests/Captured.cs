//  __  _  __    __   ___ __  ___ ___
// |  \| |/__\ /' _/ / _//__\| _ \ __|
// | | ' | \/ |`._`.| \_| \/ | v / _|
// |_|\__|\__/ |___/ \__/\__/|_|_\___|
//

using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using NosCore.Packets.ClientPackets.Inventory;
using NosCore.Packets.ClientPackets.Npcs;
using NosCore.Packets.ClientPackets.Shops;
using NosCore.Packets.Enumerations;
using NosCore.Packets.ServerPackets.Inventory;
using NosCore.Packets.ServerPackets.MiniMap;
using NosCore.Packets.ServerPackets.Portals;
using NosCore.Packets.ServerPackets.Production;
using NosCore.Packets.ServerPackets.Shop;
using NosCore.Packets.ServerPackets.Visibility;
using NosCore.Shared.Enumerations;

namespace NosCore.Parser.Tests
{
    // The packets a recorded session holds, as the parser reads them.
    internal static class Captured
    {
        public static AtPacket Arrival(short mapId, short x = 50, short y = 50, int music = 0) =>
            new() { CharacterId = 1, MapId = mapId, PositionX = x, PositionY = y, Direction = 2, Music = music };

        public static InPacket Npc(short vnum, long id, short x, short y, byte direction = 2, short dialog = 0, bool sitting = false) =>
            new()
            {
                VisualType = VisualType.Npc,
                VNum = vnum.ToString(CultureInfo.InvariantCulture),
                VisualId = id,
                PositionX = x,
                PositionY = y,
                Direction = direction,
                InNonPlayerSubPacket = new InNonPlayerSubPacket { Dialog = dialog, IsSitting = sitting }
            };

        public static InPacket Monster(short vnum, long id, short x, short y, byte direction = 2) =>
            new()
            {
                VisualType = VisualType.Monster,
                VNum = vnum.ToString(CultureInfo.InvariantCulture),
                VisualId = id,
                PositionX = x,
                PositionY = y,
                Direction = direction,
                InNonPlayerSubPacket = new InNonPlayerSubPacket()
            };

        public static GpPacket Portal(short x, short y, short destination, PortalType type) =>
            new() { SourceX = x, SourceY = y, MapId = destination, PortalType = type };

        public static WpPacket Waypoint(short x, short y, WpPortalType type, byte levelMinimum, byte levelMaximum,
            short timeSpaceId = 0) =>
            new()
            {
                PositionX = x, PositionY = y, ScriptedInstanceId = timeSpaceId, PortalType = type,
                LevelMinimum = levelMinimum, LevelMaximum = levelMaximum
            };

        public static NrunPacket Talk(NrunRunnerType runner, short option, long npcId) =>
            new() { Runner = runner, Type = option, VisualType = VisualType.Npc, VisualId = npcId };

        public static ShopPacket Shop(long npcId, byte menuType, byte shopType, string name) =>
            new() { VisualType = VisualType.Npc, VisualId = npcId, MenuType = menuType, ShopType = shopType, Name = name };

        public static ShoppingPacket Shopping(short shopType, long npcId) =>
            new() { ShopType = shopType, VisualType = VisualType.Npc, VisualId = npcId };

        public static NInvPacket SkillShop(long npcId, byte tab, params short[] skills) =>
            new() { VisualType = VisualType.Npc, VisualId = npcId, ShopKind = tab, Skills = skills.ToList() };

        public static NInvPacket ItemShop(long npcId, params NInvItemSubPacket[] items) =>
            new() { VisualType = VisualType.Npc, VisualId = npcId, ShopKind = 100, Items = items.ToList<NInvItemSubPacket?>() };

        public static NInvPacket FamilyShop(long npcId, params short[] items) =>
            new()
            {
                VisualType = VisualType.Npc,
                VisualId = npcId,
                FamilySkills = items.Select((vnum, slot) => (NInvFamilySkillSubPacket?)new NInvFamilySkillSubPacket
                {
                    SkillId = vnum,
                    Type = 2,
                    Slot = (byte)slot
                }).ToList()
            };

        public static InvPacket MainPocket(params (short Slot, short VNum)[] slots) =>
            new() { Type = PocketType.Main, IvnSubPackets = slots.Select(Slot).ToList() };

        public static IvnPacket MainPocketChange(short slot, short vnum) =>
            new() { Type = PocketType.Main, IvnSubPackets = [Slot((slot, vnum))] };

        public static UseItemPacket Use(PocketType pocket, short slot) =>
            new() { VisualType = VisualType.Player, VisualId = 1, Type = pocket, Slot = slot };

        public static MlistPacket Makes(params short[] items) => new() { ItemVNums = items.ToList() };

        public static PdtsePacket ShowRecipe(short vnum) => new() { Type = PdtsePacketType.ShowRecipe, VNum = vnum };

        public static MlistMaterialsPacket Materials(short amount, params (short VNum, short Amount)[] materials) =>
            new()
            {
                Amount = amount,
                Materials = materials.Select(m => new MlistMaterialSubPacket { VNum = m.VNum, Amount = m.Amount }).ToList()
            };

        private static IvnSubPacket Slot((short Slot, short VNum) slot) => new() { Slot = slot.Slot, VNum = slot.VNum };
    }
}
