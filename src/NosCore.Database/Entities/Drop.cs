namespace NosCore.Database.Entities
{
	public class Drop
	{
		#region Properties

		public int Amount { get; set; }

		public int DropChance { get; set; }

		public short DropId { get; set; }

		public virtual Item Item { get; set; }

		public short ItemVNum { get; set; }

		public virtual MapType MapType { get; set; }

		public short? MapTypeId { get; set; }

		public short? MonsterVNum { get; set; }

		public virtual NpcMonster NpcMonster { get; set; }

		#endregion
	}
}