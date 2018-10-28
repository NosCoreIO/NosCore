using System;

namespace NosCore.GameObject.Event
{
    public interface IGlobalEvent
    {
        TimeSpan Delay { get; set; }

        void Execution();
    }
}
