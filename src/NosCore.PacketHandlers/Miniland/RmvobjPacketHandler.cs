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

using System.Threading.Tasks;
using NosCore.Packets.ClientPackets.Miniland;
using NosCore.Packets.Enumerations;
using NosCore.Packets.ServerPackets.Miniland;
using NosCore.Packets.ServerPackets.UI;
using NosCore.Core.I18N;
using NosCore.Data.Enumerations;
using NosCore.Data.Enumerations.I18N;
using NosCore.GameObject;
using NosCore.GameObject.Networking.ClientSession;
using NosCore.GameObject.Providers.MinilandProvider;

namespace NosCore.PacketHandlers.Miniland
{
    public class RmvobjPacketHandler : PacketHandler<RmvobjPacket>, IWorldPacketHandler
    {
        private readonly IMinilandProvider _minilandProvider;

        public RmvobjPacketHandler(IMinilandProvider minilandProvider)
        {
            _minilandProvider = minilandProvider;
        }

        public override async Task ExecuteAsync(RmvobjPacket rmvobjPacket, ClientSession clientSession)
        {
            var minilandobject =
                clientSession.Character.InventoryService.LoadBySlotAndType(rmvobjPacket.Slot, NoscorePocketType.Miniland);
            if (minilandobject == null)
            {
                return;
            }

            if (_minilandProvider.GetMiniland(clientSession.Character.CharacterId).State != MinilandState.Lock)
            {
                await clientSession.SendPacketAsync(new MsgPacket
                {
                    Message = GameLanguage.Instance.GetMessageFromKey(LanguageKey.MINILAND_NEED_LOCK,
                        clientSession.Account.Language)
                }).ConfigureAwait(false);
                return;
            }

            if (!clientSession.Character.MapInstance.MapDesignObjects.ContainsKey(minilandobject.Id))
            {
                return;
            }

            var minilandObject = clientSession.Character.MapInstance.MapDesignObjects[minilandobject.Id];
            clientSession.Character.MapInstance.MapDesignObjects.TryRemove(minilandobject.Id, out _);
            await clientSession.SendPacketAsync(minilandObject.GenerateEffect(true)).ConfigureAwait(false);
            await clientSession.SendPacketAsync(new MinilandPointPacket
                {MinilandPoint = minilandobject.ItemInstance!.Item!.MinilandObjectPoint, Unknown = 100}).ConfigureAwait(false);
            await  clientSession.SendPacketAsync(minilandObject.GenerateMapDesignObject(true)).ConfigureAwait(false);
        }
    }
}