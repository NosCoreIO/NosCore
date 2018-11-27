//  __  _  __    __   ___ __  ___ ___  
// |  \| |/__\ /' _/ / _//__\| _ \ __| 
// | | ' | \/ |`._`.| \_| \/ | v / _|  
// |_|\__|\__/ |___/ \__/\__/|_|_\___| 
// 
// Copyright (C) 2018 - NosCore
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

using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using JetBrains.Annotations;
using NosCore.Configuration;
using NosCore.Core.Extensions;
using NosCore.Core.Networking;
using NosCore.Core.Serializing;
using NosCore.Data.WebApi;
using NosCore.GameObject.ComponentEntities.Extensions;
using NosCore.GameObject.Networking;
using NosCore.GameObject.Networking.Group;
using NosCore.GameObject.Services.ItemBuilder;
using NosCore.GameObject.Services.ItemBuilder.Item;
using NosCore.GameObject.Services.MapInstanceAccess;
using NosCore.Packets.CommandPackets;
using NosCore.Packets.ServerPackets;
using NosCore.Shared.Enumerations;
using NosCore.Shared.Enumerations.Account;
using NosCore.Shared.Enumerations.Character;
using NosCore.Shared.Enumerations.Interaction;
using NosCore.Shared.Enumerations.Items;
using NosCore.Shared.I18N;
using Serilog;

namespace NosCore.Controllers
{
    [UsedImplicitly]
    public class CommandPacketController : PacketController
    {
        private readonly IItemBuilderService _itemBuilderService;
        private readonly List<Item> _items;
        private readonly MapInstanceAccessService _mapInstanceAccessService;
        private readonly WorldConfiguration _worldConfiguration;
        private readonly ILogger _logger = Logger.GetLoggerConfiguration().CreateLogger();

        public CommandPacketController(WorldConfiguration worldConfiguration, List<Item> items,
            IItemBuilderService itemBuilderService, MapInstanceAccessService mapInstanceAccessService)
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
        public void Invisible(InvisibleCommandPacket invisiblePacket)
        {
            Session.Character.Camouflage = !Session.Character.Camouflage;
            Session.Character.Invisible = !Session.Character.Invisible;
            Session.Character.MapInstance.Sessions.SendPacket(Session.Character.GenerateInvisible());
            //Session.SendPacket(Session.Character.GenerateEq());
        }

        [UsedImplicitly]
        public void Position(PositionPacket positionPacket)
        {
            Session.SendPacket(Session.Character.GenerateSay(
                $"Map:{Session.Character.MapInstance.Map.MapId} - X:{Session.Character.PositionX} - Y:{Session.Character.PositionY} - " +
                    $"Dir:{Session.Character.Direction} - Cell:{Session.Character.MapInstance.Map[Session.Character.PositionX, Session.Character.PositionY]}",
                        SayColorType.Green));
        }

        [UsedImplicitly]
        public void Effect(EffectCommandPacket effectCommandpacket)
        {
            Session.Character.MapInstance.Sessions.SendPacket(
                    Session.Character.GenerateEff(effectCommandpacket.EffectId));
        }

        [UsedImplicitly]
        public void Teleport(TeleportPacket teleportPacket)
        {
            var session =
                Broadcaster.Instance.GetCharacter(s =>
                s.Name == teleportPacket.TeleportArgument); //TODO setter to protect

            if (!short.TryParse(teleportPacket.TeleportArgument, out var mapId))
            {
                if (session == null)
                {
                    _logger.Error(Language.Instance.GetMessageFromKey(LanguageKey.USER_NOT_CONNECTED,
                        Session.Account.Language));
                    return;
                }

                Session.ChangeMapInstance(session.MapInstanceId, session.MapX, session.MapY);
                return;
            }

            var mapInstance = _mapInstanceAccessService.GetBaseMapById(mapId);

            if (mapInstance == null)
            {
                _logger.Error(
                     Language.Instance.GetMessageFromKey(LanguageKey.MAP_DONT_EXIST, Session.Account.Language));
                return;
            }

            Session.ChangeMap(mapId, teleportPacket.MapX, teleportPacket.MapY);
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
            var message =
                $"({Language.Instance.GetMessageFromKey(LanguageKey.ADMINISTRATOR, Session.Account.Language)}) {shoutPacket.Message}";
            var sayPacket = new SayPacket
            {
                VisualType = VisualType.Player,
                VisualId = 0,
                Type = SayColorType.Yellow,
                Message = message
            };

            var msgPacket = new MsgPacket
            {
                Type = MessageType.Shout,
                Message = message
            };

            var sayPostedPacket = new PostedPacket
            {
                Packet = PacketFactory.Serialize(new[] { sayPacket }),
                SenderCharacter = new Character
                {
                    Name = Session.Character.Name,
                    Id = Session.Character.CharacterId
                },
                ReceiverType = ReceiverType.All
            };

            var msgPostedPacket = new PostedPacket
            {
                Packet = PacketFactory.Serialize(new[] { msgPacket }),
                ReceiverType = ReceiverType.All
            };

            WebApiAccess.Instance.BroadcastPackets(new List<PostedPacket>(new[] { sayPostedPacket, msgPostedPacket }));
        }

