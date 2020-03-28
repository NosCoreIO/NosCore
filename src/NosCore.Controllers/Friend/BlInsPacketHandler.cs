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

using NosCore.Packets.ClientPackets.Relations;
using NosCore.Packets.ServerPackets.UI;
using NosCore.Core.I18N;
using NosCore.Data.Enumerations.I18N;
using NosCore.Data.WebApi;
using NosCore.GameObject;
using NosCore.GameObject.ComponentEntities.Extensions;
using NosCore.GameObject.HttpClients.BlacklistHttpClient;
using NosCore.GameObject.Networking.ClientSession;
using Serilog;

namespace NosCore.PacketHandlers.Friend
{
    public class BlInsPackettHandler : PacketHandler<BlInsPacket>, IWorldPacketHandler
    {
        private static readonly ILogger _logger = Logger.GetLoggerConfiguration().CreateLogger();
        private readonly IBlacklistHttpClient _blacklistHttpClient;

        public BlInsPackettHandler(IBlacklistHttpClient blacklistHttpClient)
        {
            _blacklistHttpClient = blacklistHttpClient;
        }

        public override void Execute(BlInsPacket blinsPacket, ClientSession session)
        {
            var result = _blacklistHttpClient.AddToBlacklist(new BlacklistRequest
                {CharacterId = session.Character.CharacterId, BlInsPacket = blinsPacket});
            switch (result)
            {
                case LanguageKey.CANT_BLOCK_FRIEND:
                    session.SendPacket(new InfoPacket
                    {
                        Message = Language.Instance.GetMessageFromKey(LanguageKey.CANT_BLOCK_FRIEND,
                            session.Account.Language)
                    });
                    break;
                case LanguageKey.ALREADY_BLACKLISTED:
                    session.SendPacket(new InfoPacket
                    {
                        Message = Language.Instance.GetMessageFromKey(LanguageKey.ALREADY_BLACKLISTED,
                            session.Account.Language)
                    });
                    break;
                case LanguageKey.BLACKLIST_ADDED:
                    session.SendPacket(new InfoPacket
                    {
                        Message = Language.Instance.GetMessageFromKey(LanguageKey.BLACKLIST_ADDED,
                            session.Account.Language)
                    });
                    session.SendPacket(session.Character.GenerateBlinit(_blacklistHttpClient));
                    break;
                default:
                    _logger.Warning(LogLanguage.Instance.GetMessageFromKey(LogLanguageKey.FRIEND_REQUEST_DISCONNECTED));
                    break;
            }
        }
    }
}