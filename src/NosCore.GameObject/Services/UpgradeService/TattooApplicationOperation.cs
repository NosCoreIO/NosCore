//  __  _  __    __   ___ __  ___ ___
// |  \| |/__\ /' _/ / _//__\| _ \ __|
// | | ' | \/ |`._`.| \_| \/ | v / _|
// |_|\__|\__/ |___/ \__/\__/|_|_\___|
//

using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using JetBrains.Annotations;
using NosCore.Core.I18N;
using NosCore.Data.Enumerations;
using NosCore.Data.Enumerations.Interaction;
using NosCore.Data.Enumerations.Items;
using NosCore.Data.StaticEntities;
using NosCore.GameObject.Ecs.Extensions;
using NosCore.GameObject.Networking.ClientSession;
using NosCore.GameObject.Services.BattleService;
using NosCore.GameObject.Services.InventoryService;
using NosCore.GameObject.Services.NpcDialogService;
using NosCore.GameObject.Services.SkillService;
using NosCore.Packets.ClientPackets.Player;
using NosCore.Packets.Enumerations;
using NosCore.Packets.Interfaces;
using NosCore.Packets.ServerPackets.Shop;
using NosCore.Packets.ServerPackets.UI;

namespace NosCore.GameObject.Services.UpgradeService;

// The tattooist inks the pattern placed in the window as one of its loa's tattoos, picked at
// random, and the pattern is used up with the window's gold and materials. A character wears two
// tattoos at most, of two loas.
[UsedImplicitly]
public sealed class TattooApplicationOperation(IRandomProvider random, ISkillService skillService, List<SkillDto> skills,
        List<UpgradeCostDto> costs, List<UpgradeCostMaterialDto> costMaterials, IGameLanguageLocalizer localizer)
    : IUpgradeOperation
{
    private const int MaximumTattoos = 2;

    // The skill argument type: the client writes the name of the skill whose vnum it is given.
    private const byte SkillArgument = 5;

    public UpgradePacketType Kind => UpgradePacketType.ApplyTattoo;

    public NpcDialogWindow Window => NpcDialogWindow.Upgrade;

    public NpcService? Service => NpcService.TattooApplication;

    public async Task<IReadOnlyList<IPacket>> ExecuteAsync(ClientSession session, UpgradePacket packet)
    {
        var character = session.Character;
        if (character.InExchangeOrShop
            || character.InventoryService.LoadBySlotAndType(packet.Slot, (NoscorePocketType)packet.InventoryType)
                is not { ItemInstance.Item: { Effect: ItemEffectType.TattooPattern } pattern } patternSlot)
        {
            return [];
        }

        var designs = skills.Where(s => s.IsTattooOfLoa(pattern.EffectValue)).ToList();
        if (designs.Count == 0 || costs.FirstOrDefault(s => s.Kind == Kind) is not { } cost)
        {
            return [];
        }

        var worn = character.Skills.Values.Select(s => s.Skill).Where(s => s.IsTattoo()).ToList();
        if (worn.Any(s => s.CastId == designs[0].CastId))
        {
            return Refuse(Game18NConstString.AlreadyHaveTatooType);
        }

        if (worn.Count >= MaximumTattoos)
        {
            return Refuse(Game18NConstString.CanNotHaveOtherTatoo);
        }

        var materials = costMaterials.Where(s => s.UpgradeCostId == cost.UpgradeCostId).ToList();
        if (character.Gold < cost.Gold)
        {
            return [new InfoiPacket { Message = Game18NConstString.NotEnoughGold }];
        }

        if (materials.Any(s => character.InventoryService.CountItem(s.ItemVNum) < s.Amount))
        {
            return [new InfoiPacket { Message = Game18NConstString.NotEnoughIngredients }];
        }

        // Stored before anything is spent, and everything is spent before anything is sent: a failed
        // write costs nothing and a failed send cannot leave the tattoo unpaid.
        var tattoo = designs[random.Next(0, designs.Count)];
        await skillService.LearnTattooAsync(character, tattoo);
        var left = character.InventoryService.RemoveItemAmountFromInventory(1, patternSlot.ItemInstanceId);
        var spent = materials.SelectMany(s => character.InventoryService.RemoveItemAmountByVNum(s.ItemVNum, s.Amount)).ToList();
        await character.RemoveGoldAsync(cost.Gold, localizer);
        return
        [
            new MsgiPacket
            {
                Type = MessageType.Default,
                Message = Game18NConstString.TattooAplied,
                ArgumentType = SkillArgument,
                Game18NArguments = { tattoo.SkillVNum },
            },
            new ShopEndPacket { Type = ShopEndPacketType.CloseSubWindow },
            left.GeneratePocketChange(patternSlot.Type.ToPocketType(), patternSlot.Slot),
            .. spent,
            character.GenerateSki(character.UseSp),
        ];
    }

    private static IPacket[] Refuse(Game18NConstString message) =>
    [
        new MsgiPacket { Type = MessageType.Default, Message = message },
        new ShopEndPacket { Type = ShopEndPacketType.CloseSubWindow },
    ];
}
