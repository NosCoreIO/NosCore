using NosCore.Shared.Interaction;
using System.ComponentModel.DataAnnotations;

namespace NosCore.Database.Entities
{
    public class ScriptedInstance
    {
        #region Properties

        public virtual Map Map { get; set; }

        public short MapId { get; set; }

        public short PositionX { get; set; }

        public short PositionY { get; set; }

        public string Label { get; set; }

        [MaxLength(int.MaxValue)]
        public string Script { get; set; }

        public short ScriptedInstanceId { get; set; }

        public ScriptedInstanceType Type { get; set; }

        #endregion
    }
}