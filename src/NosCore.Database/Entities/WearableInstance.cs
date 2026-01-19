//  __  _  __    __   ___ __  ___ ___
// |  \| |/__\ /' _/ / _//__\| _ \ __|
// | | ' | \/ |`._`.| \_| \/ | v / _|
// |_|\__|\__/ |___/ \__/\__/|_|_\___|
//

namespace NosCore.Database.Entities
{
    public class WearableInstance : ItemInstance
    {
        public byte? Ammo { get; set; }

        public byte? Cellon { get; set; }

        public short? CloseDefence { get; set; }

        public short? Concentrate { get; set; }

        public short? CriticalDodge { get; set; }

        public byte? CriticalLuckRate { get; set; }

        public short? CriticalRate { get; set; }

        public short? DamageMaximum { get; set; }

        public short? DamageMinimum { get; set; }

        public byte? DarkElement { get; set; }

        public short? DarkResistance { get; set; }

        public short? DefenceDodge { get; set; }

        public short? DistanceDefence { get; set; }

        public short? DistanceDefenceDodge { get; set; }

        public short? ElementRate { get; set; }

        public byte? FireElement { get; set; }

        public short? FireResistance { get; set; }

        public short? HitRate { get; set; }

        public short? Hp { get; set; }

        public bool? IsEmpty { get; set; }

        public bool? IsFixed { get; set; }

        public byte? LightElement { get; set; }

        public short? LightResistance { get; set; }

        public short? MagicDefence { get; set; }

        public short? MaxElementRate { get; set; }

        public short? Mp { get; set; }

        public byte? ShellRarity { get; set; }

        public byte? WaterElement { get; set; }

        public short? WaterResistance { get; set; }

        public long? Xp { get; set; }
    }
}
