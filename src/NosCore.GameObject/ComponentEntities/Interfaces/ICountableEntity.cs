namespace NosCore.GameObject.ComponentEntities.Interfaces
{
    public interface ICountableEntity : IVisualEntity
    {
        short Amount { get; set; }
    }
}