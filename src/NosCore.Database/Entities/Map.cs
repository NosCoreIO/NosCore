using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace NosCore.Database.Entities
{
    public class Map
    {
        #region Instantiation

        public Map()
        {
            Character = new HashSet<Character>();
            MapMonster = new HashSet<MapMonster>();
            MapNpc = new HashSet<MapNpc>();
            Portal = new HashSet<Portal>();
            Portal1 = new HashSet<Portal>();
            ScriptedInstance = new HashSet<ScriptedInstance>();
            Teleporter = new HashSet<Teleporter>();
            MapTypeMap = new HashSet<MapTypeMap>();
            Respawn = new HashSet<Respawn>();
            RespawnMapType = new HashSet<RespawnMapType>();
        }

        #endregion

        #region Properties

        public virtual ICollection<Character> Character { get; set; }

        public byte[] Data { get; set; }

        [DatabaseGenerated(DatabaseGeneratedOption.None)]
        public short MapId { get; set; }

        public virtual ICollection<MapMonster> MapMonster { get; set; }

        public virtual ICollection<MapNpc> MapNpc { get; set; }

        public virtual ICollection<MapTypeMap> MapTypeMap { get; set; }

        public int Music { get; set; }

        [MaxLength(255)]
        public string Name { get; set; }

        public virtual ICollection<Portal> Portal { get; set; }

        public virtual ICollection<Portal> Portal1 { get; set; }

        public virtual ICollection<Respawn> Respawn { get; set; }

        public virtual ICollection<RespawnMapType> RespawnMapType { get; set; }

        public virtual ICollection<ScriptedInstance> ScriptedInstance { get; set; }

        public bool ShopAllowed { get; set; }

        public virtual ICollection<Teleporter> Teleporter { get; set; }

        #endregion
    }
}