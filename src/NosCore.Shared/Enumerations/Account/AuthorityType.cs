namespace NosCore.Shared.Enumerations.Account
{
    public enum AuthorityType : short
    {
        Closed = -3,
        Banned = -2,
        Unconfirmed = -1,
        User = 0,
        Moderator = 1,
        GameMaster = 2,
        Administrator = 3,
        Root = 4
    }
}