using System;

namespace OpenNosCore.Core.Serializing
{
    [AttributeUsage(AttributeTargets.Method, AllowMultiple = true)]
    public class PacketAttribute : Attribute
    {
        #region Properties

        public int Amount { get; }

        public string Header { get; }

        #endregion
    }
}