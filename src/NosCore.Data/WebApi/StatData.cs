using System;
using System.Collections.Generic;
using System.Text;
using NosCore.Shared.Enumerations;

namespace NosCore.Data.WebApi
{
    public class StatData
    {
        public Character Character { get; set; }

        public UpdateStatActionType ActionType { get; set; }

        public byte Data { get; set; }
    }
}
