namespace NosCore.GameObject.Ecs.Components;

// Where the character stood before its latest accepted walk packet. The client turns a walker
// without a dir packet, and its last packet of a walk often steps back a cell or two, so the
// facing is read over the last two packets rather than the last one.
public record struct WalkTrailComponent(short PreviousWalkX, short PreviousWalkY);
