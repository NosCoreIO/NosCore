
using OpenNosCore.Database;
using System.ComponentModel.DataAnnotations;

namespace OpenNosCore.Data
{
    public class MateDTO : PlayerEntityDTO, IDatabaseObject
    {
        private long _mateId;

        [Key]
        public long MateId
        {
            get { return _mateId; }
            set
            {
                _mateId = value;
                VisualId = value;
            }
        }

        public long CharacterId { get; set; }

        public short Skin { get; set; }

        public void Initialize()
        {
            
        }
    }
}
