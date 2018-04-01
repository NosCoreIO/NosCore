namespace NosCore.GameObject.ComponentEntities.Interfaces
{
    public interface INonPlayableEntity
    {
        bool IsMoving { get; set; }

        short Effect { get; set; }

        short EffectDelay { get; set; }

        bool IsDisabled { get; set; }

    }
}
