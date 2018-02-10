using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Text;

namespace OpenNosCore.GameObject
{
    public interface IExperiencedEntity
    {
        byte Level { get; set; }

        long LevelXp { get; set; }
    }
}
