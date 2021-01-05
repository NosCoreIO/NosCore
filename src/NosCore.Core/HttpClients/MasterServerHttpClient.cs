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
using System.Diagnostics.CodeAnalysis;
using System.Net.Http;
using System.Threading.Tasks;

namespace NosCore.Core.HttpClients
{
    public class MasterServerHttpClient
    {
        private readonly Channel _channel;
        private readonly IHttpClientFactory _httpClientFactory;

        protected MasterServerHttpClient(IHttpClientFactory httpClientFactory, Channel channel)
        {
            _httpClientFactory = httpClientFactory;
            _channel = channel;
        }

#pragma warning disable CA1056 // Uri properties should not be strings
        public virtual string ApiUrl { get; set; } = "";
#pragma warning restore CA1056 // Uri properties should not be strings
        public virtual bool RequireConnection { get; set; }

        protected HttpClient CreateClient()
        {
            return _httpClientFactory.CreateClient();
        }

        public virtual Task<HttpClient> ConnectAsync()
        {
            throw new NotImplementedException();
        }

        public Task<HttpClient?> ConnectAsync(int channelId)
        {
            throw new NotImplementedException();
        }

        protected Task<T> PostAsync<T>(object objectToPost)
        {
            throw new NotImplementedException();
        }


        protected Task<T> PatchAsync<T>(object id, object objectToPost)
        {
            throw new NotImplementedException();
        }

        protected Task<HttpResponseMessage> PostAsync(object objectToPost)
        {
            throw new NotImplementedException();
        }

        [return: MaybeNull]
        protected Task<T> GetAsync<T>()
        {
            throw new NotImplementedException();
        }

        [return: MaybeNull]
        protected Task<T> GetAsync<T>(object? id)
        {
            throw new NotImplementedException();
        }

        protected Task<HttpResponseMessage> DeleteAsync(object id)
        {
            throw new NotImplementedException();
        }
    }
}