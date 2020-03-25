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
using NosCore.Configuration;
using NosCore.Data.Enumerations;

namespace NosCore.Core
{
    [Serializable]
    public class Channel
    {
        private string _clientName = ""; 
        public WebApiConfiguration? MasterCommunication { get; set; }

        public string ClientName
        {
            get => string.IsNullOrEmpty(_clientName) ? ClientType.ToString() : _clientName;
            set => _clientName = value;
        }

        public ServerType ClientType { get; set; }

        public int Port { get; set; }

        public byte ServerGroup { get; set; }

        public int ChannelId { get; set; }

        public string Host { get; set; } = "";

        public int ConnectedAccountLimit { get; set; }

        public ServerConfiguration? WebApi { get; set; }

        public RegionType UserLanguage { get; set; }

        public string? Token { get; set; }
    }
}