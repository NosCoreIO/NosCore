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
using System.Linq.Expressions;

namespace NosCore.Database.DAL
{
    public static class ExpressionExtensions
    {
        public static Expression<Func<TTo, bool>> ReplaceParameter<TFrom, TTo>(
            this Expression<Func<TFrom, bool>> target)
        {
            return (Expression<Func<TTo, bool>>) new WhereReplacerVisitor<TFrom, TTo>().Visit(target);
        }

        private class WhereReplacerVisitor<TFrom, TTo> : ExpressionVisitor
        {
            private readonly ParameterExpression _parameter = Expression.Parameter(typeof(TTo), "c");

            protected override Expression VisitLambda<T>(Expression<T> node)
            {
                // replace parameter here
                return Expression.Lambda(Visit(node.Body), _parameter);
            }

            protected override Expression VisitMember(MemberExpression node)
            {
                // replace parameter member access with new type
                if (node.Member.DeclaringType == typeof(TFrom) && node.Expression is ParameterExpression)
                {
                    return Expression.PropertyOrField(_parameter, node.Member.Name);
                }

                return base.VisitMember(node);
            }
        }
    }
}