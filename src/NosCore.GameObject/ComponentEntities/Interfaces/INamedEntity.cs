using NosCore.Data.StaticEntities;

namespace NosCore.GameObject.ComponentEntities.Interfaces
{
	public interface INamedEntity : IAliveEntity
	{
		string Name { get; set; }
    }
}