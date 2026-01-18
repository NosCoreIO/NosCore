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

using Arch.Core;
using NosCore.Data.Enumerations;
using NosCore.GameObject.Ecs;
using NosCore.GameObject.Ecs.Components;
using NosCore.GameObject.Services.InventoryService;
using NosCore.GameObject.Services.ItemGenerationService.Item;
using NosCore.Packets.ClientPackets.Inventory;
using NosCore.Packets.ClientPackets.Npcs;
using NosCore.Packets.Enumerations;
using NosCore.Packets.ServerPackets.Chats;
using NosCore.Packets.ServerPackets.Entities;
using NosCore.Packets.ServerPackets.Groups;
using NosCore.Packets.ServerPackets.Inventory;
using NosCore.Packets.ServerPackets.Movement;
using NosCore.Packets.ServerPackets.Player;
using NosCore.Packets.ServerPackets.Shop;
using NosCore.Shared.Enumerations;
using System.Collections.Generic;
using System.Linq;

namespace NosCore.GameObject.Ecs.Systems;

public interface IEntityPacketSystem
{
    DirPacket GenerateChangeDir(PlayerContext player);
    DirPacket GenerateChangeDir(Entity entity, MapWorld world);
    RequestNpcPacket GenerateNpcReq(PlayerContext player, long dialog);
    RequestNpcPacket GenerateNpcReq(Entity entity, MapWorld world, long dialog);
    PinitSubPacket GenerateSubPinit(PlayerContext player, int groupPosition);
    PidxSubPacket GenerateSubPidx(PlayerContext player);
    PidxSubPacket GenerateSubPidx(PlayerContext player, bool isMemberOfGroup);
    PidxSubPacket GenerateSubPidx(long visualId, bool isMemberOfGroup);
    SayPacket GenerateSay(PlayerContext player, string message, SayColorType type);
    SayPacket GenerateSay(Entity entity, MapWorld world, string message, SayColorType type);
    SayPacket GenerateSay(PlayerContext player, SayPacket packet);
    SayItemPacket GenerateSayItem(PlayerContext player, string message, InventoryItemInstance item);
    ShopPacket GenerateShop(PlayerContext player, Shop? shop, RegionType language);
    ShopPacket GenerateShop(Entity entity, MapWorld world, Shop? shop, RegionType language);
    UseItemPacket GenerateUseItem(PlayerContext player, PocketType type, short slot, byte mode, byte parameter);
    PairyPacket GeneratePairy(PlayerContext player, WearableInstance? fairy);
    MovePacket GenerateMove(PlayerContext player);
    MovePacket GenerateMove(PlayerContext player, short? mapX, short? mapY);
    EffectPacket GenerateEff(PlayerContext player, int effectId);
    EffectPacket GenerateEff(Entity entity, MapWorld world, int effectId);
    RestPacket GenerateRest(PlayerContext player);
    PflagPacket GeneratePFlag(PlayerContext player, Shop? shop);
    NInvPacket GenerateNInv(PlayerContext player, Shop shop, double percent, short typeShop);
    NInvPacket GenerateNInv(Entity entity, MapWorld world, Shop shop, double percent, short typeShop);
}

public class EntityPacketSystem : IEntityPacketSystem
{
    public DirPacket GenerateChangeDir(PlayerContext player)
    {
        return new DirPacket
        {
            VisualType = VisualType.Player,
            VisualId = player.VisualId,
            Direction = player.Direction
        };
    }

    public DirPacket GenerateChangeDir(Entity entity, MapWorld world)
    {
        return new DirPacket
        {
            VisualType = entity.GetVisualType(world),
            VisualId = entity.GetVisualId(world),
            Direction = entity.GetDirection(world)
        };
    }

    public RequestNpcPacket GenerateNpcReq(PlayerContext player, long dialog)
    {
        return new RequestNpcPacket
        {
            Type = VisualType.Player,
            TargetId = player.VisualId,
            Data = dialog
        };
    }

