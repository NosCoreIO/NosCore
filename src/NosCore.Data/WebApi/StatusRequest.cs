using System;
using System.Collections.Generic;
using System.Text;

namespace NosCore.Data.WebApi
{
    public class StatusRequest
    {
        public long CharacterId { get; set; }
        public bool Status { get; set; }
        public string Name { get; set; }
    }
}
