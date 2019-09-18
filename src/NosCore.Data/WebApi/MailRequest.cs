using NosCore.Data.Dto;

namespace NosCore.Data.WebApi
{
    public class MailRequest
    {
        public MailDto Mail { get; set; }
        public short? VNum { get; set; }
        public short? Amount { get; set; }
        public sbyte? Rare { get; set; }
        public byte? Upgrade { get; set; }
    }
}