        [UsedImplicitly]
        public void CreateItem(CreateItemPacket createItemPacket)
        {
            var vnum = createItemPacket.VNum;
            sbyte rare = 0;
            const short boxEffect = 999;
            byte upgrade = 0;
            byte design = 0;
            short amount = 1;
            if (vnum == 1046)
            {
                return; // cannot create gold as item, use $Gold instead
            }

            var iteminfo = _items.Find(item => item.VNum == vnum);
            if (iteminfo == null)
            {
                Session.SendPacket(new MsgPacket
                {
                    Message = Language.Instance.GetMessageFromKey(LanguageKey.NO_ITEM, Session.Account.Language),
                    Type = 0
                });
                return;
            }

            if (iteminfo.IsColored || iteminfo.Effect == boxEffect)
            {
                if (createItemPacket.DesignOrAmount.HasValue)
                {
                    design = (byte)createItemPacket.DesignOrAmount.Value;
                }

                rare = createItemPacket.Upgrade.HasValue && iteminfo.Effect == boxEffect
                    ? (sbyte)createItemPacket.Upgrade.Value : rare;
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

                    if (iteminfo.EquipmentSlot != EquipmentType.Sp && upgrade == 0
                        && iteminfo.BasicUpgrade != 0)
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
                amount = createItemPacket.DesignOrAmount.Value > _worldConfiguration.MaxItemAmount
                    ? _worldConfiguration.MaxItemAmount : createItemPacket.DesignOrAmount.Value;
            }

            var inv = Session.Character.Inventory.AddItemToPocket(_itemBuilderService.Create(vnum,
                Session.Character.CharacterId, amount: amount, rare: rare, upgrade: upgrade, design: design));

            if (inv.Count <= 0)
            {
                Session.SendPacket(new MsgPacket
                {
                    Message = Language.Instance.GetMessageFromKey(LanguageKey.NOT_ENOUGH_PLACE,
                    Session.Account.Language),
                    Type = 0
                });
                return;
            }

            Session.SendPacket(inv.GeneratePocketChange());
            var firstItem = inv[0];
            var wearable =
                Session.Character.Inventory.LoadBySlotAndType<WearableInstance>(firstItem.Slot,
                firstItem.Type);

            if (wearable?.Item.EquipmentSlot is EquipmentType.Armor ||
                wearable?.Item.EquipmentSlot is EquipmentType.MainWeapon ||
                wearable?.Item.EquipmentSlot is EquipmentType.SecondaryWeapon)
            {
                wearable.SetRarityPoint();
            }
            else if (wearable?.Item.EquipmentSlot is EquipmentType.Boots ||
                wearable?.Item.EquipmentSlot is EquipmentType.Gloves)
            {
                wearable.FireResistance = (short)(wearable.Item.FireResistance * upgrade);
                wearable.DarkResistance = (short)(wearable.Item.DarkResistance * upgrade);
                wearable.LightResistance = (short)(wearable.Item.LightResistance * upgrade);
                wearable.WaterResistance = (short)(wearable.Item.WaterResistance * upgrade);
            }
            else
            {
                _logger.Debug(LogLanguage.Instance.GetMessageFromKey(LanguageKey.NO_SPECIAL_PROPERTIES_WEARABLE));
            }

            Session.SendPacket(Session.Character.GenerateSay(
                $"{Language.Instance.GetMessageFromKey(LanguageKey.ITEM_ACQUIRED, Session.Account.Language)}: {iteminfo.Name} x {amount}",
                SayColorType.Green));
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
        public void Level(SetLevelCommandPacket levelPacket)
        {
           Session.Character.SetLevel(levelPacket.Level);
        }

        [UsedImplicitly]
        public void JobLevel(SetJobLevelCommandPacket jobLevelPacket)
        {
            Session.Character.SetJobLevel(jobLevelPacket.Level);
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