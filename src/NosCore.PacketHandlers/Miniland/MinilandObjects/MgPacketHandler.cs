//  __  _  __    __   ___ __  ___ ___
// |  \| |/__\ /' _/ / _//__\| _ \ __|
// | | ' | \/ |`._`.| \_| \/ | v / _|
// |_|\__|\__/ |___/ \__/\__/|_|_\___|
//

using NosCore.Data.Enumerations.Items;
using NosCore.Data.Enumerations.Miniland;
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
    public class MgPacketHandler(IMinilandService minilandProvider, IItemGenerationService itemProvider,
        IMinigameRewardService minigameRewards)
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
            if (instance.Item.MinigameType is not { } game)
            {
                return;
            }

            //todo check if enought points

            switch (minigamePacket.Type)
            {
                case MinigamePacketType.Play:
                    await PlayAsync(mg, game);
                    break;

                case MinigamePacketType.BroadcastEffect:
                    await BroadcastEffectAsync(mg);
                    break;

                case MinigamePacketType.ShowBoxLevels:
                    await ShowBoxLevelsAsync(mg, game);
                    break;

                case MinigamePacketType.SelectGift:
                    await SelectGiftAsync(mg);
                    break;

                case MinigamePacketType.ShowManagement:
                    await ShowMinilandManagmentAsync(mg);
                    break;

                case MinigamePacketType.Refill:
                    await RefillAsync(mg);
                    break;

                case MinigamePacketType.ShowGifts:
                    await ShowGiftsAsync(mg);
                    break;

                case MinigamePacketType.OpenGiftBatch:
                    await OpenGiftBatchAsync(mg);
                    break;

                case MinigamePacketType.UseCoupon:
                    await UseCouponAsync(mg);
                    break;
            }
        }

        private async Task UseCouponAsync(Minigame mg)
        {
            var item = mg.Session.Character.InventoryService.Select(s => s.Value)
                .Where(s => s.ItemInstance?.Item.Effect == ItemEffectType.MinigameDurabilityCoupon).OrderBy(s => s.Slot)
                .FirstOrDefault();
            if (item == null)
            {
                await mg.Session.SendPacketAsync(new InfoiPacket { Message = Game18NConstString.NoCoupons });
                return;
            }

            var point = item.ItemInstance.Item.EffectValue;
            await mg.Session.Character.UseUpOneAsync(item);
            mg.Instance.DurabilityPoint += point;
            await mg.Session.SendPacketAsync(new InfoiPacket
            {
                Message = Game18NConstString.ToppedUpPoints,
                ArgumentType = 4,
                Game18NArguments = { point }
            });
            await ShowMinilandManagmentAsync(mg);
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
                var reward = minigameRewards.DrawReward(mg.Packet.MinigameVNum, (byte)(mg.Packet.Point ?? 0));
                var gift = reward == null ? null : new Gift(reward.ItemVNum, (byte)reward.Amount);
                if (gift == null)
                {
                    return;
                }
                if (gifts.Any(o => o.VNum == gift.VNum))
                {
                    gifts.First(o => o.VNum == gift.VNum).Amount += gift.Amount;
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
                        await mg.Session.SendPacketAsync(mg.Session.Character.GenerateSayi(SayColorType.Yellow, Game18NConstString.ReceivedThisItem,
                            2, item.Item.VNum.ToString(), amount));
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

            var obj = minigameRewards.DrawReward(mg.Packet.MinigameVNum, (byte)(mg.Packet.Point ?? 0));
            if (obj == null)
            {
                return;
            }
            await mg.Session.SendPacketAsync(new MloRwPacket { Amount = obj.Amount, VNum = obj.ItemVNum });
            // mg.Session.SendPacket(new MlptPacket {mg.Miniland.MinilandPoint, 100});
            var inv = mg.Session.Character.InventoryService.AddItemToPocket(InventoryItemInstance.Create(
                itemProvider.Create(obj.ItemVNum,
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

        private async Task ShowBoxLevelsAsync(Minigame mg, MinigameType game)
        {
            mg.Miniland.CurrentMinigame = 0;
            await mg.Session.Character.MapInstance.SendPacketAsync(new GuriPacket
            {
                Type = GuriPacketType.Dance,
                Value = 1,
                EntityId = mg.Session.Character.CharacterId
            });
            var level = minigameRewards.GetLevel(game, mg.Packet.Point ?? 0);

            await mg.Session.SendPacketAsync(level is { } reached
                ? new MloLvPacket { Level = reached }
                : (IPacket)new MinigamePacket
                { Type = MinigamePacketType.ShowBoxLevels, Id = (byte)game, MinigameVNum = mg.Packet.MinigameVNum, Unknown = 0, Point = 0 });
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

        private async Task PlayAsync(Minigame mg, MinigameType game)
        {
            if (mg.Instance.DurabilityPoint <= 0)
            {
                await mg.Session.SendPacketAsync(mg.Session.Character.GenerateSayi(SayColorType.Red, Game18NConstString.NeedToRestoreDurability));
                return;
            }

            if (mg.Miniland == null || mg.Miniland.MinilandPoint <= 0)
            {
                await mg.Session.SendPacketAsync(new QnaiPacket
                {
                    YesPacket = new MinigamePacket
                    {
                        Type = MinigamePacketType.Play,
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
            mg.Miniland.CurrentMinigame = game switch
            {
                MinigameType.Quarry => 5102,
                MinigameType.Sawmill => 5103,
                MinigameType.FiringRange => 5105,
                MinigameType.Fishpond => 5104,
                MinigameType.Typewriter => 5113,
                _ => 5112
            };
            await mg.Session.SendPacketAsync(new MloStPacket { Game = (short)game });
        }
    }
}
