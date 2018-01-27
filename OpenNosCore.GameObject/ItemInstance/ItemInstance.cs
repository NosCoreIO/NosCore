using OpenNosCore.Data;
using OpenNosCore.Packets;

namespace OpenNos.GameObject
{
    public class ItemInstance  : VisualEntityDTO
    {

        public ItemInstance()
        {
            InItemSubPacket = new InItemSubPacket();
        }
        public ItemDTO Item { get; set; }

        public short Design { get; set; }
    }
}