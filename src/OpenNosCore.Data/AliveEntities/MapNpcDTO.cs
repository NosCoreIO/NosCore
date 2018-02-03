using OpenNosCore.Database;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text;


namespace OpenNosCore.Data
{
    public class MapNpcDTO : NonPlayerEntityDTO, IDatabaseObject
    {  
        private long _mapNpcId;

        [Key]
        public long MapNpcId
        {
            get { return _mapNpcId; }
            set {
                _mapNpcId = value;
                VisualId = value;
            }
        }


        public short Dialog { get; set; }

        public virtual ICollection<RecipeDTO> Recipe { get; set; }

        public virtual ICollection<ShopDTO> Shop { get; set; }

        public virtual ICollection<TeleporterDTO> Teleporter { get; set; }

        public void Initialize()
        {

        }
    }
}
