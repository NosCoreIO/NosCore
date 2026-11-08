//  __  _  __    __   ___ __  ___ ___
// |  \| |/__\ /' _/ / _//__\| _ \ __|
// | | ' | \/ |`._`.| \_| \/ | v / _|
// |_|\__|\__/ |___/ \__/\__/|_|_\___|
//

using NosCore.Data.Dto;
using NosCore.Data.StaticEntities;
using NosCore.Core.Concurrency;
using NosCore.GameObject.Ecs.Interfaces;
using System;
using System.Collections.Concurrent;
using System.Linq;

namespace NosCore.GameObject.Services.ShopService
{
    public class Shop : ShopDto
    {
        private int? _size;

        public ConcurrentDictionary<int, ShopItem> ShopItems { get; set; } = new();

        public ICharacterEntity? OwnerCharacter { get; set; }
        public long Sell { get; internal set; }

        // Buyers and the owner closing the shop serialise on this, so a sale is checked and
        // transferred as one step.
        public AsyncLock Lock { get; } = new();

        public bool IsClosed { get; internal set; }

        public IDisposable? DialogSubscription { get; set; }

        public int Size
        {
            get => _size ?? ShopItems.Values.Select(s => s.Slot).DefaultIfEmpty().Max() + 1;
            set => _size = value;
        }
        public I18NString Name { get; set; } = new();
    }
}
