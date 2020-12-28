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

namespace NosCore.GameObject.Networking.ClientSession
{
    public class RequestData<T> : RequestData
    {
        public RequestData(T data)
        {
            Data = data;
        }

        public RequestData(ClientSession clientSession, T data) : base(clientSession)
        {
            Data = data;
        }

        public T Data { get; }
    }

    public class RequestData : IRequestData
    {
        public RequestData(ClientSession clientSession)
        {
            ClientSession = clientSession;
        }

        public RequestData()
        {
            ClientSession = null!;
        }

        public ClientSession ClientSession { get; }
    }

    public interface IRequestData
    {
    }
}