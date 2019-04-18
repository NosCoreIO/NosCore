using System;
using System.Collections.Generic;
using System.Text;
using ChickenAPI.Packets.ClientPackets.Inventory;
using ChickenAPI.Packets.Enumerations;
using ChickenAPI.Packets.ServerPackets.UI;
using NosCore.Core.I18N;
using NosCore.Data.Enumerations.I18N;
using NosCore.GameObject;
using NosCore.GameObject.ComponentEntities.Extensions;
using NosCore.GameObject.Networking.ClientSession;
using Serilog;

namespace NosCore.PacketHandlers.Inventory
{
    public class BiPacketHandler : PacketHandler<BiPacket>, IWorldPacketHandler
    {
        private readonly ILogger _logger;

        public BiPacketHandler(ILogger logger)
        {
            _logger = logger;
        }

        public override void Execute(BiPacket bIPacket, ClientSession session)
        {
            switch (bIPacket.Option)
            {
                case null:
                    session.SendPacket(
                        new DlgPacket
                        {
                            YesPacket = new BiPacket
                            {
                                PocketType = bIPacket.PocketType,
                                Slot = bIPacket.Slot,
                                Option = RequestDeletionType.Requested
                            },
                            NoPacket = new BiPacket
                            {
                                PocketType = bIPacket.PocketType,
                                Slot = bIPacket.Slot,
                                Option = RequestDeletionType.Declined
                            },
                            Question = Language.Instance.GetMessageFromKey(LanguageKey.ASK_TO_DELETE,
                                session.Account.Language)
                        });
                    break;

                case RequestDeletionType.Requested:
                    session.SendPacket(
                        new DlgPacket
                        {
                            YesPacket = new BiPacket
                            {
                                PocketType = bIPacket.PocketType,
                                Slot = bIPacket.Slot,
                                Option = RequestDeletionType.Confirmed
                            },
                            NoPacket = new BiPacket
                            {
                                PocketType = bIPacket.PocketType,
                                Slot = bIPacket.Slot,
                                Option = RequestDeletionType.Declined
                            },
                            Question = Language.Instance.GetMessageFromKey(LanguageKey.SURE_TO_DELETE,
                                session.Account.Language)
                        });
                    break;

                case RequestDeletionType.Confirmed:
                    if (session.Character.InExchangeOrShop)
                    {
                        _logger.Error(LogLanguage.Instance.GetMessageFromKey(LogLanguageKey.CANT_MOVE_ITEM_IN_SHOP));
                        return;
                    }

                    var item = session.Character.Inventory.DeleteFromTypeAndSlot(bIPacket.PocketType, bIPacket.Slot);
                    session.SendPacket(item.GeneratePocketChange(bIPacket.PocketType, bIPacket.Slot));
                    break;
                default:
                    return;
            }
        }
    }
}
