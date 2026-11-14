//  __  _  __    __   ___ __  ___ ___
// |  \| |/__\ /' _/ / _//__\| _ \ __|
// | | ' | \/ |`._`.| \_| \/ | v / _|
// |_|\__|\__/ |___/ \__/\__/|_|_\___|
//

using Microsoft.Extensions.Options;
using NodaTime;
using NosCore.Core.Configuration;
using NosCore.Data.Enumerations;
using NosCore.GameObject.Ecs.Extensions;
using NosCore.GameObject.Networking.ClientSession;
using NosCore.GameObject.Services.BattleService;
using NosCore.GameObject.Services.ItemGenerationService.Item;
using NosCore.GameObject.Services.SpecialistCardService;
using NosCore.Networking;
using NosCore.Packets.ClientPackets.Specialists;
using NosCore.Packets.Enumerations;
using NosCore.Packets.ServerPackets.UI;
using System;
using System.Threading.Tasks;

namespace NosCore.GameObject.Services.TransformationService;

public sealed record SpecialistOnCooldown(short SecondsLeft);

public union SpecialistTransformationResult(SpecialistOnCooldown, SpecialistTransformationOutcome);

public sealed record SpecialistPointsSpent(SpecialistInstance Card);

public union SpecialistPointsResult(SpecialistPointsSpent, SpecialistPointsOutcome);

public interface ISpecialistTransformationService
{
    // Takes the worn specialist off, starts the transformation bar, or, once the bar has run,
    // transforms.
    Task<SpecialistTransformationResult> ToggleAsync(ClientSession session, bool barCompleted);

    Task<SpecialistPointsResult> SpendPointsAsync(ClientSession session, long? transportId, short damage,
        short defence, short element, short hp);

    // Spends a second's worth of specialist points while the transformed character is fighting,
    // and ends the transformation once both pools are empty.
    Task DrainPointsAsync(ClientSession session);
}

public sealed class SpecialistTransformationService(IClock clock, ITransformationService transformationService,
        ISpecialistCardService specialistCardService, IVitalityService vitalityService,
        IOptions<WorldConfiguration> worldConfiguration)
    : ISpecialistTransformationService
{
    private static readonly Duration CombatWindow = Duration.FromSeconds(15);

    public async Task<SpecialistTransformationResult> ToggleAsync(ClientSession session, bool barCompleted)
    {
        var character = session.Character;
        if (character.IsSitting)
        {
            return SpecialistTransformationOutcome.Ignored;
        }

        if (character.InventoryService.LoadBySlotAndType((byte)EquipmentType.Sp, NoscorePocketType.Wear)?.ItemInstance
            is not SpecialistInstance specialistInstance)
        {
            return SpecialistTransformationOutcome.NoSpecialistCardEquipped;
        }

        if (character.IsVehicled)
        {
            return SpecialistTransformationOutcome.InVehicle;
        }

        var currentRunningSeconds = (clock.GetCurrentInstant() - character.LastSp).TotalSeconds;

        if (character.UseSp)
        {
            character.LastSp = clock.GetCurrentInstant();
            await transformationService.RemoveSpAsync(session);
            return SpecialistTransformationOutcome.Handled;
        }

        if ((character.SpPoint == 0) && (character.SpAdditionPoint == 0))
        {
            return SpecialistTransformationOutcome.NoPoints;
        }

        if (currentRunningSeconds < character.SpCooldown)
        {
            return new SpecialistOnCooldown((short)(character.SpCooldown - (int)Math.Round(currentRunningSeconds)));
        }

        if (barCompleted)
        {
            if (character.TryCompleteDelay(DelayPacketType.Locomotion, specialistInstance.Id, clock.GetCurrentInstant()))
            {
                await transformationService.ChangeSpAsync(session);
            }

            return SpecialistTransformationOutcome.Handled;
        }

        await character.StartDelayAsync(new DelayPacket
        {
            Type = DelayPacketType.Locomotion,
            Delay = 5000,
            Packet = new SpTransformPacket { Type = SlPacketType.WearSpAndTransform }
        }, specialistInstance.Id, clock.GetCurrentInstant());
        await character.MapInstance.SendPacketAsync(new GuriPacket
        {
            Type = GuriPacketType.Dance,
            Argument = 1,
            EntityId = character.CharacterId
        });
        return SpecialistTransformationOutcome.Handled;
    }

    public async Task<SpecialistPointsResult> SpendPointsAsync(ClientSession session, long? transportId, short damage,
        short defence, short element, short hp)
    {
        var character = session.Character;
        if (!character.UseSp
            || character.InventoryService.LoadBySlotAndType((byte)EquipmentType.Sp, NoscorePocketType.Wear)?.ItemInstance
                is not SpecialistInstance specialist
            || transportId != specialist.TransportId)
        {
            return SpecialistPointsOutcome.CardNotWorn;
        }

        if (!specialistCardService.TrySpendPoints(specialist, damage, defence, element, hp))
        {
            return SpecialistPointsOutcome.Refused;
        }

        await vitalityService.RefreshAndNotifyAsync(character);
        return new SpecialistPointsSpent(specialist);
    }

    public async Task DrainPointsAsync(ClientSession session)
    {
        var character = session.Character;
        var now = clock.GetCurrentInstant();
        if (!character.UseSp || character.IsVehicled || now - character.LastAttack > CombatWindow
            || character.InventoryService.LoadBySlotAndType((byte)EquipmentType.Sp, NoscorePocketType.Wear)?.ItemInstance
                is not SpecialistInstance { Item.SpType: > 0 } specialist)
        {
            return;
        }

        var cost = specialist.Item.SpType;
        var fromBasic = Math.Min(cost, character.SpPoint);
        character.SpPoint -= fromBasic;
        character.SpAdditionPoint = Math.Max(0, character.SpAdditionPoint - (cost - fromBasic));
        if (character.SpPoint == 0 && character.SpAdditionPoint == 0)
        {
            character.LastSp = now;
            await transformationService.RemoveSpAsync(session);
        }

        await session.SendPacketAsync(character.GenerateSpPoint(worldConfiguration));
    }
}
