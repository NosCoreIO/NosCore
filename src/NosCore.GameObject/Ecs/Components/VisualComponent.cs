namespace NosCore.GameObject.Ecs.Components;

public record struct VisualComponent(short Morph, byte MorphUpgrade, short MorphDesign, byte MorphBonus, bool NoAttack, bool NoMove, bool IsSitting);
