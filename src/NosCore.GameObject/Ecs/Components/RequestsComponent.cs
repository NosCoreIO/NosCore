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

// What the entity answers when someone acts on it - a dialog opened, a shop browsed.
public record struct RequestsComponent(
    Dictionary<Type, Subject<RequestData>> Requests);
