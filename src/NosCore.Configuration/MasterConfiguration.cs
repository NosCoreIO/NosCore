namespace NosCore.Configuration
{
    public class MasterConfiguration : WebApiConfiguration
    {
        public SqlConnectionConfiguration Database { get; set; }
    }
}