    public RequestNpcPacket GenerateNpcReq(Entity entity, MapWorld world, long dialog)
    {
        return new RequestNpcPacket
        {
            Type = entity.GetVisualType(world),
            TargetId = entity.GetVisualId(world),
            Data = dialog
        };
    }

    public PinitSubPacket GenerateSubPinit(PlayerContext player, int groupPosition)
    {
        return new PinitSubPacket
        {
            VisualType = VisualType.Player,
            VisualId = player.VisualId,
            GroupPosition = groupPosition,
            Level = player.Level,
            Name = player.Name,
            Gender = player.Gender,
            Race = (byte)player.Class,
            Morph = player.Morph,
            HeroLevel = player.HeroLevel
        };
    }

    public PidxSubPacket GenerateSubPidx(PlayerContext player)
    {
        return GenerateSubPidx(player, false);
    }

    public PidxSubPacket GenerateSubPidx(PlayerContext player, bool isMemberOfGroup)
    {
        return new PidxSubPacket
        {
            IsGrouped = isMemberOfGroup,
            VisualId = player.VisualId
        };
    }

    public PidxSubPacket GenerateSubPidx(long visualId, bool isMemberOfGroup)
    {
        return new PidxSubPacket
        {
            IsGrouped = isMemberOfGroup,
            VisualId = visualId
        };
    }

    public SayPacket GenerateSay(PlayerContext player, string message, SayColorType type)
    {
        return new SayPacket
        {
            VisualType = VisualType.Player,
            VisualId = player.VisualId,
            Type = type,
            Message = message
        };
    }

    public SayPacket GenerateSay(Entity entity, MapWorld world, string message, SayColorType type)
    {
        return new SayPacket
        {
            VisualType = entity.GetVisualType(world),
            VisualId = entity.GetVisualId(world),
            Type = type,
            Message = message
        };
    }

    public SayPacket GenerateSay(PlayerContext player, SayPacket packet)
    {
        return new SayPacket
        {
            VisualType = VisualType.Player,
            VisualId = player.VisualId,
            Type = packet.Type,
            Message = packet.Message
        };
    }

    public SayItemPacket GenerateSayItem(PlayerContext player, string message, InventoryItemInstance item)
    {
        var isNormalItem = item.Type != NoscorePocketType.Equipment && item.Type != NoscorePocketType.Specialist;
        return new SayItemPacket
        {
            VisualType = VisualType.Player,
            VisualId = player.VisualId,
            OratorSlot = 17,
            Message = message,
            IconInfo = isNormalItem ? new IconInfoPacket
            {
                IconId = item.ItemInstance.ItemVNum
            } : null,
            EquipmentInfo = isNormalItem ? null : new EInfoPacket(),
            SlInfo = item.Type != NoscorePocketType.Specialist ? null : new SlInfoPacket()
        };
    }

    public ShopPacket GenerateShop(PlayerContext player, Shop? shop, RegionType language)
    {
        return new ShopPacket
        {
            VisualType = VisualType.Player,
            VisualId = player.VisualId,
            ShopId = shop?.ShopId ?? 0,
            MenuType = shop?.MenuType ?? 0,
            ShopType = shop?.ShopType,
            Name = shop?.Name[language]
        };
    }

    public ShopPacket GenerateShop(Entity entity, MapWorld world, Shop? shop, RegionType language)
    {
        return new ShopPacket
        {
            VisualType = entity.GetVisualType(world),
            VisualId = entity.GetVisualId(world),
            ShopId = shop?.ShopId ?? 0,
            MenuType = shop?.MenuType ?? 0,
            ShopType = shop?.ShopType,
            Name = shop?.Name[language]
        };
    }

    public UseItemPacket GenerateUseItem(PlayerContext player, PocketType type, short slot, byte mode, byte parameter)
    {
        return new UseItemPacket
        {
            VisualId = player.VisualId,
            VisualType = VisualType.Player,
            Type = type,
            Slot = slot,
            Mode = mode,
            Parameter = parameter
        };
    }

