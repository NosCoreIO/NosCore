//  __  _  __    __   ___ __  ___ ___
// |  \| |/__\ /' _/ / _//__\| _ \ __|
// | | ' | \/ |`._`.| \_| \/ | v / _|
// |_|\__|\__/ |___/ \__/\__/|_|_\___|
//

namespace NosCore.Data.WebApi
{
    public class Character
    {
        public string? Name { get; set; }

        public long Id { get; set; }
        public bool FriendRequestBlocked { get; set; }
    }
}
