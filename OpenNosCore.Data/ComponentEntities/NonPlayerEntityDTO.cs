using OpenNosCore.Packets;

namespace OpenNosCore.Data
{
    public class NonPlayerEntityDTO : AliveEntity
    {
        public NonPlayerEntityDTO() : base()
        {
            InOwnableSubPacket = new InOwnableSubPacket();
            InNonPlayerSubPacket = new InNonPlayerSubPacket();
        }
        public bool IsMoving { get; set; }

        public short Effect { get; set; }

        public short EffectDelay { get; set; }

        public bool IsDisabled { get; set; }

    }
}
