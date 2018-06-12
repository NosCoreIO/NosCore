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
					.Lambda<VoidDelegate>(callExpression, instanceExpression, argumentsExpression).Compile();
				Delegate = (instance, arguments) =>
				{
					voidDelegate(instance, arguments);
					return null;
				};
			}
			else
			{
				Delegate = Expression.Lambda<ReturnValueDelegate>(Expression.Convert(callExpression, typeof(object)),
					instanceExpression, argumentsExpression).Compile();
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