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
        public static IQueryable<T> FindAllAsync<T, TKey>(this DbSet<T> dbSet, PropertyInfo keyProperty,
            params TKey[] keyValues)
        where T : class
        {
            var list = keyValues.ToList();
            // build lambda expression
            var parameter = Expression.Parameter(typeof(T), "e");
            var methodInfo = typeof(List<TKey>).GetMethod("Contains");
            // ReSharper disable once AssignNullToNotNullAttribute
            var body = Expression.Call(Expression.Constant(list, typeof(List<TKey>)), methodInfo, Expression.MakeMemberAccess(parameter, keyProperty));
            var predicateExpression = Expression.Lambda<Func<T, bool>>(body, parameter);

            // run query
            return dbSet.Where(predicateExpression);
        }
    }
}