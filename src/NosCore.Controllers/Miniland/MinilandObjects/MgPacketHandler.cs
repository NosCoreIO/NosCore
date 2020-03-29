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

using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using NosCore.Packets.ClientPackets.Miniland;
using NosCore.Packets.Enumerations;
using NosCore.Packets.Interfaces;
using NosCore.Packets.ServerPackets.Miniland;
using NosCore.Packets.ServerPackets.UI;
using NosCore.Core.I18N;
using NosCore.Data.Enumerations.I18N;
using NosCore.GameObject;
using NosCore.GameObject.ComponentEntities.Extensions;
using NosCore.GameObject.Helper;
using NosCore.GameObject.Networking.ClientSession;
using NosCore.GameObject.Networking.Group;
using NosCore.GameObject.Providers.InventoryService;
using NosCore.GameObject.Providers.ItemProvider;
using NosCore.GameObject.Providers.MapInstanceProvider;
using NosCore.GameObject.Providers.MinilandProvider;

namespace NosCore.PacketHandlers.Miniland.MinilandObjects
{
    public class MgPacketHandler : PacketHandler<MinigamePacket>, IWorldPacketHandler
    {
        private readonly IItemProvider _itemProvider;
        private readonly IMinilandProvider _minilandProvider;
        private ClientSession? _clientSession;
        private MinigamePacket? _minigamePacket;
        private GameObject.Providers.MinilandProvider.Miniland? _miniland;
        private MapDesignObject? _minilandObject;

        public MgPacketHandler(IMinilandProvider minilandProvider, IItemProvider itemProvider)
        {
            _minilandProvider = minilandProvider;
            _itemProvider = itemProvider;
        }

        public override async Task Execute(MinigamePacket minigamePacket, ClientSession clientSession)
        {
            _clientSession = clientSession;
            _minigamePacket = minigamePacket;
            _miniland = _minilandProvider.GetMiniland(clientSession.Character.CharacterId);
            _minilandObject =
                clientSession.Character.MapInstance!.MapDesignObjects.Values.FirstOrDefault(s =>
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
                    await Play(game).ConfigureAwait(false);
                    break;

                case 2:
                    await BroadcastEffect().ConfigureAwait(false);
                    break;

                case 3:
                    await ShowBoxLevels(game).ConfigureAwait(false);
                    break;

                case 4:
                    await SelectGift().ConfigureAwait(false);
                    break;

                case 5:
                    await ShowMinilandManagment().ConfigureAwait(false);
                    break;

                case 6:
                    await Refill().ConfigureAwait(false);
                    break;

                case 7:
                    await ShowGifts().ConfigureAwait(false);
                    break;

                case 8:
                    await OpenGiftBatch().ConfigureAwait(false);
                    break;

                case 9:
                    await UseCoupon().ConfigureAwait(false);
                    break;
            }
        }

        private async Task UseCoupon()
        {
            var item = _clientSession!.Character.InventoryService.Select(s => s.Value)
                .Where(s => (s.ItemInstance?.ItemVNum == 1269) || (s.ItemInstance?.ItemVNum == 1271)).OrderBy(s => s.Slot)
                .FirstOrDefault();
            if (item != null)
            {
                var point = item.ItemInstance!.ItemVNum == 1269 ? 300 : 500;
                _clientSession.Character.InventoryService.RemoveItemAmountFromInventory(1, item.ItemInstance.Id);
                _minilandObject!.InventoryItemInstance!.ItemInstance!.DurabilityPoint += point;
                await _clientSession.SendPacket(new InfoPacket
                {
                    Message = string.Format(GameLanguage.Instance.GetMessageFromKey(LanguageKey.REFILL_MINIGAME,
                        _clientSession.Account.Language), point)
                }).ConfigureAwait(false);
                await ShowMinilandManagment().ConfigureAwait(false);
            }
        }

        private Task ShowMinilandManagment()
        {
            return _clientSession!.SendPacket(new MloMgPacket
            {
                MinigameVNum = _minigamePacket!.MinigameVNum,
                MinilandPoint = _miniland!.MinilandPoint,
                Unknown1 = 0,
                Unknown2 = 0,
                DurabilityPoint = _minilandObject!.InventoryItemInstance!.ItemInstance!.DurabilityPoint,
                MinilandObjectPoint = _minilandObject.InventoryItemInstance.ItemInstance.Item!.MinilandObjectPoint
            });
        }

        private async Task OpenGiftBatch()
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
                if (gift != null)
                {
                    if (gifts.Any(o => o.VNum == gift.VNum))
                    {
                        gifts.First(o => o.Amount == gift.Amount).Amount += gift.Amount;
                    }
                    else
                    {
                        gifts.Add(gift);
                    }
                }
            }

