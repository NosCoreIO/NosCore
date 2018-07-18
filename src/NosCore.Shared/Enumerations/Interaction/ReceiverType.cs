namespace NosCore.Shared.Enumerations.Interaction
{
    public enum ReceiverType : byte
    {
        All = 1,
        AllExceptMe = 2,
        AllInRange = 3,
        OnlySomeone = 4,
        AllNoEmoBlocked = 5,
        AllNoHeroBlocked = 6,
        Group = 7,
        AllExceptGroup = 8
    }
}