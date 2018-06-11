using NosCore.Core.Networking;
using NosCore.Data;
using System;
using System.Collections.Generic;
using System.Text;

namespace NosCore.GameObject.Networking
{
    public interface IClientSession : INetworkClient
    {
        bool HasCurrentMapInstance { get; }
        AccountDTO Account { get; set; }
        bool HasSelectedCharacter { get; }
        Character Character { get; }
        bool IsAuthenticated { get; set; }

        void SetCharacter(Character character);
        void InitializeAccount(AccountDTO accountDTO);
    }
}
