//  __  _  __    __   ___ __  ___ ___
// |  \| |/__\ /' _/ / _//__\| _ \ __|
// | | ' | \/ |`._`.| \_| \/ | v / _|
// |_|\__|\__/ |___/ \__/\__/|_|_\___|
//

using NosCore.GameObject.Ecs;
using NosCore.GameObject.Networking.ClientSession;
using NosCore.Packets.ClientPackets.Drops;
using System;
using System.Collections.Generic;
using System.Reactive.Subjects;
using System.Threading.Tasks;

namespace NosCore.GameObject.Services.MapItemGenerationService
{
    public class MapItemRequestContext
    {
        public Subject<RequestData<Tuple<MapItemComponentBundle, GetPacket>>> PickupSubject { get; } = new();
        public List<Task> HandlerTasks { get; } = new();
    }
}
