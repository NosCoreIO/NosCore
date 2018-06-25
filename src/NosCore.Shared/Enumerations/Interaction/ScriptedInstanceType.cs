using System.Diagnostics.CodeAnalysis;

namespace NosCore.Shared.Enumerations.Interaction
{
	[SuppressMessage("ReSharper", "UnusedMember.Global")]
    public enum ScriptedInstanceType : byte
	{
		TimeSpace = 0,
		Raid = 1,
		RaidAct4 = 2
	}
}