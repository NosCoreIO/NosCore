//  __  _  __    __   ___ __  ___ ___
// |  \| |/__\ /' _/ / _//__\| _ \ __|
// | | ' | \/ |`._`.| \_| \/ | v / _|
// |_|\__|\__/ |___/ \__/\__/|_|_\___|
// 
// Copyright (C) 2019 - NosCore
// 
// NosCore is a free software: you can redistribute it and/or modify
// it under the terms of the GNU General Public License as published by
// the Free Software Foundation, either version 3 of the License, or any later version.
// 
// This program is distributed in the hope that it will be useful,
// but WITHOUT ANY WARRANTY; without even the implied warranty of
// MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
// GNU General Public License for more details.
// 
// You should have received a copy of the GNU General Public License
// along with this program.  If not, see <http://www.gnu.org/licenses/>.

using NosCore.GameObject.ComponentEntities.Interfaces;
using NosCore.GameObject.Networking.ClientSession;
using NosCore.Packets.ClientPackets.Npcs;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using System.Threading.Tasks;

namespace NosCore.GameObject.Services.NRunService
{
    public class NrunService : INrunService
    {
        private readonly List<IEventHandler<Tuple<IAliveEntity, NrunPacket>, Tuple<IAliveEntity, NrunPacket>>>
            _handlers;

        public NrunService(
            IEnumerable<IEventHandler<Tuple<IAliveEntity, NrunPacket>, Tuple<IAliveEntity, NrunPacket>>> handlers)
        {
            _handlers = handlers.ToList();
        }

        public Task NRunLaunchAsync(ClientSession clientSession, Tuple<IAliveEntity, NrunPacket> data)
        {
            var handlersRequest = new Subject<RequestData<Tuple<IAliveEntity, NrunPacket>>>();
            var taskList = new List<Task>();
            _handlers.ForEach(handler =>
            {
                if (handler.Condition(data))
                {
                    handlersRequest.Select(request =>
                    {
                        var task = handler.ExecuteAsync(request);
                        taskList.Add(task);
                        return task;
                    }).Subscribe();
                }
            });
            handlersRequest.OnNext(new RequestData<Tuple<IAliveEntity, NrunPacket>>(clientSession, data));
            return Task.WhenAll(taskList);
        }
    }
}