using System.Diagnostics.CodeAnalysis;

namespace NosCore.Shared.Enumerations
{
    [SuppressMessage("ReSharper", "UnusedMember.Global")]
    public enum MessageType : byte
    {
        Whisper = 0,
        PrivateChat = 1,
        Family = 2,
        Shout = 3,
        FamilyChat = 4,
        WhisperGm = 5
    }
}