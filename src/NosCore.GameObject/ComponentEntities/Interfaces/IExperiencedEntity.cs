namespace NosCore.GameObject.ComponentEntities.Interfaces
{
    public interface IExperiencedEntity
    {
        byte Level { get; set; }

        long LevelXp { get; set; }
    }
}
