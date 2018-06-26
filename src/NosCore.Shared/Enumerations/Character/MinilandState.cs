using System.Diagnostics.CodeAnalysis;

namespace NosCore.Shared.Enumerations.Character
{
	[SuppressMessage("ReSharper", "UnusedMember.Global")]
    public enum MinilandState : byte
	{
		Open = 0,
		Private = 1,
		Lock = 2
	}
}