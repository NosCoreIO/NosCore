using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using NosCore.Data.WebApi;

namespace NosCore.GameObject.HttpClients
{
    public interface IBlacklistHttpClient
    {
        List<CharacterRelationStatus> GetCharacterRelationStatus(long visualEntityVisualId);
    }
}
