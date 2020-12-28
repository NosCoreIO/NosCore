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

using NosCore.Core.I18N;
using NosCore.Data.Enumerations.I18N;
using NosCore.GameObject.ComponentEntities.Interfaces;
using NosCore.GameObject.Networking.ClientSession;
using NosCore.Packets.ClientPackets.Npcs;
using NosCore.Packets.Enumerations;
using NosCore.Packets.ServerPackets.UI;
using NosCore.Shared.Enumerations;
using System;
using System.Threading.Tasks;

//TODO stop using obsolete
#pragma warning disable 618

namespace NosCore.GameObject.Services.NRunService.Handlers
{
    public class ChangeClassEventHandler : INrunEventHandler
    {
        public bool Condition(Tuple<IAliveEntity, NrunPacket> item)
        {
            return (item.Item2.Runner == NrunRunnerType.ChangeClass) &&
                (item.Item2.Type > 0) && (item.Item2.Type < 4) && (item.Item1 != null);
        }

        public async Task ExecuteAsync(RequestData<Tuple<IAliveEntity, NrunPacket>> requestData)
        {
            if (requestData.ClientSession.Character.Class != (byte)CharacterClassType.Adventurer)
            {
                await requestData.ClientSession.SendPacketAsync(new MsgPacket
                {
                    Message = GameLanguage.Instance.GetMessageFromKey(LanguageKey.NOT_ADVENTURER,
                        requestData.ClientSession.Account.Language),
                    Type = MessageType.White
                }).ConfigureAwait(false);
                return;
            }

            if ((requestData.ClientSession.Character.Level < 15) || (requestData.ClientSession.Character.JobLevel < 20))
            {
                await requestData.ClientSession.SendPacketAsync(new MsgiPacket
                {
                    Message = Game18NConstString.CanNotChangeJobAtThisLevel,
                    Type = MessageType.White
                }).ConfigureAwait(false);
                return;
            }

            await requestData.ClientSession.Character.ChangeClassAsync((CharacterClassType)(requestData.Data.Item2.Type ?? 0)).ConfigureAwait(false);
        }
    }
}