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
using NosCore.Packets.ClientPackets.Miniland;
using NosCore.Packets.Enumerations;
using NosCore.Packets.ServerPackets.Miniland;
using NosCore.Packets.ServerPackets.UI;
using NosCore.Core.I18N;
using NosCore.Data;
using NosCore.Data.Enumerations.I18N;
using NosCore.Data.Enumerations.Items;
using NosCore.GameObject;
using NosCore.GameObject.Networking.ClientSession;
using NosCore.GameObject.Providers.MapInstanceProvider;
using NosCore.GameObject.Providers.MinilandProvider;

namespace NosCore.PacketHandlers.Miniland
{
    public class AddobjPacketHandler : PacketHandler<AddobjPacket>, IWorldPacketHandler
    {
        private readonly IMinilandProvider _minilandProvider;

        public AddobjPacketHandler(IMinilandProvider minilandProvider)
        {
            _minilandProvider = minilandProvider;
        }

        public override async Task Execute(AddobjPacket addobjPacket, ClientSession clientSession)
        {
            var minilandobject =
                clientSession.Character.InventoryService.LoadBySlotAndType(addobjPacket.Slot, NoscorePocketType.Miniland);
            if (minilandobject == null)
            {
                return;
            }

            if (clientSession.Character.MapInstance!.MapDesignObjects.ContainsKey(minilandobject.Id))
            {
                await clientSession.SendPacket(new MsgPacket
                {
                    Message = GameLanguage.Instance.GetMessageFromKey(LanguageKey.ALREADY_THIS_MINILANDOBJECT,
                        clientSession.Account.Language)
                }).ConfigureAwait(false);
                return;
            }

            if (_minilandProvider.GetMiniland(clientSession.Character.CharacterId).State != MinilandState.Lock)
            {
                await clientSession.SendPacket(new MsgPacket
                {
                    Message = GameLanguage.Instance.GetMessageFromKey(LanguageKey.MINILAND_NEED_LOCK,
                        clientSession.Account.Language)
                }).ConfigureAwait(false);
                return;
            }

            var minilandobj = new MapDesignObject
            {
                MinilandObjectId = Guid.NewGuid(),
                MapX = addobjPacket.PositionX,
                MapY = addobjPacket.PositionY,
                Level1BoxAmount = 0,
                Level2BoxAmount = 0,
                Level3BoxAmount = 0,
                Level4BoxAmount = 0,
                Level5BoxAmount = 0
            };


            if (minilandobject.ItemInstance?.Item?.ItemType == ItemType.House)
            {
                var min = clientSession.Character.MapInstance.MapDesignObjects
                    .FirstOrDefault(s => (s.Value.InventoryItemInstance?.ItemInstance?.Item?.ItemType == ItemType.House) &&
                        (s.Value.InventoryItemInstance.ItemInstance.Item.ItemSubType ==
                            minilandobject.ItemInstance.Item.ItemSubType)).Value;
                if (min != null)
                {
                    await clientSession.HandlePackets(new[] {new RmvobjPacket {Slot = min.InventoryItemInstance?.Slot ?? 0}}).ConfigureAwait(false);
                }
            }

            _minilandProvider.AddMinilandObject(minilandobj, clientSession.Character.CharacterId, minilandobject);

            await clientSession.SendPacket(minilandobj.GenerateEffect()).ConfigureAwait(false);
            await clientSession.SendPacket(new MinilandPointPacket
                {MinilandPoint = minilandobject.ItemInstance?.Item?.MinilandObjectPoint ?? 0, Unknown = 100}).ConfigureAwait(false);
            await clientSession.SendPacket(minilandobj.GenerateMapDesignObject()).ConfigureAwait(false);
        }
    }
}