    public PairyPacket GeneratePairy(PlayerContext player, WearableInstance? fairy)
    {
        var isBuffed = false;
        return new PairyPacket
        {
            VisualType = VisualType.Player,
            VisualId = player.VisualId,
            FairyMoveType = fairy == null ? 0 : 4,
            Element = fairy?.Item?.Element ?? 0,
            ElementRate = fairy?.ElementRate + fairy?.Item?.ElementRate ?? 0,
            Morph = fairy?.Item?.Morph ?? 0 + (isBuffed ? 5 : 0)
        };
    }

    public MovePacket GenerateMove(PlayerContext player)
    {
        return GenerateMove(player, null, null);
    }

    public MovePacket GenerateMove(PlayerContext player, short? mapX, short? mapY)
    {
        return new MovePacket
        {
            VisualEntityId = player.VisualId,
            MapX = mapX ?? player.PositionX,
            MapY = mapY ?? player.PositionY,
            Speed = player.Speed,
            VisualType = VisualType.Player
        };
    }

    public EffectPacket GenerateEff(PlayerContext player, int effectId)
    {
        return new EffectPacket
        {
            EffectType = VisualType.Player,
            VisualEntityId = player.VisualId,
            Id = effectId
        };
    }

    public EffectPacket GenerateEff(Entity entity, MapWorld world, int effectId)
    {
        return new EffectPacket
        {
            EffectType = entity.GetVisualType(world),
            VisualEntityId = entity.GetVisualId(world),
            Id = effectId
        };
    }

    public RestPacket GenerateRest(PlayerContext player)
    {
        return new RestPacket
        {
            VisualType = VisualType.Player,
            VisualId = player.VisualId,
            IsSitting = player.IsSitting
        };
    }

    public PflagPacket GeneratePFlag(PlayerContext player, Shop? shop)
    {
        return new PflagPacket
        {
            VisualType = VisualType.Player,
            VisualId = player.VisualId,
            Flag = shop?.ShopId ?? 0
        };
    }

    public NInvPacket GenerateNInv(PlayerContext player, Shop shop, double percent, short typeShop)
    {
        return GenerateNInvInternal(VisualType.Player, player.VisualId, shop, percent, typeShop);
    }

    public NInvPacket GenerateNInv(Entity entity, MapWorld world, Shop shop, double percent, short typeShop)
    {
        return GenerateNInvInternal(entity.GetVisualType(world), entity.GetVisualId(world), shop, percent, typeShop);
    }

    private NInvPacket GenerateNInvInternal(VisualType visualType, long visualId, Shop shop, double percent, short typeShop)
    {
        var shopItemList = new List<NInvItemSubPacket?>();
        var list = shop.ShopItems.Values.Where(s => s.Type == typeShop).ToList();
        for (var i = 0; i < shop.Size; i++)
        {
            var item = list.Find(s => s.Slot == i);
            if (item == null)
            {
                shopItemList.Add(null);
            }
            else
            {
                shopItemList.Add(new NInvItemSubPacket
                {
                    Type = (PocketType)item.ItemInstance!.Item.Type,
                    Slot = item.Slot,
                    Price = (int)(item.Price ?? (item.ItemInstance.Item.ReputPrice > 0
                        ? item.ItemInstance.Item.ReputPrice : item.ItemInstance.Item.Price * percent)),
                    RareAmount = item.ItemInstance.Item.Type == (byte)NoscorePocketType.Equipment
                        ? item.ItemInstance.Rare
                        : item.Amount,
                    UpgradeDesign = item.ItemInstance.Item.Type == (byte)NoscorePocketType.Equipment
                        ? item.ItemInstance.Item.IsColored
                            ? item.ItemInstance.Item.Color : item.ItemInstance.Upgrade : (short?)null,
                    VNum = item.ItemInstance.Item.VNum
                });
            }
        }

        return new NInvPacket
        {
            VisualType = visualType,
            VisualId = visualId,
            ShopKind = (byte)(percent * 100),
            Unknown = 0,
            Items = shopItemList
        };
    }
}
