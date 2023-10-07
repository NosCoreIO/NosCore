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

using NosCore.GameObject;
using NosCore.GameObject.ComponentEntities.Extensions;
using NosCore.GameObject.Helper;
using NosCore.GameObject.Networking.ClientSession;
using NosCore.GameObject.Services.InventoryService;
using NosCore.GameObject.Services.ItemGenerationService;
using NosCore.GameObject.Services.MapInstanceGenerationService;
using NosCore.GameObject.Services.MinilandService;
using NosCore.Packets.ClientPackets.Miniland;
using NosCore.Packets.Enumerations;
using NosCore.Packets.Interfaces;
using NosCore.Packets.ServerPackets.Miniland;
using NosCore.Packets.ServerPackets.UI;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using NosCore.Networking;
using NosCore.Packets.ServerPackets.Chats;
using NosCore.Shared.Enumerations;


//TODO stop using obsolete
#pragma warning disable 618

namespace NosCore.PacketHandlers.Miniland.MinilandObjects
{
    public class MgPacketHandler(IMinilandService minilandProvider, IItemGenerationService itemProvider)
        : PacketHandler<MinigamePacket>, IWorldPacketHandler
    {
        private ClientSession? _clientSession;
        private MinigamePacket? _minigamePacket;
        private GameObject.Services.MinilandService.Miniland? _miniland;
        private MapDesignObject? _minilandObject;

        public override async Task ExecuteAsync(MinigamePacket minigamePacket, ClientSession clientSession)
        {
            _clientSession = clientSession;
            _minigamePacket = minigamePacket;
            _miniland = minilandProvider.GetMiniland(clientSession.Character.CharacterId);
            _minilandObject =
                clientSession.Character.MapInstance.MapDesignObjects.Values.FirstOrDefault(s =>
                    s.Slot == minigamePacket.Id);
            if ((_minilandObject == null) || (_miniland == null))
            {
                return;
            }

            if (_minilandObject?.InventoryItemInstance?.ItemInstance?.Item?.IsWarehouse != false)
            {
                return;
            }

            var game = (byte)(_minilandObject.InventoryItemInstance.ItemInstance.Item.EquipmentSlot ==
                EquipmentType.MainWeapon
                    ? (4 + _minilandObject.InventoryItemInstance.ItemInstance.ItemVNum) % 10
                    : (int)_minilandObject.InventoryItemInstance.ItemInstance.Item.EquipmentSlot / 3);
            //todo check if enought points

            switch (minigamePacket.Type)
            {
                case 1:
                    await PlayAsync(game).ConfigureAwait(false);
                    break;

                case 2:
                    await BroadcastEffectAsync().ConfigureAwait(false);
                    break;

                case 3:
                    await ShowBoxLevelsAsync(game).ConfigureAwait(false);
                    break;

                case 4:
                    await SelectGiftAsync().ConfigureAwait(false);
                    break;

                case 5:
                    await ShowMinilandManagmentAsync().ConfigureAwait(false);
                    break;

                case 6:
                    await RefillAsync().ConfigureAwait(false);
                    break;

                case 7:
                    await ShowGiftsAsync().ConfigureAwait(false);
                    break;

                case 8:
                    await OpenGiftBatchAsync().ConfigureAwait(false);
                    break;

                case 9:
                    await UseCouponAsync().ConfigureAwait(false);
                    break;
            }
        }

        private async Task UseCouponAsync()
        {
            var item = _clientSession!.Character.InventoryService.Select(s => s.Value)
                .Where(s => (s.ItemInstance?.ItemVNum == 1269) || (s.ItemInstance?.ItemVNum == 1271)).OrderBy(s => s.Slot)
                .FirstOrDefault();
            if (item != null)
            {
                var point = item.ItemInstance!.ItemVNum == 1269 ? 300 : 500;
                _clientSession.Character.InventoryService.RemoveItemAmountFromInventory(1, item.ItemInstance.Id);
                _minilandObject!.InventoryItemInstance!.ItemInstance!.DurabilityPoint += point;
                await _clientSession.SendPacketAsync(new InfoiPacket
                {
                    Message = Game18NConstString.ToppedUpPoints,
                    ArgumentType = 4,
                    Game18NArguments = { point }
                }).ConfigureAwait(false);
                await ShowMinilandManagmentAsync().ConfigureAwait(false);
            }
        }

        private Task ShowMinilandManagmentAsync()
        {
            return _clientSession!.SendPacketAsync(new MloMgPacket
            {
                MinigameVNum = _minigamePacket!.MinigameVNum,
                MinilandPoint = _miniland!.MinilandPoint,
                Unknown1 = 0,
                Unknown2 = 0,
                DurabilityPoint = _minilandObject!.InventoryItemInstance!.ItemInstance!.DurabilityPoint,
                MinilandObjectPoint = _minilandObject.InventoryItemInstance.ItemInstance.Item!.MinilandObjectPoint
            });
        }

        private async Task OpenGiftBatchAsync()
        {
            var amount = 0;
            switch (_minigamePacket!.Point)
            {
                case 0:
                    amount = _minilandObject?.Level1BoxAmount ?? 0;
                    break;

                case 1:
                    amount = _minilandObject?.Level2BoxAmount ?? 0;
                    break;

                case 2:
                    amount = _minilandObject?.Level3BoxAmount ?? 0;
                    break;

                case 3:
                    amount = _minilandObject?.Level4BoxAmount ?? 0;
                    break;

                case 4:
                    amount = _minilandObject?.Level5BoxAmount ?? 0;
                    break;
            }

            var gifts = new List<Gift>();
            for (var i = 0; i < amount; i++)
            {
                var gift = MinilandHelper.Instance.GetMinilandGift(_minigamePacket.MinigameVNum,
                    _minigamePacket.Point ?? 0);
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
                    var inv = _clientSession!.Character.InventoryService.AddItemToPocket(
                        InventoryItemInstance.Create(item, _clientSession.Character.CharacterId));
                    if (inv != null && inv.Count != 0)
                    {
                        await _clientSession.SendPacketAsync(new SayiPacket
                        {
                            VisualType = VisualType.Player,
                            VisualId = _clientSession.Character.CharacterId,
                            Type = SayColorType.Yellow,
                            Message = Game18NConstString.ReceivedThisItem,
                            ArgumentType = 2,
                            Game18NArguments = { item.Item.VNum.ToString(), amount }
                        }).ConfigureAwait(false);
                    }

                    list.Add(new MloPmgSubPacket
                    { BoxVNum = gifts.ElementAt(i).VNum, BoxAmount = gifts.ElementAt(i).Amount });
                }
                else
                {
                    list.Add(new MloPmgSubPacket { BoxVNum = 0, BoxAmount = 0 });
                }
            }

            await ShowGiftsAsync(list.ToArray()).ConfigureAwait(false);
        }

