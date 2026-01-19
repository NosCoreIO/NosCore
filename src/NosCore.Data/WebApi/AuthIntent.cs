//  __  _  __    __   ___ __  ___ ___
// |  \| |/__\ /' _/ / _//__\| _ \ __|
// | | ' | \/ |`._`.| \_| \/ | v / _|
// |_|\__|\__/ |___/ \__/\__/|_|_\___|
//

namespace NosCore.Data.WebApi
{
    public class AuthIntent
    {
        public long SessionId { get; set; }
        public string AccountName { get; set; } = null!;
    }
}
