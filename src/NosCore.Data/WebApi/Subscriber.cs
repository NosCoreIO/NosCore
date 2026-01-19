//  __  _  __    __   ___ __  ___ ___
// |  \| |/__\ /' _/ / _//__\| _ \ __|
// | | ' | \/ |`._`.| \_| \/ | v / _|
// |_|\__|\__/ |___/ \__/\__/|_|_\___|
//

using NosCore.Shared.Enumerations;

namespace NosCore.Data.WebApi
{
    public class Subscriber
    {
        public string? Name { get; set; }
        public RegionType Language { get; set; }
        public long ChannelId { get; set; }
        public Character? ConnectedCharacter { get; set; }
        public long Id { get; set; }
    }
}
