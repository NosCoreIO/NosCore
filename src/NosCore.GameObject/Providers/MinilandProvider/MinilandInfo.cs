using NosCore.Data.Enumerations.Character;
using System;

namespace NosCore.GameObject.Providers.MinilandProvider
{
    public class MinilandInfo
    {
        public Guid MapInstanceId { get; set; }

        public MinilandState State { get; set; }
    }
}
