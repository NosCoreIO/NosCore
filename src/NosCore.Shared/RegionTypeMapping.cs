using NosCore.Shared.Enumerations;

namespace NosCore.Shared
{
    public class RegionTypeMapping
    {
        public int SessionId { get; set; }
        public RegionType RegionType { get; set; }

        public RegionTypeMapping(int sessionId, RegionType regionType)
        {
            SessionId = sessionId;
            RegionType = regionType;
        }
    }
}
