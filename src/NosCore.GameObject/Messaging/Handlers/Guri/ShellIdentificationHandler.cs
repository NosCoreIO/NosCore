//  __  _  __    __   ___ __  ___ ___
// |  \| |/__\ /' _/ / _//__\| _ \ __|
// | | ' | \/ |`._`.| \_| \/ | v / _|
// |_|\__|\__/ |___/ \__/\__/|_|_\___|
//

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using JetBrains.Annotations;
using NosCore.Algorithm.ShellService;
using NosCore.Data.Enumerations;
using NosCore.Data.Enumerations.Items;
using NosCore.GameObject.Ecs.Extensions;
using NosCore.GameObject.Messaging.Events;
using NosCore.GameObject.Services.InventoryService;
using NosCore.GameObject.Services.ItemGenerationService.Item;
using NosCore.GameObject.Services.ShellOptionService;
using NosCore.Packets.Enumerations;
using NosCore.Packets.Interfaces;
using NosCore.Packets.ServerPackets.UI;

namespace NosCore.GameObject.Messaging.Handlers.Guri
{
    [UsedImplicitly]
    public sealed class ShellIdentificationHandler(IShellOptionService shellOptionService, IShellService shellService)
    {
        [UsedImplicitly]
        public async Task Handle(GuriPacketReceivedEvent evt)
        {
            var packet = evt.Packet;
            if (packet.Type != GuriPacketType.ShellIdentification || packet.VisualId is not { } slot
                || slot is < short.MinValue or > short.MaxValue)
            {
                return;
            }

            var session = evt.ClientSession;
            var inventory = session.Character.InventoryService;
            if (inventory.LoadBySlotAndType((short)slot, NoscorePocketType.Equipment)?.ItemInstance
                is not WearableInstance { Item.ItemType: ItemType.Shell } shell)
            {
                return;
            }

            if (shellOptionService.Load(shell.Id).Count > 0)
            {
                await SendAsync(evt, Game18NConstString.OptionsAlreadyIdentified);
                return;
            }

            var options = shellOptionService.Roll(shell);
            if (options.Count == 0)
            {
                await SendAsync(evt, Game18NConstString.NoEffect);
                return;
            }

            var pearls = inventory.Values
                .Where(i => i.ItemInstance.Item.Effect == ItemEffectType.ShellIdentification)
                .OrderBy(i => i.Slot)
                .ToList();
            var cost = shellService.GetIdentificationCost(shell.Upgrade, (byte)shell.Rare);
            if (pearls.Sum(i => i.ItemInstance.Amount) < cost)
            {
                await SendAsync(evt, Game18NConstString.NotEnoughPearls);
                return;
            }

            await shellOptionService.SaveAsync(shell.Id, options);
            var packets = new List<IPacket>();
            foreach (var pearl in pearls)
            {
                if (cost <= 0)
                {
                    break;
                }

                var taken = (short)Math.Min(cost, pearl.ItemInstance.Amount);
                inventory.RemoveItemAmountFromInventory(taken, pearl.ItemInstanceId);
                cost -= taken;
                packets.Add((pearl.ItemInstance.Amount > 0 ? pearl : null).GeneratePocketChange(pearl.Type.ToPocketType(), pearl.Slot));
            }

            packets.Add(new MsgiPacket { Type = MessageType.Default, Message = Game18NConstString.OptionsIdentified });
            await session.SendPacketsAsync(packets);
        }

        private static Task SendAsync(GuriPacketReceivedEvent evt, Game18NConstString message) =>
            evt.ClientSession.SendPacketAsync(new MsgiPacket { Type = MessageType.Default, Message = message });
    }
}
