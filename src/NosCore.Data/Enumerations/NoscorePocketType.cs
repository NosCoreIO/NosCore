//  __  _  __    __   ___ __  ___ ___
// |  \| |/__\ /' _/ / _//__\| _ \ __|
// | | ' | \/ |`._`.| \_| \/ | v / _|
// |_|\__|\__/ |___/ \__/\__/|_|_\___|
//

using NosCore.Packets.Enumerations;

namespace NosCore.Data.Enumerations
{
    public enum NoscorePocketType : byte
    {
        Equipment = PocketType.Equipment,
        Main = PocketType.Main,
        Etc = PocketType.Etc,
        Miniland = PocketType.Miniland,
        Specialist = PocketType.Specialist,
        Costume = PocketType.Costume,
        Wear = 8
    }
}
