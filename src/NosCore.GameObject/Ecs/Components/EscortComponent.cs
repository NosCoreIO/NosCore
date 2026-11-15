namespace NosCore.GameObject.Ecs.Components;

// An escort waits until a player comes near, then follows that player and takes its steps from that player's ptctl.
public record struct EscortComponent(bool IsEscort, long LeaderId);
