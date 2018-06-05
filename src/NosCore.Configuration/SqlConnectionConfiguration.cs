namespace NosCore.Configuration
{
    public class SqlConnectionConfiguration
    {
        public string Host { get; set; }
        public int Port { get; set; }
        public string Database { get; set; }
        public string Username { get; set; }
        public string Password { get; set; }

        public string ConnectionString { get { return $"Host={Host};Port={Port};Database={Database};Username={Username};Password={Password};"; } }
    }
}