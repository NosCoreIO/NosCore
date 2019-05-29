using System;
using System.Collections.Generic;
using System.Text;

namespace NosCore.Data.WebApi
{
    public class CharacterRelationStatus
    {
        public long CharacterRelationId { get; set; }
        public long CharacterId { get; set; }
        public string CharacterName { get; set; }
        public bool IsConnected { get; set; }
    }
}
