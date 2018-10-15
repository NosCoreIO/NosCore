//  __  _  __    __   ___ __  ___ ___  
// |  \| |/__\ /' _/ / _//__\| _ \ __| 
// | | ' | \/ |`._`.| \_| \/ | v / _|  
// |_|\__|\__/ |___/ \__/\__/|_|_\___| 
// 
// Copyright (C) 2018 - NosCore
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

using FastExpressionCompiler;
using System.Collections.Generic;
using System.Linq.Expressions;
using System.Reflection;

namespace NosCore.Core
{
    public class FastMethodInfo
    {
        public FastMethodInfo(MethodInfo methodInfo)
        {
            var instanceExpression = Expression.Parameter(typeof(object), "instance");
            var argumentsExpression = Expression.Parameter(typeof(object[]), "arguments");
            var argumentExpressions = new List<Expression>();
            var parameterInfos = methodInfo.GetParameters();
            for (var i = 0; i < parameterInfos.Length; ++i)
            {
                var parameterInfo = parameterInfos[i];
                argumentExpressions.Add(Expression.Convert(
                    Expression.ArrayIndex(argumentsExpression, Expression.Constant(i)), parameterInfo.ParameterType));
            }

            var callExpression =
                Expression.Call(
                    !methodInfo.IsStatic ? Expression.Convert(instanceExpression, methodInfo.ReflectedType) : null,
                    methodInfo, argumentExpressions);
            if (callExpression.Type == typeof(void))
            {
                var voidDelegate = Expression
                    .Lambda<VoidDelegate>(callExpression, instanceExpression, argumentsExpression).CompileFast();
                Delegate = (instance, arguments) =>
                {
                    voidDelegate(instance, arguments);
                    return null;
                };
            }
            else
            {
                Delegate = Expression.Lambda<ReturnValueDelegate>(Expression.Convert(callExpression, typeof(object)),
                    instanceExpression, argumentsExpression).CompileFast();
            }
        }

        private ReturnValueDelegate Delegate { get; }

        public object Invoke(object instance, params object[] arguments)
        {
            return Delegate(instance, arguments);
        }

        private delegate object ReturnValueDelegate(object instance, object[] arguments);

        private delegate void VoidDelegate(object instance, object[] arguments);
    }
}