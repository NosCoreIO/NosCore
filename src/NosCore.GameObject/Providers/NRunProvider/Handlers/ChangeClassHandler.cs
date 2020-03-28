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
using System.Linq;
using System.Threading.Tasks;
using NosCore.Packets.ClientPackets.Npcs;
using NosCore.Packets.Enumerations;
using NosCore.Packets.ServerPackets.UI;
using NosCore.Core.I18N;
using NosCore.Data;
using NosCore.Data.Enumerations.I18N;
using NosCore.GameObject.ComponentEntities.Interfaces;
using NosCore.GameObject.Networking.ClientSession;

namespace NosCore.GameObject.Providers.NRunProvider.Handlers
{
    public class
        ChangeClassEventHandler : IEventHandler<Tuple<IAliveEntity, NrunPacket>, Tuple<IAliveEntity, NrunPacket>>
    {
        public bool Condition(Tuple<IAliveEntity, NrunPacket> item)
        {
            return (item.Item2.Runner == NrunRunnerType.ChangeClass) &&
                (item.Item2.Type > 0) && (item.Item2.Type < 4) && (item.Item1 != null);
        }

        public async Task Execute(RequestData<Tuple<IAliveEntity, NrunPacket>> requestData)
        {
            if (requestData.ClientSession.Character.Class != (byte) CharacterClassType.Adventurer)
            {
                await requestData.ClientSession.SendPacket(new MsgPacket
                {
                    Message = Language.Instance.GetMessageFromKey(LanguageKey.NOT_ADVENTURER,
                        requestData.ClientSession.Account.Language),
                    Type = MessageType.White
                });
                return;
            }

            if ((requestData.ClientSession.Character.Level < 15) || (requestData.ClientSession.Character.JobLevel < 20))
            {
                await requestData.ClientSession.SendPacket(new MsgPacket
                {
                    Message = Language.Instance.GetMessageFromKey(LanguageKey.TOO_LOW_LEVEL,
                        requestData.ClientSession.Account.Language),
                    Type = MessageType.White
                });
                return;
            }

            if (requestData.ClientSession.Character.InventoryService.Any(s => s.Value.Type == NoscorePocketType.Wear))
            {
                await requestData.ClientSession.SendPacket(new MsgPacket
                {
                    Message = Language.Instance.GetMessageFromKey(LanguageKey.EQ_NOT_EMPTY,
                        requestData.ClientSession.Account.Language),
                    Type = MessageType.White
                });
                return;
            }

            await requestData.ClientSession.Character.ChangeClass((CharacterClassType) requestData.Data.Item2.Type);
        }
    }
}