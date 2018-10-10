using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using JetBrains.Annotations;
using NosCore.Configuration;
using NosCore.Core.Extensions;
using NosCore.Core.Serializing;
using NosCore.Data.WebApi;
using NosCore.GameObject;
using NosCore.GameObject.ComponentEntities.Extensions;
using NosCore.GameObject.Networking;
using NosCore.GameObject.Services;
using NosCore.GameObject.Services.MapInstanceAccess;
using NosCore.GameObject.Services.ItemBuilder;
using NosCore.GameObject.Services.ItemBuilder.Item;
using NosCore.Packets.CommandPackets;
using NosCore.Packets.ServerPackets;
using NosCore.Shared.Enumerations;
using NosCore.Shared.Enumerations.Items;
using NosCore.Shared.Enumerations.Map;
using NosCore.Shared.I18N;

namespace NosCore.Controllers
{
    [UsedImplicitly]
    public class CommandPacketController : PacketController
    {
        private readonly WorldConfiguration _worldConfiguration;
        private readonly List<Item> _items;
        private readonly IItemBuilderService _itemBuilderService;
        private readonly MapInstanceAccessService _mapInstanceAccessService;

        public CommandPacketController(WorldConfiguration worldConfiguration, List<Item> items, IItemBuilderService itemBuilderService, MapInstanceAccessService mapInstanceAccessService)
        {
            _worldConfiguration = worldConfiguration;
            _items = items;
            _itemBuilderService = itemBuilderService;
            _mapInstanceAccessService = mapInstanceAccessService;
        }

        [UsedImplicitly]
        public CommandPacketController()
        {
        }

        [UsedImplicitly]
        public void Gold(GoldCommandPacket goldPacket)
        {
            if (goldPacket.Gold + Session.Character.Gold > _worldConfiguration.MaxGoldAmount)
            {
                Session.SendPacket(Session.Character.GenerateSay(goldPacket.Help(), SayColorType.Yellow));
                return;
            }
            Session.Character.Gold += goldPacket.Gold;
            Session.SendPacket(Session.Character.GenerateGold());
        }

        public void Shout(ShoutPacket shoutPacket)
        {
            var sayPacket = new SayPacket
            {
                VisualType = VisualType.Player,
                VisualId = 0,
                Type = SayColorType.Yellow,
                Message =
                    $"({Language.Instance.GetMessageFromKey(LanguageKey.ADMINISTRATOR, Session.Account.Language)}) {shoutPacket.Message}"
            };

            var msgPacket = new MsgPacket
            {
                Type = MessageType.Shout,
                Message = shoutPacket.Message
            };

            var sayPostedPacket = new PostedPacket
            {
                Packet = PacketFactory.Serialize(new[] { sayPacket }),
                SenderCharacter = new Data.WebApi.Character()
                {
                    Name = Session.Character.Name,
                    Id = Session.Character.CharacterId
                }
            };

            var msgPostedPacket = new PostedPacket
            {
                Packet = PacketFactory.Serialize(new[] { msgPacket })
            };

            ServerManager.Instance.BroadcastPackets(new List<PostedPacket>(new[] { sayPostedPacket, msgPostedPacket }));
        }

