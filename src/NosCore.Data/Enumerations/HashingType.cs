namespace NosCore.Data.Enumerations
{
    public enum HashingType
    {
        Sha512,
        //if your care about security use Pbkdf2 (bcrypt doesn't have a approved package for c#)
        //PBKDF2-HMAC-SHA512-150000
        Pbkdf2,
        BCrypt
    }
}
