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

using NosCore.Core.HttpClients.ConnectedAccountHttpClients;
using NosCore.Core.I18N;
using NosCore.Data.CommandPackets;
using NosCore.Data.Enumerations.I18N;
using NosCore.GameObject;
using NosCore.GameObject.ComponentEntities.Extensions;
using NosCore.GameObject.HttpClients.MailHttpClient;
using NosCore.GameObject.Networking.ClientSession;
using NosCore.Packets.Enumerations;
using NosCore.Packets.ServerPackets.UI;
using System.Threading.Tasks;

namespace NosCore.PacketHandlers.Command
{
    public class GiftPacketHandler : PacketHandler<GiftPacket>, IWorldPacketHandler
    {
        private readonly IConnectedAccountHttpClient _connectedAccountHttpClient;
        private readonly IMailHttpClient _mailHttpClient;

        public GiftPacketHandler(IConnectedAccountHttpClient connectedAccountHttpClient, IMailHttpClient mailHttpClient)
        {
            _connectedAccountHttpClient = connectedAccountHttpClient;
            _mailHttpClient = mailHttpClient;
        }

        public override async Task ExecuteAsync(GiftPacket giftPacket, ClientSession session)
        {
            var receiver =
                await _connectedAccountHttpClient.GetCharacterAsync(null, giftPacket.CharacterName ?? session.Character.Name).ConfigureAwait(false);

            if (receiver.Item2 == null)
            {
                await session.SendPacketAsync(new InfoiPacket
                {
                    Message = Game18NConstString.UnknownCharacter
                }).ConfigureAwait(false);
                return;
            }

            await _mailHttpClient.SendGiftAsync(session.Character!, receiver.Item2.ConnectedCharacter!.Id, giftPacket.VNum,
                giftPacket.Amount, giftPacket.Rare, giftPacket.Upgrade, false).ConfigureAwait(false);
            await session.SendPacketAsync(session.Character.GenerateSay(GameLanguage.Instance.GetMessageFromKey(
                LanguageKey.GIFT_SENT,
                session.Account.Language), SayColorType.Yellow)).ConfigureAwait(false);
        }
    }
}