using NosCore.Data;
using NosCore.GameObject.Item;
using System;
using System.Collections.Generic;
using System.Text;

namespace NosCore.GameObject.Services
{
    public interface IItemCreatorService
    {
        ItemInstance Create(short itemToCreateVNum, long characterId, short amount = 1, sbyte rare = 0,
            byte upgrade = 0, byte design = 0);
        ItemInstance Convert(ItemInstanceDTO k);
    }
}
