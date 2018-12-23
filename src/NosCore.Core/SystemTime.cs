using System;
using System.Collections.Generic;
using System.Text;

namespace NosCore.Core
{
    public static class SystemTime
    {
        public static Func<DateTime> Now = () => DateTime.Now;

        public static void Freeze() => Freeze(new DateTime(2000, 1, 1));

        public static void Freeze(DateTime time)
        {
            Now = () => time;
        }
    }
}
