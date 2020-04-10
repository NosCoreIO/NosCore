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
using Microsoft.EntityFrameworkCore;
using NosCore.Core.I18N;
using NosCore.Dao.Interfaces;
using NosCore.Data.Enumerations.I18N;
using Serilog;

namespace NosCore.Database.DAL
{
    public sealed class DataAccessHelper : IDbContextBuilder
    {
        private readonly ILogger _logger = Logger.GetLoggerConfiguration().CreateLogger();

        private DbContextOptions? _option;

        /// <summary>
        ///     Creates new instance of database context.
        /// </summary>
        public DbContext CreateContext()
        {
            return new NosCoreContext(_option);
        }

        public void InitializeForTest(DbContextOptions option)
        {
            _option = option;
        }

        public void Initialize(DbContextOptions option)
        {
            _option = option;
            using var context = CreateContext();
            try
            {
                context.Database.Migrate();
                context.Database.GetDbConnection().Open();
                _logger.Information(LogLanguage.Instance.GetMessageFromKey(LogLanguageKey.DATABASE_INITIALIZED));
            }
            catch (Exception ex)
            {
                _logger.Error("Database Error", ex);
                _logger.Error(LogLanguage.Instance.GetMessageFromKey(LogLanguageKey.DATABASE_NOT_UPTODATE));
                throw;
            }
        }
    }
}