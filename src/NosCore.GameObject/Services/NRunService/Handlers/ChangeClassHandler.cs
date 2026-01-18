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

using NosCore.Data.Enumerations;
using NosCore.Data.Enumerations.I18N;
using NosCore.GameObject.Networking;
using NosCore.Packets.Enumerations;
using NosCore.Packets.ServerPackets.Chats;
using NosCore.Packets.ServerPackets.UI;
using NosCore.Shared.Enumerations;
using System.Linq;
using System.Threading.Tasks;
using NosCore.Shared.I18N;
using Serilog;

namespace NosCore.GameObject.Services.NRunService.Handlers
{
    public class ChangeClassEventHandler(ILogger logger, ILogLanguageLocalizer<LogLanguageKey> languageLocalizer)
        : INrunEventHandler
    {
        public bool Condition(NrunData item)
        {
            return (item.Packet.Runner == NrunRunnerType.ChangeClass) &&
                (item.Packet.Type > 0) && (item.Packet.Type < 4) && (item.Entity != null);
        }

        public async Task ExecuteAsync(RequestData<NrunData> requestData)
        {
            var player = requestData.ClientSession.Player;

            if (player.Class != CharacterClassType.Adventurer)
            {
                return;
            }

            if (!player.Group!.IsEmpty)
            {
                await requestData.ClientSession.SendPacketAsync(new SayiPacket
                {
                    VisualType = VisualType.Player,
                    VisualId = player.CharacterId,
                    Type = SayColorType.Red,
                    Message = Game18NConstString.CantUseInGroup
                }).ConfigureAwait(false);
                return;
            }

            var level = player.Level;
            var jobLevel = player.JobLevel;
            if (level < 15 || jobLevel < 20)
            {
                await requestData.ClientSession.SendPacketAsync(new MsgiPacket
                {
                    Type = MessageType.Default,
                    Message = Game18NConstString.CanNotChangeJobAtThisLevel
                }).ConfigureAwait(false);

                await requestData.ClientSession.SendPacketAsync(new SayiPacket
                {
                    VisualType = VisualType.Player,
                    VisualId = player.CharacterId,
                    Type = SayColorType.Yellow,
                    Message = Game18NConstString.CanNotChangeJobAtThisJobLevel
                }).ConfigureAwait(false);
                return;
            }

            var hasEquipment = player.InventoryService.Any(i => i.Value.Type == NoscorePocketType.Wear);
            if (hasEquipment)
            {
                await requestData.ClientSession.SendPacketAsync(new SayiPacket
                {
                    VisualType = VisualType.Player,
                    VisualId = player.CharacterId,
                    Type = SayColorType.Yellow,
                    Message = Game18NConstString.RemoveEquipment
                }).ConfigureAwait(false);
                return;
            }

            var classType = (CharacterClassType)(requestData.Data.Packet.Type ?? 0);
            if (player.Class == classType)
            {
                logger.Error(languageLocalizer[LogLanguageKey.CANT_CHANGE_SAME_CLASS]);
                return;
            }

            player.SetClass(classType);
            player.SetJobLevel(1);
            player.SetJobLevelXp(0);
        }
    }
}