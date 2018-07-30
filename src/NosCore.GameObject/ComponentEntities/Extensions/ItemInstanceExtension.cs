using System;
using System.Collections.Generic;
using System.Text;
using NosCore.GameObject.Item;
using NosCore.Shared.Enumerations.Items;

namespace NosCore.GameObject.ComponentEntities.Extensions
{
    public static class ItemInstanceExtension
    {
        public static IvnPacket GeneratePocketChange(this ItemInstance itemInstance, PocketType type, short slot)
        {
            if (itemInstance == null)
            {
                return new IvnPacket
                {
                    Type = type,
                    Slot = slot,
                    VNum = -1,
                    Rare = 0,
                    Upgrade = 0,
                    SecondUpgrade = 0
                };
            }
        }
    }
}
