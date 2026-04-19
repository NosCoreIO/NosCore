//  __  _  __    __   ___ __  ___ ___
// |  \| |/__\ /' _/ / _//__\| _ \ __|
// | | ' | \/ |`._`.| \_| \/ | v / _|
// |_|\__|\__/ |___/ \__/\__/|_|_\___|
//

namespace NosCore.GameObject.Networking.ClientSession
{
    // Marker type used as a dictionary key for the NPC-dialog request Subject on
    // IRequestableEntity. RequestNpcPacketHandler fires it; NonPlayableEntityExtension
    // subscribes it to ShowDialogAsync. Decoupled from the NRun handler hierarchy so that
    // INrunEventHandler can be deleted independently.
    public sealed class NpcDialogRequestSubject;
}
