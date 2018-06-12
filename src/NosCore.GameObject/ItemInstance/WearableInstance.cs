using System;
using NosCore.GameObject.ComponentEntities.Interfaces;

namespace NosCore.GameObject.ItemInstance
{
	public class WearableInstance : ItemInstance, IVisualEntity
	{
		public string Name { get; set; }
		public short? Amount { get; set; }
		public byte VisualType { get; set; } = 9;
		public short VNum { get; set; }
		public long VisualId { get; set; }
		public byte? Direction { get; set; }
		public Guid MapInstanceId { get; set; }
		public short PositionX { get; set; }
		public short PositionY { get; set; }
	}
}