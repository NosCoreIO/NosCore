using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using System.Text;
using NosCore.Data.Enumerations;

namespace NosCore.Core
{
    public interface IGenericDao<TDto>
    {
        SaveResult Delete(object dtokey);
        TDto FirstOrDefault(Expression<Func<TDto, bool>> predicate);
        SaveResult InsertOrUpdate(ref TDto dto);
        SaveResult InsertOrUpdate(IEnumerable<TDto> dtos);

        IEnumerable<TDto> LoadAll();

        IEnumerable<TDto> Where(Expression<Func<TDto, bool>> predicate);
    }
}
