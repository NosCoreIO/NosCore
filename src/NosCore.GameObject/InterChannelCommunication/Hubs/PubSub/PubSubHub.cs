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
using System.Threading.Tasks;
using Microsoft.AspNetCore.SignalR;
using NosCore.Data.WebApi;
using NosCore.GameObject.InterChannelCommunication.Messages;

namespace NosCore.GameObject.InterChannelCommunication.Hubs.PubSub
{
    public class PubSubHub(MasterClientList masterClientList)
        : Hub, IPubSubHub
    {
        public async Task<bool> SendMessageAsync(IMessage message)
        {
            await Clients.Others.SendAsync("ReceiveMessage", message);
            return true;
        }

        public async Task<bool> SendMessagesAsync(List<IMessage> messages)
        {
            foreach (var message in messages)
            {
                await Clients.Others.SendAsync("ReceiveMessage", message);
            }
            return true;
        }

        public Task<List<Subscriber>> GetSubscribersAsync()
        {
            return Task.FromResult(masterClientList.ConnectedAccounts.SelectMany(x => x.Value.Values).ToList());
        }

        public Task<bool> SubscribeAsync(Subscriber subscriber)
        {
            if (!masterClientList.ConnectedAccounts.ContainsKey(Context.UserIdentifier ?? throw new InvalidOperationException())
                || !masterClientList.Channels.ContainsKey(Context.UserIdentifier ?? throw new InvalidOperationException()))
            {
                return Task.FromResult(false);
            }
            subscriber.ChannelId = masterClientList.Channels[Context.UserIdentifier].Id;
            masterClientList.ConnectedAccounts[Context.UserIdentifier].AddOrUpdate(subscriber.Id, subscriber, (_, _) => subscriber);
            return Task.FromResult(true);
        }

        public Task<bool> UnsubscribeAsync(long id)
        {
            if (!masterClientList.ConnectedAccounts.ContainsKey(Context.UserIdentifier ?? throw new InvalidOperationException()))
            {
                return Task.FromResult(false);
            }
            return Task.FromResult(masterClientList.ConnectedAccounts[Context.UserIdentifier].TryRemove(id, out _));
        }
    }
}