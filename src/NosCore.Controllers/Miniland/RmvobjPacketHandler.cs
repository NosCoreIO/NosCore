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
using NosCore.Data;
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

        public override Task Execute(RmvobjPacket rmvobjPacket, ClientSession clientSession)
        {
            var minilandobject =
                clientSession.Character.InventoryService.LoadBySlotAndType(rmvobjPacket.Slot, NoscorePocketType.Miniland);
            if (minilandobject == null)
            {
                return Task.CompletedTask;
            }

            if (_minilandProvider.GetMiniland(clientSession.Character.CharacterId).State != MinilandState.Lock)
            {
                clientSession.SendPacket(new MsgPacket
                {
                    Message = Language.Instance.GetMessageFromKey(LanguageKey.MINILAND_NEED_LOCK,
                        clientSession.Account.Language)
                });
                return Task.CompletedTask;
            }

            if (!clientSession.Character.MapInstance!.MapDesignObjects.ContainsKey(minilandobject.Id))
            {
                return Task.CompletedTask;
            }

            var minilandObject = clientSession.Character.MapInstance.MapDesignObjects[minilandobject.Id];
            clientSession.Character.MapInstance.MapDesignObjects.TryRemove(minilandobject.Id, out _);
            clientSession.SendPacket(minilandObject.GenerateEffect(true));
            clientSession.SendPacket(new MinilandPointPacket
                {MinilandPoint = minilandobject.ItemInstance!.Item!.MinilandObjectPoint, Unknown = 100});
            clientSession.SendPacket(minilandObject.GenerateMapDesignObject(true));
            return Task.CompletedTask;
        }
    }
}