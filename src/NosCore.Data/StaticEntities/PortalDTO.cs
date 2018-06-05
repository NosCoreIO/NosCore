using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Text;

namespace NosCore.Data.StaticEntities
{
    public class PortalDTO : IDTO
    {
        public short DestinationMapId { get; set; }

        public short DestinationX { get; set; }

        public short DestinationY { get; set; }

        public bool IsDisabled { get; set; }

        public int PortalId { get; set; }

        public short SourceMapId { get; set; }

        public short SourceX { get; set; }

        public short SourceY { get; set; }

        public short Type { get; set; }
    }
}
