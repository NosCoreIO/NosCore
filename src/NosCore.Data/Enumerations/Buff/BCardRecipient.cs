//  __  _  __    __   ___ __  ___ ___
// |  \| |/__\ /' _/ / _//__\| _ \ __|
// | | ' | \/ |`._`.| \_| \/ | v / _|
// |_|\__|\__/ |___/ \__/\__/|_|_\___|
//

namespace NosCore.Data.Enumerations.Buff
{
    // Who a monster's own card lands on. Only monster cards carry one, and only Enemy is acted on so far.
    public enum BCardRecipient : byte
    {
        None = 0,
        // Whatever the monster hits.
        Enemy = 1,
        // The monster itself.
        Caster = 2
    }
}
