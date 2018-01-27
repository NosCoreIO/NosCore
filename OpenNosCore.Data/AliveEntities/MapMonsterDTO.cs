
using OpenNosCore.Database;
using System.ComponentModel.DataAnnotations;

namespace OpenNosCore.Data
{
    public class MapMonster : NonPlayerEntityDTO, IDatabaseObject
    {
        private long _mapMonsterId;

        [Key]
        public long MapMonsterId
        {
            get { return _mapMonsterId; }
            set
            {
                _mapMonsterId = value;
                VisualId = value;
            }
        }

        public void Initialize()
        {
            
        }
    }
}