        [UsedImplicitly]
        public void CreateItem(CreateItemPacket createItemPacket)
        {
            if (createItemPacket != null)
            {
                var vnum = createItemPacket.VNum;
                sbyte rare = 0;
                const short boxEffect = 999;
                byte upgrade = 0, design = 0;
                short amount = 1;
                if (vnum == 1046)
                {
                    return; // cannot create gold as item, use $Gold instead
                }
                var iteminfo = _items.Find(item => item.VNum == vnum);
                if (iteminfo != null)
                {
                    if (iteminfo.IsColored || iteminfo.Effect == boxEffect)
                    {
                        if (createItemPacket.DesignOrAmount.HasValue)
                        {
                            design = (byte)createItemPacket.DesignOrAmount.Value;
                        }
                        rare = createItemPacket.Upgrade.HasValue && iteminfo.Effect == boxEffect ? (sbyte)createItemPacket.Upgrade.Value : rare;
                    }
                    else if (iteminfo.Type == PocketType.Equipment)
                    {
                        if (createItemPacket.Upgrade.HasValue)
                        {
                            if (iteminfo.EquipmentSlot != EquipmentType.Sp)
                            {
                                upgrade = createItemPacket.Upgrade.Value;
                            }
                            else
                            {
                                design = createItemPacket.Upgrade.Value;
                            }
                            if (iteminfo.EquipmentSlot != EquipmentType.Sp && upgrade == 0 && iteminfo.BasicUpgrade != 0)
                            {
                                upgrade = iteminfo.BasicUpgrade;
                            }
                        }
                        if (createItemPacket.DesignOrAmount.HasValue)
                        {
                            if (iteminfo.EquipmentSlot == EquipmentType.Sp)
                            {
                                upgrade = (byte)createItemPacket.DesignOrAmount.Value;
                            }
                            else
                            {
                                rare = (sbyte)createItemPacket.DesignOrAmount.Value;
                            }
                        }
                    }
                    if (createItemPacket.DesignOrAmount.HasValue && !createItemPacket.Upgrade.HasValue)
                    {
                        amount = createItemPacket.DesignOrAmount.Value > _worldConfiguration.MaxItemAmount ? _worldConfiguration.MaxItemAmount : createItemPacket.DesignOrAmount.Value;
                    }

                    var inv = Session.Character.Inventory.AddItemToPocket(_itemBuilderService.Create(vnum, Session.Character.CharacterId, amount: amount, rare: rare, upgrade: upgrade, design: design));

                    if (inv.Count > 0)
                    {
                        Session.SendPacket(inv.GeneratePocketChange());
                        var firstItem = inv[0];
                        var wearable = Session.Character.Inventory.LoadBySlotAndType<WearableInstance>(firstItem.Slot, firstItem.Type);
                        if (wearable != null)
                        {
                            switch (wearable.Item.EquipmentSlot)
                            {
                                case EquipmentType.Armor:
                                case EquipmentType.MainWeapon:
                                case EquipmentType.SecondaryWeapon:
                                    wearable.SetRarityPoint();
                                    break;

                                case EquipmentType.Boots:
                                case EquipmentType.Gloves:
                                    wearable.FireResistance = (short)(wearable.Item.FireResistance * upgrade);
                                    wearable.DarkResistance = (short)(wearable.Item.DarkResistance * upgrade);
                                    wearable.LightResistance = (short)(wearable.Item.LightResistance * upgrade);
                                    wearable.WaterResistance = (short)(wearable.Item.WaterResistance * upgrade);
                                    break;
                            }
                        }

                        Session.SendPacket(Session.Character.GenerateSay($"{Language.Instance.GetMessageFromKey(LanguageKey.ITEM_ACQUIRED, Session.Account.Language)}: {iteminfo.Name} x {amount}", SayColorType.Green));
                    }
                    else
                    {
                        Session.SendPacket(new MsgPacket() { Message = Language.Instance.GetMessageFromKey(LanguageKey.NOT_ENOUGH_PLACE, Session.Account.Language), Type = 0 });
                    }
                }
                else
                {
                    Session.SendPacket(new MsgPacket() { Message = Language.Instance.GetMessageFromKey(LanguageKey.NO_ITEM, Session.Account.Language), Type = 0 });
                }
            }
            else
            {
                Session.SendPacket(Session.Character.GenerateSay(CreateItemPacket.ReturnHelp(), SayColorType.Yellow));
            }
        }

        [UsedImplicitly]
        public void Speed(SpeedPacket speedPacket)
        {
            if (speedPacket.Speed > 0 && speedPacket.Speed < 60)
            {
                Session.Character.Speed = speedPacket.Speed >= 60 ? (byte)59 : speedPacket.Speed;
                Session.SendPacket(Session.Character.GenerateCond());
            }
            else
            {
                Session.SendPacket(Session.Character.GenerateSay(speedPacket.Help(), SayColorType.Yellow));
            }
        }

        [UsedImplicitly]
        public void Help(HelpPacket helpPacket)
        {
            Session.SendPacket(Session.Character.GenerateSay("-------------Help command-------------",
                SayColorType.Purple));
            var classes = helpPacket.GetType().Assembly.GetTypes().Where(t =>
                    typeof(ICommandPacket).IsAssignableFrom(t)
                    && t.GetCustomAttribute<PacketHeaderAttribute>()?.Authority <= Session.Account.Authority)
                .OrderBy(x => x.Name).ToList();
            foreach (var type in classes)
            {
                var classInstance = type.CreateInstance<ICommandPacket>();
                var method = type.GetMethod("Help");
                if (method == null)
                {
                    continue;
                }

                var message = method.Invoke(classInstance, null).ToString();
                if (!string.IsNullOrEmpty(message))
                {
                    Session.SendPacket(Session.Character.GenerateSay(message, SayColorType.Green));
                }
            }
        }
    }
}