//  __  _  __    __   ___ __  ___ ___
// |  \| |/__\ /' _/ / _//__\| _ \ __|
// | | ' | \/ |`._`.| \_| \/ | v / _|
// |_|\__|\__/ |___/ \__/\__/|_|_\___|
// 
// Copyright (C) 2019 - NosCore
// 
// NosCore is a free software: you can redistribute it and/or modify
// it under the terms of the GNU General Public License as published by
// the Free Software Foundation, either version 3 of the License, or any later version.
// 
// This program is distributed in the hope that it will be useful,
// but WITHOUT ANY WARRANTY; without even the implied warranty of
// MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
// GNU General Public License for more details.
// 
// You should have received a copy of the GNU General Public License
// along with this program.  If not, see <http://www.gnu.org/licenses/>.

using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using FastExpressionCompiler;
using NosCore.Core.Extensions;

namespace NosCore.Core
{
    public static class DelegateBuilder
    {
        public static T BuildDelegate<T>(MethodInfo method, params object[] missingParamValues) where T : class
        {
            var queueMissingParams = new Queue<object>(missingParamValues);

            var dgtMi = typeof(T).GetMethod("Invoke");
            var dgtParams = dgtMi.GetParameters();

            var paramsOfDelegate = dgtParams
                .Select(tp => Expression.Parameter(tp.ParameterType, tp.Name))
                .ToArray();

            var methodParams = method.GetParameters();

            if (method.IsStatic)
            {
                var paramsToPass = methodParams
                    .Select((p, i) => CreateParam(paramsOfDelegate, i, p, queueMissingParams))
                    .ToArray();

                var expr = Expression.Lambda<T>(
                    Expression.Call(method, paramsToPass),
                    paramsOfDelegate);

                return expr.CompileFast();
            }
            else
            {
                var paramThis = Expression.Convert(paramsOfDelegate[0], method.DeclaringType);

                var paramsToPass = methodParams
                    .Select((p, i) => CreateParam(paramsOfDelegate, i + 1, p, queueMissingParams))
                    .ToArray();

                var expr = Expression.Lambda<T>(
                    Expression.Call(paramThis, method, paramsToPass),
                    paramsOfDelegate);

                return expr.CompileFast();
            }
        }

        private static Expression CreateParam(ParameterExpression[] paramsOfDelegate, int i,
            ParameterInfo callParamType, Queue<object> queueMissingParams)
        {
            if (i < paramsOfDelegate.Length)
            {
                return Expression.Convert(paramsOfDelegate[i], callParamType.ParameterType);
            }

            if (queueMissingParams.Count > 0)
            {
                return Expression.Constant(queueMissingParams.Dequeue());
            }

            if (callParamType.ParameterType.IsValueType)
            {
                return Expression.Constant(callParamType.ParameterType.CreateInstance<object>());
            }

            return Expression.Constant(null);
        }
    }
}