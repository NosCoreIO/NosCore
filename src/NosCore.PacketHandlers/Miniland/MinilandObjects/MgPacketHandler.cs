//  __  _  __    __   ___ __  ___ ___
// |  \| |/__\ /' _/ / _//__\| _ \ __|
// | | ' | \/ |`._`.| \_| \/ | v / _|
// |_|\__|\__/ |___/ \__/\__/|_|_\___|
//

using NosCore.GameObject.Ecs.Extensions;
using NosCore.GameObject.Infastructure;
using NosCore.GameObject.Map;
using NosCore.GameObject.Networking.ClientSession;
using NosCore.GameObject.Services.InventoryService;
using NosCore.GameObject.Services.ItemGenerationService;
using NosCore.GameObject.Services.ItemGenerationService.Item;
using NosCore.GameObject.Services.MailService;
using NosCore.GameObject.Services.MinilandService;
using NosCore.Networking;
using NosCore.Packets.ClientPackets.Miniland;
using NosCore.Packets.Enumerations;
using NosCore.Packets.Interfaces;
using NosCore.Packets.ServerPackets.Chats;
using NosCore.Packets.ServerPackets.Miniland;
using NosCore.Packets.ServerPackets.UI;
using NosCore.Shared.Enumerations;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;


//TODO stop using obsolete
#pragma warning disable 618

