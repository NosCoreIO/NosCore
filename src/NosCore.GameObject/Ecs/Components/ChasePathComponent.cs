using System.Collections.Generic;
using NodaTime;

namespace NosCore.GameObject.Ecs.Components;

public sealed record CachedPath(short TargetX, short TargetY, List<(short X, short Y)> Path, Instant LastStepAt,
    bool Unreachable);

public record struct ChasePathComponent(CachedPath? ChasePath);
