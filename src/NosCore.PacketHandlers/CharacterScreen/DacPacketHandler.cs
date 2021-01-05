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

using NosCore.Core.HttpClients.AuthHttpClients;
using NosCore.Core.I18N;
using NosCore.Dao.Interfaces;
using NosCore.Data.Dto;
using NosCore.Data.Enumerations.I18N;
using NosCore.GameObject;
using NosCore.GameObject.Networking.ClientSession;
using NosCore.Packets.ClientPackets.CharacterSelectionScreen;
using NosCore.Packets.ClientPackets.Infrastructure;
using Serilog;
using System.Threading.Tasks;
using NosCore.GameObject.HubClients.ChannelHubClient;

namespace NosCore.PacketHandlers.CharacterScreen
{
    public class DacPacketHandler : PacketHandler<DacPacket>, IWorldPacketHandler
    {
        private readonly IDao<AccountDto, long> _accountDao;
        private readonly IAuthHttpClient _authHttpClient;
        private readonly ILogger _logger;
        private readonly IChannelHubClient _channelHubClient;

        public DacPacketHandler(IDao<AccountDto, long> accountDao,
            ILogger logger, IAuthHttpClient authHttpClient,
            IChannelHubClient channelHubClient)
        {
            _accountDao = accountDao;
            _logger = logger;
            _authHttpClient = authHttpClient;
            _channelHubClient = channelHubClient;
        }

        public override async Task ExecuteAsync(DacPacket packet, ClientSession clientSession)
        {
            await EntryPointPacketHandler.VerifyConnectionAsync(clientSession, _logger, _authHttpClient,
                 _accountDao, true, packet.AccountName, "thisisgfmode", -1, _channelHubClient);
            if (clientSession.Account == null!)
            {
                return;
            }
            await clientSession.HandlePacketsAsync(new[] { new SelectPacket { Slot = packet.Slot } })
                .ConfigureAwait(false);

            _logger.Information(LogLanguage.Instance.GetMessageFromKey(LogLanguageKey.ACCOUNT_ARRIVED),
                clientSession.Account!.Name);
        }
    }
}