namespace NosCore.PacketHandlers.Miniland.MinilandObjects
{
    public class MgPacketHandler(IMinilandService minilandProvider, IItemGenerationService itemProvider)
        : PacketHandler<MinigamePacket>, IWorldPacketHandler
    {
        private sealed record Minigame(ClientSession Session, MinigamePacket Packet,
            GameObject.Services.MinilandService.Miniland Miniland, MapDesignObject Object, IItemInstance Instance);

        public override async Task ExecuteAsync(MinigamePacket minigamePacket, ClientSession clientSession)
        {
            var miniland = minilandProvider.GetMiniland(clientSession.Character.CharacterId);
            var minilandObject =
                clientSession.Character.MapInstance.MapDesignObjects.Values.FirstOrDefault(s =>
                    s.Slot == minigamePacket.Id);
            if ((minilandObject == null) || (miniland == null))
            {
                return;
            }

            if (minilandObject.InventoryItemInstance?.ItemInstance is not { Item.IsWarehouse: false } instance)
            {
                return;
            }

            var mg = new Minigame(clientSession, minigamePacket, miniland, minilandObject, instance);
            var game = (byte)(instance.Item.EquipmentSlot ==
                EquipmentType.MainWeapon
                    ? (4 + instance.ItemVNum) % 10
                    : (int)instance.Item.EquipmentSlot / 3);
            //todo check if enought points

            switch (minigamePacket.Type)
            {
                case 1:
                    await PlayAsync(mg, game);
                    break;

                case 2:
                    await BroadcastEffectAsync(mg);
                    break;

                case 3:
                    await ShowBoxLevelsAsync(mg, game);
                    break;

                case 4:
                    await SelectGiftAsync(mg);
                    break;

                case 5:
                    await ShowMinilandManagmentAsync(mg);
                    break;

                case 6:
                    await RefillAsync(mg);
                    break;

                case 7:
                    await ShowGiftsAsync(mg);
                    break;

                case 8:
                    await OpenGiftBatchAsync(mg);
                    break;

                case 9:
                    await UseCouponAsync(mg);
                    break;
            }
        }

        private async Task UseCouponAsync(Minigame mg)
        {
            var item = mg.Session.Character.InventoryService.Select(s => s.Value)
                .Where(s => (s.ItemInstance?.ItemVNum == 1269) || (s.ItemInstance?.ItemVNum == 1271)).OrderBy(s => s.Slot)
                .FirstOrDefault();
            if (item != null)
            {
                var point = item.ItemInstance.ItemVNum == 1269 ? 300 : 500;
                mg.Session.Character.InventoryService.RemoveItemAmountFromInventory(1, item.ItemInstance.Id);
                mg.Instance.DurabilityPoint += point;
                await mg.Session.SendPacketAsync(new InfoiPacket
                {
                    Message = Game18NConstString.ToppedUpPoints,
                    ArgumentType = 4,
                    Game18NArguments = { point }
                });
                await ShowMinilandManagmentAsync(mg);
            }
        }

        private Task ShowMinilandManagmentAsync(Minigame mg)
        {
            return mg.Session.SendPacketAsync(new MloMgPacket
            {
                MinigameVNum = mg.Packet.MinigameVNum,
                MinilandPoint = mg.Miniland.MinilandPoint,
                Unknown1 = 0,
                Unknown2 = 0,
                DurabilityPoint = mg.Instance.DurabilityPoint,
                MinilandObjectPoint = mg.Instance.Item.MinilandObjectPoint
            });
        }

        private async Task OpenGiftBatchAsync(Minigame mg)
        {
            var amount = 0;
            switch (mg.Packet.Point)
            {
                case 0:
                    amount = mg.Object.Level1BoxAmount;
                    break;

                case 1:
                    amount = mg.Object.Level2BoxAmount;
                    break;

                case 2:
                    amount = mg.Object.Level3BoxAmount;
                    break;

                case 3:
                    amount = mg.Object.Level4BoxAmount;
                    break;

                case 4:
                    amount = mg.Object.Level5BoxAmount;
                    break;
            }

            var gifts = new List<Gift>();
            for (var i = 0; i < amount; i++)
            {
                var gift = MinilandHelper.Instance.GetMinilandGift(mg.Packet.MinigameVNum,
                    mg.Packet.Point ?? 0);
                if (gift == null)
                {
                    return;
                }
                if (gifts.Any(o => o.VNum == gift.VNum))
                {
                    gifts.First(o => o.Amount == gift.Amount).Amount += gift.Amount;
                }
                else
                {
                    gifts.Add(gift);
                }
            }

            var str = string.Empty;
            var list = new List<MloPmgSubPacket>();
            for (var i = 0; i < 9; i++)
            {
                if (gifts.Count > i)
                {
                    var item = itemProvider.Create(gifts.ElementAt(i).VNum, gifts.ElementAt(i).Amount);
                    var inv = mg.Session.Character.InventoryService.AddItemToPocket(
                        InventoryItemInstance.Create(item, mg.Session.Character.CharacterId));
                    if (inv != null && inv.Count != 0)
                    {
                        await mg.Session.SendPacketAsync(new SayiPacket
                        {
                            VisualType = VisualType.Player,
                            VisualId = mg.Session.Character.CharacterId,
                            Type = SayColorType.Yellow,
                            Message = Game18NConstString.ReceivedThisItem,
                            ArgumentType = 2,
                            Game18NArguments = { item.Item.VNum.ToString(), amount }
                        });
                    }

                    list.Add(new MloPmgSubPacket
                    { BoxVNum = gifts.ElementAt(i).VNum, BoxAmount = gifts.ElementAt(i).Amount });
                }
                else
                {
                    list.Add(new MloPmgSubPacket { BoxVNum = 0, BoxAmount = 0 });
                }
            }

            await ShowGiftsAsync(mg, list.ToArray());
        }

        private Task ShowGiftsAsync(Minigame mg)
        {
            return ShowGiftsAsync(mg, new[]
            {
                new MloPmgSubPacket {BoxVNum = 0, BoxAmount = 0},
                new MloPmgSubPacket {BoxVNum = 0, BoxAmount = 0},
                new MloPmgSubPacket {BoxVNum = 0, BoxAmount = 0},
                new MloPmgSubPacket {BoxVNum = 0, BoxAmount = 0},
                new MloPmgSubPacket {BoxVNum = 0, BoxAmount = 0},
                new MloPmgSubPacket {BoxVNum = 0, BoxAmount = 0},
                new MloPmgSubPacket {BoxVNum = 0, BoxAmount = 0},
                new MloPmgSubPacket {BoxVNum = 0, BoxAmount = 0},
                new MloPmgSubPacket {BoxVNum = 0, BoxAmount = 0}
            });
        }

        private Task ShowGiftsAsync(Minigame mg, MloPmgSubPacket[] array)
        {
            return mg.Session.SendPacketAsync(new MloPmgPacket
            {
                MinigameVNum = mg.Packet.MinigameVNum,
                MinilandPoint = mg.Miniland.MinilandPoint,
                LowDurability = mg.Instance.DurabilityPoint < 1000,
                IsFull = false,
                MloPmgSubPackets = new[]
                {
                    new MloPmgSubPacket {BoxVNum = 392, BoxAmount = mg.Object.Level1BoxAmount},
                    new MloPmgSubPacket {BoxVNum = 393, BoxAmount = mg.Object.Level2BoxAmount},
                    new MloPmgSubPacket {BoxVNum = 394, BoxAmount = mg.Object.Level3BoxAmount},
                    new MloPmgSubPacket {BoxVNum = 395, BoxAmount = mg.Object.Level4BoxAmount},
                    new MloPmgSubPacket {BoxVNum = 396, BoxAmount = mg.Object.Level5BoxAmount}
                }.Concat(array).ToArray()
            });
        }

        private async Task RefillAsync(Minigame mg)
        {
            if (mg.Packet?.Point == null)
            {
                return;
            }

            if (mg.Session?.Character.Gold > mg.Packet.Point)
            {
                mg.Session.Character.Gold -= (int)mg.Packet.Point;
                await mg.Session.SendPacketAsync(mg.Session.Character.GenerateGold());
                mg.Instance.DurabilityPoint +=
                    (int)(mg.Packet.Point / 100);
                await mg.Session.SendPacketAsync(new InfoiPacket
                {
                    Message = Game18NConstString.ToppedUpPoints,
                    ArgumentType = 4,
                    Game18NArguments = { (int)mg.Packet.Point / 100 }
                });
                await ShowMinilandManagmentAsync(mg);
            }
        }

        private async Task SelectGiftAsync(Minigame mg)
        {
            if (mg.Miniland.MinilandPoint < 100)
            {
                return;
            }

            var obj = MinilandHelper.Instance.GetMinilandGift(mg.Packet.MinigameVNum, mg.Packet.Point ?? 0);
            if (obj == null)
            {
                return;
            }
            await mg.Session.SendPacketAsync(new MloRwPacket { Amount = obj.Amount, VNum = obj.VNum });
            // mg.Session.SendPacket(new MlptPacket {mg.Miniland.MinilandPoint, 100});
            var inv = mg.Session.Character.InventoryService.AddItemToPocket(InventoryItemInstance.Create(
                itemProvider.Create(obj.VNum,
                    obj.Amount), mg.Session.Character.CharacterId));
            mg.Miniland.MinilandPoint -= 100;
            if (inv == null || inv.Count == 0)
            {
                //todo add gifts
                //mg.Session.Character.SendGift(mg.Session.Character.CharacterId, obj.VNum, obj.Amount, 0, 0, false);
            }

            if (mg.Miniland.MapInstanceId != mg.Session.Character.MapInstanceId)
            {
                switch (mg.Packet.Point)
                {
                    case 0:
                        mg.Object.Level1BoxAmount++;
                        break;

                    case 1:
                        mg.Object.Level2BoxAmount++;
                        break;

                    case 2:
                        mg.Object.Level3BoxAmount++;
                        break;

                    case 3:
                        mg.Object.Level4BoxAmount++;
                        break;

                    case 4:
                        mg.Object.Level5BoxAmount++;
                        break;
                }
            }
        }

        private async Task ShowBoxLevelsAsync(Minigame mg, byte game)
        {
            mg.Miniland.CurrentMinigame = 0;
            await mg.Session.Character.MapInstance.SendPacketAsync(new GuriPacket
            {
                Type = GuriPacketType.Dance,
                Value = 1,
                EntityId = mg.Session.Character.CharacterId
            });
            short level = -1;
            for (short i = 0; i < MinilandHelper.Instance.MinilandMaxPoint[game].Count(); i++)
            {
                if (mg.Packet.Point > MinilandHelper.Instance.MinilandMaxPoint[game][i])
                {
                    level = i;
                }
                else
                {
                    break;
                }
            }

            await mg.Session.SendPacketAsync(level != -1
                ? new MloLvPacket { Level = level }
                : (IPacket)new MinigamePacket
                { Type = 3, Id = game, MinigameVNum = mg.Packet.MinigameVNum, Unknown = 0, Point = 0 });
        }

        private Task BroadcastEffectAsync(Minigame mg)
        {
            mg.Miniland.CurrentMinigame = 0;
            return mg.Session.Character.MapInstance.SendPacketAsync(new GuriPacket
            {
                Type = GuriPacketType.Dance,
                Value = 1,
                EntityId = mg.Session.Character.CharacterId
            });
        }

        private async Task PlayAsync(Minigame mg, byte game)
        {
            if (mg.Instance.DurabilityPoint <= 0)
            {
                await mg.Session.SendPacketAsync(new SayiPacket
                {
                    VisualType = VisualType.Player,
                    VisualId = mg.Session.Character.CharacterId,
                    Type = SayColorType.Red,
                    Message = Game18NConstString.NeedToRestoreDurability
                });
                return;
            }

            if (mg.Miniland == null || mg.Miniland.MinilandPoint <= 0)
            {
                await mg.Session.SendPacketAsync(new QnaiPacket
                {
                    YesPacket = new MinigamePacket
                    {
                        Type = 1,
                        Id = 7,
                        MinigameVNum = 3125,
                        Point = 1,
                        Unknown = 1
                    },
                    Question = Game18NConstString.NotEnoughProductionPointsAskStart
                });
                return;
            }

            await mg.Session.Character.MapInstance.SendPacketAsync(new GuriPacket
            {
                Type = GuriPacketType.Dance,
                Value = 1,
                EntityId = mg.Session.Character.CharacterId
            });
            mg.Miniland.CurrentMinigame = (short)(game == 0 ? 5102 : game == 1 ? 5103 : game == 2 ? 5105 : game == 3
                ? 5104 : game == 4 ? 5113 : 5112);
            await mg.Session.SendPacketAsync(new MloStPacket { Game = game });
        }
    }
}
