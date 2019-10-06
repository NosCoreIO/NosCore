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

using System;
using System.Collections.Generic;
using System.Linq.Expressions;
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