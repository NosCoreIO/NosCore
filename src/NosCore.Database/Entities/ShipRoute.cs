using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using NosCore.Database.Entities.Base;
using NosCore.Packets.Enumerations;

namespace NosCore.Database.Entities
{
    public class ShipRoute : IStaticEntity
    {
        [Key]
        public short ShipRouteId { get; set; }
        [Column(TypeName = "smallint")]
        public NrunRunnerType Runner { get; set; }
        [Column(TypeName = "smallint")]
        public NrunRunnerType CancelRunner { get; set; }
        public short SourceMapId { get; set; }
        public short BoardingNpcVNum { get; set; }
        public short BoardingDialog { get; set; }
        public short ShipMapId { get; set; }
        public short ShipX { get; set; }
        public short ShipY { get; set; }
        public short DestinationMapId { get; set; }
        public short DestinationX { get; set; }
        public short DestinationY { get; set; }
        public short ReturnX { get; set; }
        public short ReturnY { get; set; }
        public short CaptainVNum { get; set; }
        public short CaptainDialog { get; set; }
        public short CaptainX { get; set; }
        public short CaptainY { get; set; }
        public FactionType? RequiredFaction { get; set; }
        public int DepartureIntervalSeconds { get; set; }
        public long Fare { get; set; }
    }
}
