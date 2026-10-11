using System.Threading;
using NodaTime;
using NosCore.GameObject.Services.DigestionService;

namespace NosCore.GameObject.Ecs.Components;

public record struct DigestionComponent(DigestionState Digestion);

// Shared by reference: item use fills it from a message handler while the map loop drains it.
public sealed class DigestionState
{
    public DigestionSchedule Food { get; } = new();

    public DigestionSchedule Snack { get; } = new();

    public Instant LastTick { get; set; }

    public SemaphoreSlim Lock { get; } = new(1, 1);
}
