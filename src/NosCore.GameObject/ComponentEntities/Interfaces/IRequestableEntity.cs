//  __  _  __    __   ___ __  ___ ___
// |  \| |/__\ /' _/ / _//__\| _ \ __|
// | | ' | \/ |`._`.| \_| \/ | v / _|
// |_|\__|\__/ |___/ \__/\__/|_|_\___|
//

using NosCore.GameObject.Networking.ClientSession;
using System;
using System.Collections.Generic;
using System.Reactive.Subjects;
using System.Threading.Tasks;

namespace NosCore.GameObject.ComponentEntities.Interfaces
{
    public interface IRequestableEntity<T>
    {
        List<Task> HandlerTasks { get; set; }

        Dictionary<Type, Subject<RequestData<T>>> Requests { get; set; }
    }

    public interface IRequestableEntity
    {
        Dictionary<Type, Subject<RequestData>> Requests { get; set; }
    }
}
