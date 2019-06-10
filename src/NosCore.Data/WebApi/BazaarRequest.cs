using System;
using System.Collections.Generic;
using System.Text;

namespace NosCore.Data.WebApi
{
    public class BazaarRequest
    {
        public Guid ItemInstanceId { get; set; }
        public long CharacterId { get; set; }
        public string CharacterName { get; set; }
        public bool HasMedal { get; set; }
        public long Price { get; set; }
        public bool IsPackage { get; set; }
        public short Duration { get; set; }
        public short Amount { get; set; }
    }
}
