using System;
using System.Collections.Generic;
using System.Text;
using NosCore.Shared.Enumerations;

namespace NosCore.Core.Extensions
{
    public static class RegionTypeExtension
    {
        public static Encoding GetEncoding(this RegionType region)
        {
            switch (region)
            {
                case RegionType.ES:
                case RegionType.EN:
                case RegionType.FR:
                    return CodePagesEncodingProvider.Instance.GetEncoding(1252);
                case RegionType.DE:
                case RegionType.IT:
                case RegionType.PL:
                case RegionType.CS:
                    return CodePagesEncodingProvider.Instance.GetEncoding(1250);
                case RegionType.TR:
                    return CodePagesEncodingProvider.Instance.GetEncoding(1254);
                default:
                    return Encoding.Default;
            }
        }
    }
}
