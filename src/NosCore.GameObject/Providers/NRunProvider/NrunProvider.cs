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

using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using NosCore.Packets.ClientPackets.Npcs;
using NosCore.GameObject.ComponentEntities.Interfaces;
using NosCore.GameObject.Networking.ClientSession;

namespace NosCore.GameObject.Providers.NRunProvider
{
    public class NrunProvider : INRunProvider
    {
        private readonly List<IEventHandler<Tuple<IAliveEntity, NrunPacket>, Tuple<IAliveEntity, NrunPacket>>>
            _handlers;

        public NrunProvider(
            IEnumerable<IEventHandler<Tuple<IAliveEntity, NrunPacket>, Tuple<IAliveEntity, NrunPacket>>> handlers)
        {
            _handlers = handlers.ToList();
        }

        public void NRunLaunch(ClientSession clientSession, Tuple<IAliveEntity, NrunPacket> data)
        {
            var handlersRequest = new Subject<RequestData<Tuple<IAliveEntity, NrunPacket>>>();
            _handlers.ForEach(handler =>
            {
                if (handler.Condition(data))
                {
                    handlersRequest.Subscribe(async o =>
                    {
                        await Observable.FromAsync(async () =>
                        {
                            await handler.ExecuteAsync(o).ConfigureAwait(false);
                        });
                    });
                }
            });
            handlersRequest.OnNext(new RequestData<Tuple<IAliveEntity, NrunPacket>>(clientSession, data));
        }
    }
}