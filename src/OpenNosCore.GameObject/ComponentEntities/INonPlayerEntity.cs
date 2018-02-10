using OpenNosCore.Packets;

namespace OpenNosCore.GameObject
{
    public interface INonPlayableEntity
    {
        bool IsMoving { get; set; }

        short Effect { get; set; }

        short EffectDelay { get; set; }

        bool IsDisabled { get; set; }

    }
}
