//  __  _  __    __   ___ __  ___ ___
// |  \| |/__\ /' _/ / _//__\| _ \ __|
// | | ' | \/ |`._`.| \_| \/ | v / _|
// |_|\__|\__/ |___/ \__/\__/|_|_\___|
//

namespace NosCore.Data.Enumerations.Bazaar
{
    public enum BazaarOperationState : byte
    {
        Pending = 0,
        Settled = 1,
        Compensated = 2
    }
}
