using System.Diagnostics.CodeAnalysis;

namespace NosCore.Shared.Enumerations.Interaction
{
	[SuppressMessage("ReSharper", "UnusedMember.Global")]
    public enum TalentArenaOptionType : byte
	{
		Watch = 0,
		Nothing = 1,
		Call = 2,
		WatchAndCall = 3
	}
}