using System.Diagnostics.CodeAnalysis;

namespace NosCore.Shared.Enumerations.Group
{
    [SuppressMessage("ReSharper", "UnusedMember.Global")]
    public enum GroupRequestType : byte
    {
        Requested = 0,
        Invited = 1,
        Accepted = 3,
        Declined = 4,
        Sharing = 5,
        AcceptedShare = 6,
        DeclinedShare = 7
    }
}