            var str = string.Empty;
            var list = new List<MloPmgSubPacket>();
            for (var i = 0; i < 9; i++)
            {
                if (gifts.Count > i)
                {
                    var item = _itemProvider.Create(gifts.ElementAt(i).VNum, gifts.ElementAt(i).Amount);
                    var inv = _clientSession!.Character.InventoryService.AddItemToPocket(
                        InventoryItemInstance.Create(item, _clientSession.Character.CharacterId));
                    if (inv != null && inv.Count != 0)
                    {
                        await _clientSession.SendPacket(_clientSession.Character.GenerateSay(
                            $"{GameLanguage.Instance.GetMessageFromKey(LanguageKey.ITEM_ACQUIRED, _clientSession.Account.Language)}: {item.Item!.Name[_clientSession.Account.Language]} x {amount}",
                            SayColorType.Green)).ConfigureAwait(false);
                    }

                    list.Add(new MloPmgSubPacket
                    { BoxVNum = gifts.ElementAt(i).VNum, BoxAmount = gifts.ElementAt(i).Amount });
                }
                else
                {
                    list.Add(new MloPmgSubPacket { BoxVNum = 0, BoxAmount = 0 });
                }
            }

            await ShowGifts(list.ToArray()).ConfigureAwait(false);
        }

        private async Task ShowGifts()
        {
            await ShowGifts(new[]
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
            }).ConfigureAwait(false);
        }

        private async Task ShowGifts(MloPmgSubPacket[] array)
        {
            await _clientSession!.SendPacket(new MloPmgPacket
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
            }).ConfigureAwait(false);
        }

        private async Task Refill()
        {
            if (_minigamePacket?.Point == null)
            {
                return;
            }

            if (_clientSession?.Character.Gold > _minigamePacket.Point)
            {
                _clientSession.Character.Gold -= (int)_minigamePacket.Point;
                await _clientSession.SendPacket(_clientSession.Character.GenerateGold()).ConfigureAwait(false);
                _minilandObject!.InventoryItemInstance!.ItemInstance!.DurabilityPoint +=
                    (int)(_minigamePacket.Point / 100);
                await _clientSession.SendPacket(new InfoPacket
                {
                    Message = string.Format(GameLanguage.Instance.GetMessageFromKey(LanguageKey.REFILL_MINIGAME,
                        _clientSession.Account.Language), (int)(_minigamePacket.Point / 100))
                }).ConfigureAwait(false);
                await ShowMinilandManagment().ConfigureAwait(false);
            }
        }

        private async Task SelectGift()
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

            await _clientSession!.SendPacket(new MloRwPacket { Amount = obj.Amount, VNum = obj.VNum }).ConfigureAwait(false);
            // _clientSession.SendPacket(new MlptPacket {_miniland.MinilandPoint, 100});
            var inv = _clientSession.Character.InventoryService.AddItemToPocket(InventoryItemInstance.Create(
                _itemProvider.Create(obj.VNum,
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

        private async Task ShowBoxLevels(byte game)
        {
            _miniland!.CurrentMinigame = 0;
            await _clientSession!.Character.MapInstance!.SendPacket(new GuriPacket
            {
                Type = GuriPacketType.Unknow2,
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

            await _clientSession.SendPacket(level != -1
                ? new MloLvPacket { Level = level }
                : (IPacket)new MinigamePacket
                    { Type = 3, Id = game, MinigameVNum = _minigamePacket!.MinigameVNum, Unknown = 0, Point = 0 }).ConfigureAwait(false);
        }

        private Task BroadcastEffect()
        {
            _miniland!.CurrentMinigame = 0;
            return _clientSession!.Character.MapInstance!.SendPacket(new GuriPacket
            {
                Type = GuriPacketType.Unknow2,
                Value = 1,
                EntityId = _clientSession.Character.CharacterId
            });
        }

        private async Task Play(byte game)
        {
            if (_minilandObject!.InventoryItemInstance!.ItemInstance!.DurabilityPoint <= 0)
            {
                await _clientSession!.SendPacket(new MsgPacket
                {
                    Message = GameLanguage.Instance.GetMessageFromKey(LanguageKey.NOT_ENOUGH_DURABILITY_POINT,
                        _clientSession.Account.Language)
                }).ConfigureAwait(false);
                return;
            }

            if (_miniland == null || _miniland.MinilandPoint <= 0)
            {
                await _clientSession!.SendPacket(new QnaPacket
                {
                    YesPacket = new MinigamePacket
                    {
                        Type = 1,
                        Id = 7,
                        MinigameVNum = 3125,
                        Point = 1,
                        Unknown = 1
                    },
                    Question = GameLanguage.Instance.GetMessageFromKey(LanguageKey.NOT_ENOUGH_MINILAND_POINT,
                        _clientSession.Account.Language)
                }).ConfigureAwait(false);
                return;
            }

            await _clientSession!.Character.MapInstance!.SendPacket(new GuriPacket
            {
                Type = GuriPacketType.Unknow,
                Value = 1,
                EntityId = _clientSession.Character.CharacterId
            }).ConfigureAwait(false);
            _miniland.CurrentMinigame = (short)(game == 0 ? 5102 : game == 1 ? 5103 : game == 2 ? 5105 : game == 3
                ? 5104 : game == 4 ? 5113 : 5112);
            await _clientSession.SendPacket(new MloStPacket { Game = game }).ConfigureAwait(false);
        }
    }
}