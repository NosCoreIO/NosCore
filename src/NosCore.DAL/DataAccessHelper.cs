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
using System;
using Microsoft.EntityFrameworkCore;
using NosCore.Database;
using NosCore.Shared.I18N;

namespace NosCore.DAL
{
    public sealed class DataAccessHelper
    {
        private static DataAccessHelper instance;

        #region Members

        private DbContextOptions _option;

        #endregion

        private DataAccessHelper()
        {
        }

        public static DataAccessHelper Instance => instance ?? (instance = new DataAccessHelper());

        #region Methods

        /// <summary>
        ///     Creates new instance of database context.
        /// </summary>
        public NosCoreContext CreateContext()
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
            using (var context = CreateContext())
            {
                try
                {
                    context.Database.Migrate();
                    context.Database.GetDbConnection().Open();
                    Logger.Log.Info(LogLanguage.Instance.GetMessageFromKey(LanguageKey.DATABASE_INITIALIZED));
                }
                catch (Exception ex)
                {
                    Logger.Log.Error("Database Error", ex);
                    Logger.Log.Error(LogLanguage.Instance.GetMessageFromKey(LanguageKey.DATABASE_NOT_UPTODATE));
                    throw;
                }
            }
        }

        #endregion
    }
}