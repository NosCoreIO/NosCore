namespace NosCore.GameObject.Ecs.Components;

public record struct VisualComponent(short Morph, byte MorphUpgrade, byte MorphDesign, byte MorphBonus, bool NoAttack, bool NoMove, bool IsSitting);
