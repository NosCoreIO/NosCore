using System.Diagnostics.CodeAnalysis;

namespace NosCore.Shared.Enumerations.Family
{
	[SuppressMessage("ReSharper", "UnusedMember.Global")]
    public enum FamilyAuthority : byte
	{
		Head = 0,
		Assistant = 1,
		Manager = 2,
		Member = 3
	}
}