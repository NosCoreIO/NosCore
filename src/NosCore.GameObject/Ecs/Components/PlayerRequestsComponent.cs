//  __  _  __    __   ___ __  ___ ___
// |  \| |/__\ /' _/ / _//__\| _ \ __|
// | | ' | \/ |`._`.| \_| \/ | v / _|
// |_|\__|\__/ |___/ \__/\__/|_|_\___|
//

using NosCore.GameObject.Networking.ClientSession;
using System;
using System.Collections.Generic;
using System.Reactive.Subjects;

namespace NosCore.GameObject.Ecs.Components;

public record struct PlayerRequestsComponent(
    Dictionary<Type, Subject<RequestData>> Requests);
