﻿//  __  _  __    __   ___ __  ___ ___
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
using System.Reactive.Subjects;
using ChickenAPI.Packets.ClientPackets.Player;
using NosCore.GameObject.Networking.ClientSession;

namespace NosCore.GameObject.Providers.UpgradeProvider
{
    public class UpgradeProvider : IUpgradeProvider
    {
        private readonly List<IEventHandler<UpgradeProperties, UpgradeProperties>> _handlers;

        public UpgradeProvider(IEnumerable<IEventHandler<UpgradeProperties, UpgradeProperties>> handlers)
        {
            _handlers = handlers.ToList();
        }

        public void Upgrade(ClientSession clientSession, UpgradeProperties data)
        {
            using var handlersRequest = new Subject<RequestData<UpgradeProperties>>();
            _handlers.ForEach(handler =>
            {
                if (handler.Condition(data))
                {
                    handlersRequest.Subscribe(handler.Execute);
                }
            });
            handlersRequest.OnNext(new RequestData<UpgradeProperties>(clientSession, data));
        }
    }
}
