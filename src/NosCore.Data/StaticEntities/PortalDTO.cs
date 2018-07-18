using System.ComponentModel.DataAnnotations;
using NosCore.Shared.Enumerations.Map;

namespace NosCore.Data.StaticEntities
{
    public class PortalDTO : IDTO
    {
        public short DestinationMapId { get; set; }

        public short DestinationX { get; set; }

        public short DestinationY { get; set; }

        public bool IsDisabled { get; set; }

        [Key]
        public int PortalId { get; set; }

        public short SourceMapId { get; set; }

        public short SourceX { get; set; }

        public short SourceY { get; set; }

        public PortalType Type { get; set; }
    }
}