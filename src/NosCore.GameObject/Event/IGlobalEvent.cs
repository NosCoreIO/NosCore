using System;
using System.Collections.Generic;
using System.Text;

namespace NosCore.GameObject.Event
{
    public interface IGlobalEvent
    {
        TimeSpan Delay { get; set; }

        void Execution();
    }
}
