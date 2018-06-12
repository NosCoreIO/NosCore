using System.Collections.Generic;
using System.Reflection;

namespace NosCore.Core.Extensions
{
	public static class AssemblyExtension
	{
		public static IEnumerable<T> GetInstancesOfImplementingTypes<T>(this Assembly assembly)
		{
			foreach (var t in assembly.GetTypes())
			{
				if (typeof(T).IsAssignableFrom(t))
				{
					yield return t.CreateInstance<T>();
				}
			}
		}
	}
}