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

using NosCore.Data.Enumerations.I18N;
using NosCore.Data.WebApi;
using NosCore.GameObject;
using NosCore.GameObject.Ecs.Systems;
using NosCore.Packets.ClientPackets.Relations;
using NosCore.Packets.Enumerations;
using NosCore.Packets.ServerPackets.UI;
using NosCore.Shared.I18N;
using Serilog;
using System.Threading.Tasks;
using NosCore.GameObject.InterChannelCommunication.Hubs.BlacklistHub;
using NosCore.GameObject.Networking;

namespace NosCore.PacketHandlers.Friend
{
    public class BlInsPackettHandler(IBlacklistHub blacklistHttpClient, ILogger logger,
            ILogLanguageLocalizer<LogLanguageKey> logLanguage, ICharacterPacketSystem characterPacketSystem)
        : PacketHandler<BlInsPacket>, IWorldPacketHandler
    {
        public override async Task ExecuteAsync(BlInsPacket blinsPacket, ClientSession session)
        {
            var result = await blacklistHttpClient.AddBlacklistAsync(new BlacklistRequest
            { CharacterId = session.Player.CharacterId, BlInsPacket = blinsPacket }).ConfigureAwait(false);
            switch (result)
            {
                case LanguageKey.CANT_BLOCK_FRIEND:
                    await session.SendPacketAsync(new InfoiPacket
                    {
                        Message = Game18NConstString.CannotBlackListFriend
                    }).ConfigureAwait(false);
                    break;
                case LanguageKey.ALREADY_BLACKLISTED:
                    await session.SendPacketAsync(new InfoiPacket
                    {
                        Message = Game18NConstString.AlreadyBlacklisted
                    }).ConfigureAwait(false);
                    break;
                case LanguageKey.BLACKLIST_ADDED:
                    await session.SendPacketAsync(new InfoiPacket
                    {
                        Message = Game18NConstString.CharacterBlacklisted
                    }).ConfigureAwait(false);
                    await session.SendPacketAsync(await characterPacketSystem.GenerateBlinitAsync(session.Player, blacklistHttpClient).ConfigureAwait(false)).ConfigureAwait(false);
                    break;
                default:
                    logger.Warning(logLanguage[LogLanguageKey.FRIEND_REQUEST_DISCONNECTED]);
                    break;
            }
        }
    }
}