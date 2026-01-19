//  __  _  __    __   ___ __  ___ ___
// |  \| |/__\ /' _/ / _//__\| _ \ __|
// | | ' | \/ |`._`.| \_| \/ | v / _|
// |_|\__|\__/ |___/ \__/\__/|_|_\___|
//

using Microsoft.Extensions.Options;
using NosCore.Core.Configuration;
using NosCore.Core.I18N;
using NosCore.Data.Enumerations;
using NosCore.Data.Enumerations.I18N;
using NosCore.GameObject.ComponentEntities.Extensions;
using NosCore.GameObject.Infastructure;
using NosCore.GameObject.Networking.ClientSession;
using NosCore.Networking;
using NosCore.Packets.ClientPackets.Inventory;
using NosCore.Packets.Enumerations;
using NosCore.Packets.ServerPackets.Chats;
using NosCore.Packets.ServerPackets.UI;
using NosCore.Shared.Enumerations;
using System.Threading.Tasks;


//TODO stop using obsolete
#pragma warning disable 618

namespace NosCore.PacketHandlers.Inventory
{
    public class PutPacketHandler(IOptions<WorldConfiguration> worldConfiguration,
            IGameLanguageLocalizer gameLanguageLocalizer)
        : PacketHandler<PutPacket>, IWorldPacketHandler
    {
        public override async Task ExecuteAsync(PutPacket putPacket, ClientSession clientSession)
        {
            var invitem =
                clientSession.Character.InventoryService.LoadBySlotAndType(putPacket.Slot,
                    (NoscorePocketType)putPacket.PocketType);
            if (invitem?.ItemInstance?.Item?.IsDroppable ?? false)
            {
                if ((putPacket.Amount > 0) && (putPacket.Amount <= worldConfiguration.Value.MaxItemAmount))
                {
                    if (clientSession.Character.MapInstance.MapItems.Count < 200)
                    {
                        var droppedItem =
                            clientSession.Character.MapInstance.PutItem(putPacket.Amount, invitem.ItemInstance,
                                clientSession);
                        if (droppedItem == null)
                        {
                            await clientSession.SendPacketAsync(new SayiPacket
                            {
                                VisualType = VisualType.Player,
                                VisualId = clientSession.Character.CharacterId,
                                Type = SayColorType.Yellow,
                                Message = Game18NConstString.CantDropItem
                            });
                            return;
                        }

                        invitem = clientSession.Character.InventoryService.LoadBySlotAndType(putPacket.Slot,
                            (NoscorePocketType)putPacket.PocketType);
                        await clientSession.SendPacketAsync(invitem.GeneratePocketChange(putPacket.PocketType, putPacket.Slot));
                        await clientSession.Character.MapInstance.SendPacketAsync(droppedItem.GenerateDrop());
                    }
                    else
                    {
                        await clientSession.SendPacketAsync(new MsgPacket
                        {
                            Type = MessageType.Default,
                            Message = gameLanguageLocalizer[LanguageKey.DROP_MAP_FULL, clientSession.Account.Language],
                        });
                    }
                }
                else
                {
                    await clientSession.SendPacketAsync(new MsgPacket
                    {
                        Type = MessageType.Default,
                        Message = gameLanguageLocalizer[LanguageKey.BAD_DROP_AMOUNT, clientSession.Account.Language],
                    });
                }
            }
            else
            {
                await clientSession.SendPacketAsync(new SayiPacket
                {
                    VisualType = VisualType.Player,
                    VisualId = clientSession.Character.CharacterId,
                    Type = SayColorType.Yellow,
                    Message = Game18NConstString.CantDropItem
                });
            }
        }
    }
}
