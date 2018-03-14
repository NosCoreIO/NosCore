using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using NosCore.Domain;

namespace NosCore.Database
{
    public interface IGenericDAO<TEntity, TDTO> : IMappingBaseDAO where TEntity : class
    {
        IEnumerable<TDTO> LoadAll();

        IEnumerable<TDTO> Where(Expression<Func<TEntity, bool>> predicate);

        TDTO FirstOrDefault(Expression<Func<TEntity, bool>> predicate);

        SaveResult InsertOrUpdate(ref TDTO dto);

        SaveResult InsertOrUpdate(IEnumerable<TDTO> dtos);

        SaveResult Delete(object entitykey);

    }
}