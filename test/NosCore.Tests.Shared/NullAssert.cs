//  __  _  __    __   ___ __  ___ ___
// |  \| |/__\ /' _/ / _//__\| _ \ __|
// | | ' | \/ |`._`.| \_| \/ | v / _|
// |_|\__|\__/ |___/ \__/\__/|_|_\___|
//

using System.Runtime.CompilerServices;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace NosCore.Tests.Shared
{
    public static class NullAssert
    {
        public static T NotNull<T>(this T? value, [CallerArgumentExpression(nameof(value))] string expression = "")
            where T : class =>
            value ?? throw new AssertFailedException($"{expression} was null.");

        public static T NotNull<T>(this T? value, [CallerArgumentExpression(nameof(value))] string expression = "")
            where T : struct =>
            value ?? throw new AssertFailedException($"{expression} was null.");
    }
}
