using NosCore.Shared.Enumerations;

namespace NosCore.Data.WebApi
{
    public class ConnectedAccount
    {
        public string Name { get; set; }
        public RegionType Language { get; set; }
	    public int ChannelId { get; set; }
    }
}