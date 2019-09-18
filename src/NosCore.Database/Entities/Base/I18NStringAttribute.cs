using System;
using System.Collections.Generic;
using System.Text;

namespace NosCore.Database.Entities.Base
{
    public class I18NStringAttribute : Attribute
    {
        public Type Type { get; set; }
        public I18NStringAttribute(Type type)
        {
            Type = type;
        }
    }
}
