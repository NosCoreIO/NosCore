using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using NosCore.GameObject.ComponentEntities.Interfaces;

namespace NosCore.GameObject.Services.SaveService
{
    public interface ISaveService
    {
        Task SaveAsync(ICharacterEntity character);
    }
}
