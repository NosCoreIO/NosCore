namespace NosCore.Core.Serializing
{
    public abstract class PacketDefinition
    {
        #region Properties

        public string OriginalContent { get; set; }

        public string OriginalHeader { get; set; }

        #endregion
    }
}