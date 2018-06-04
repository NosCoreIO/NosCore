namespace NosCore.Shared.Account
{
    public enum AuthorityType : short
    {
        Closed = -3,
        Banned = -2,
        Unconfirmed = -1,
        User = 0,
        Moderator = 1,
        GameMaster = 2
    }
}