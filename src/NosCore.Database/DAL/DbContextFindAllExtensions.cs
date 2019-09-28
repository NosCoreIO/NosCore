using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using Microsoft.EntityFrameworkCore;

namespace NosCore.Database.DAL
{
    public static class DbContextFindAllExtensions
    {
        private static readonly MethodInfo ContainsMethod = typeof(Enumerable).GetMethods()
            .FirstOrDefault(m => (m.Name == "Contains") && (m.GetParameters().Length == 2))
            .MakeGenericMethod(typeof(object));

        public static IQueryable<T> FindAllAsync<T>(this DbSet<T> dbSet, PropertyInfo keyProperty,
            params object[] keyValues)
        where T : class
        {
            if (keyValues.Length == 0)
            {
                return Enumerable.Empty<T>().AsQueryable();
            }
            // build lambda expression
            var parameter = Expression.Parameter(typeof(T), "e");
            var body = Expression.Call(null, ContainsMethod,
                Expression.Constant(keyValues),
                Expression.Convert(Expression.MakeMemberAccess(parameter, keyProperty), typeof(object)));
            var predicateExpression = Expression.Lambda<Func<T, bool>>(body, parameter);

            // run query
            return dbSet.Where(predicateExpression);
        }
    }
}