        private Task ShowGiftsAsync()
        {
            return ShowGiftsAsync(new[]
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

        private Task ShowGiftsAsync(MloPmgSubPacket[] array)
        {
            return _clientSession!.SendPacketAsync(new MloPmgPacket
            {
                MinigameVNum = _minigamePacket!.MinigameVNum,
                MinilandPoint = _miniland!.MinilandPoint,
                LowDurability = _minilandObject!.InventoryItemInstance!.ItemInstance!.DurabilityPoint < 1000,
                IsFull = false,
                MloPmgSubPackets = new[]
                {
                    new MloPmgSubPacket {BoxVNum = 392, BoxAmount = _minilandObject.Level1BoxAmount},
                    new MloPmgSubPacket {BoxVNum = 393, BoxAmount = _minilandObject.Level2BoxAmount},
                    new MloPmgSubPacket {BoxVNum = 394, BoxAmount = _minilandObject.Level3BoxAmount},
                    new MloPmgSubPacket {BoxVNum = 395, BoxAmount = _minilandObject.Level4BoxAmount},
                    new MloPmgSubPacket {BoxVNum = 396, BoxAmount = _minilandObject.Level5BoxAmount}
                }.Concat(array).ToArray()
            });
        }

        private async Task RefillAsync()
        {
            if (_minigamePacket?.Point == null)
            {
                return;
            }

            if (_clientSession?.Character.Gold > _minigamePacket.Point)
            {
                _clientSession.Character.Gold -= (int)_minigamePacket.Point;
                await _clientSession.SendPacketAsync(_clientSession.Character.GenerateGold()).ConfigureAwait(false);
                _minilandObject!.InventoryItemInstance!.ItemInstance!.DurabilityPoint +=
                    (int)(_minigamePacket.Point / 100);
                await _clientSession.SendPacketAsync(new InfoiPacket
                {
                    Message = Game18NConstString.ToppedUpPoints,
                    ArgumentType = 4,
                    Game18NArguments = { (int)_minigamePacket.Point / 100 }
                }).ConfigureAwait(false);
                await ShowMinilandManagmentAsync().ConfigureAwait(false);
            }
        }

        private async Task SelectGiftAsync()
        {
            if (_miniland!.MinilandPoint < 100)
            {
                return;
            }

            var obj = MinilandHelper.Instance.GetMinilandGift(_minigamePacket!.MinigameVNum, _minigamePacket.Point ?? 0);
            if (obj == null)
            {
                return;
            }
            await _clientSession!.SendPacketAsync(new MloRwPacket { Amount = obj.Amount, VNum = obj.VNum }).ConfigureAwait(false);
            // _clientSession.SendPacket(new MlptPacket {_miniland.MinilandPoint, 100});
            var inv = _clientSession.Character.InventoryService.AddItemToPocket(InventoryItemInstance.Create(
                itemProvider.Create(obj.VNum,
                    obj.Amount), _clientSession.Character.CharacterId));
            _miniland.MinilandPoint -= 100;
            if (inv == null || inv.Count == 0)
            {
                //todo add gifts
                //_clientSession.Character.SendGift(_clientSession.Character.CharacterId, obj.VNum, obj.Amount, 0, 0, false);
            }

            if (_miniland.MapInstanceId != _clientSession.Character.MapInstanceId)
            {
                switch (_minigamePacket.Point)
                {
                    case 0:
                        _minilandObject!.Level1BoxAmount++;
                        break;

                    case 1:
                        _minilandObject!.Level2BoxAmount++;
                        break;

                    case 2:
                        _minilandObject!.Level3BoxAmount++;
                        break;

                    case 3:
                        _minilandObject!.Level4BoxAmount++;
                        break;

                    case 4:
                        _minilandObject!.Level5BoxAmount++;
                        break;
                }
            }
        }

        private async Task ShowBoxLevelsAsync(byte game)
        {
            _miniland!.CurrentMinigame = 0;
            await _clientSession!.Character.MapInstance.SendPacketAsync(new GuriPacket
            {
                Type = GuriPacketType.Dance,
                Value = 1,
                EntityId = _clientSession.Character.CharacterId
            }).ConfigureAwait(false);
            short level = -1;
            for (short i = 0; i < MinilandHelper.Instance.MinilandMaxPoint[game].Count(); i++)
            {
                if (_minigamePacket!.Point > MinilandHelper.Instance.MinilandMaxPoint[game][i])
                {
                    level = i;
                }
                else
                {
                    break;
                }
            }

            await _clientSession.SendPacketAsync(level != -1
                ? new MloLvPacket { Level = level }
                : (IPacket)new MinigamePacket
                { Type = 3, Id = game, MinigameVNum = _minigamePacket!.MinigameVNum, Unknown = 0, Point = 0 }).ConfigureAwait(false);
        }

        private Task BroadcastEffectAsync()
        {
            _miniland!.CurrentMinigame = 0;
            return _clientSession!.Character.MapInstance.SendPacketAsync(new GuriPacket
            {
                Type = GuriPacketType.Dance,
                Value = 1,
                EntityId = _clientSession.Character.CharacterId
            });
        }

        private async Task PlayAsync(byte game)
        {
            if (_minilandObject!.InventoryItemInstance!.ItemInstance!.DurabilityPoint <= 0)
            {
                await _clientSession!.SendPacketAsync(new SayiPacket
                {
                    VisualType = VisualType.Player,
                    VisualId = _clientSession.Character.CharacterId,
                    Type = SayColorType.Red,
                    Message = Game18NConstString.NeedToRestoreDurability
                }).ConfigureAwait(false);
                return;
            }

            if (_miniland == null || _miniland.MinilandPoint <= 0)
            {
                await _clientSession!.SendPacketAsync(new QnaiPacket
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
                }).ConfigureAwait(false);
                return;
            }

            await _clientSession!.Character.MapInstance.SendPacketAsync(new GuriPacket
            {
                Type = GuriPacketType.Dance,
                Value = 1,
                EntityId = _clientSession.Character.CharacterId
            }).ConfigureAwait(false);
            _miniland.CurrentMinigame = (short)(game == 0 ? 5102 : game == 1 ? 5103 : game == 2 ? 5105 : game == 3
                ? 5104 : game == 4 ? 5113 : 5112);
            await _clientSession.SendPacketAsync(new MloStPacket { Game = game }).ConfigureAwait(false);
        }
    }
}