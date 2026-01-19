//  __  _  __    __   ___ __  ___ ___
// |  \| |/__\ /' _/ / _//__\| _ \ __|
// | | ' | \/ |`._`.| \_| \/ | v / _|
// |_|\__|\__/ |___/ \__/\__/|_|_\___|
//

using NosCore.GameObject.Infastructure;
using NosCore.GameObject.Networking.ClientSession;
using NosCore.Packets.ClientPackets.UI;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive.Linq;
using System.Reactive.Subjects;

namespace NosCore.GameObject.Services.GuriRunnerService
{
    public class GuriRunnerService(IEnumerable<IEventHandler<GuriPacket, GuriPacket>> handlers)
        : IGuriRunnerService
    {
        private readonly List<IEventHandler<GuriPacket, GuriPacket>> _handlers = Enumerable.ToList<IEventHandler<GuriPacket, GuriPacket>>(handlers);

        public void GuriLaunch(ClientSession clientSession, GuriPacket data)
        {
            var handlersRequest = new Subject<RequestData<GuriPacket>>();
            _handlers.ForEach(handler =>
            {
                if (handler.Condition(data))
                {
                    handlersRequest.Select(handler.ExecuteAsync).Subscribe();
                }
            });
            handlersRequest.OnNext(new RequestData<GuriPacket>(clientSession, data));
        }
    }
}
