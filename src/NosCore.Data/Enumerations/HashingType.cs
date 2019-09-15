namespace NosCore.Data.Enumerations
{
    public enum HashingType
    {
        Sha512,
        //if your care about security use Pbkdf2 (bcrypt doesn't have a approved package for c#)
        Pbkdf2,
        BCrypt
    }
}
