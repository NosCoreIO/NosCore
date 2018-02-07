using Microsoft.EntityFrameworkCore;
using OpenNosCore.Configuration;
using OpenNosCore.Core.Logger;
using System;

namespace OpenNosCore.Database
{
    public class DataAccessHelper
    {
        private static DataAccessHelper instance;

        private DataAccessHelper() { }

        public static DataAccessHelper Instance
        {
            get
            {
                if (instance == null)
                {
                    instance = new DataAccessHelper();
                }
                return instance;
            }
        }

        #region Members

        private string Host;
        private string DatabaseName;

        #endregion

        #region Methods


        /// <summary>
        /// Creates new instance of database context.
        /// </summary>
        public OpenNosCoreContext CreateContext()
        {
            return new OpenNosCoreContext(Host, DatabaseName);
        }
   

        public bool Initialize(DatabaseConfiguration Database)
        {
            Host = Database.Host;
            DatabaseName = Database.Name;
            using (OpenNosCoreContext context = CreateContext())
            {
                try
                {
                    context.Database.Migrate();
                    context.Database.GetDbConnection().Open();
                    Logger.Log.Info(LogLanguage.Instance.GetMessageFromKey("DATABASE_INITIALIZED"));
                }
                catch (Exception ex)
                {
                    Logger.Log.Error("Database Error", ex);
                    Logger.Log.Error(LogLanguage.Instance.GetMessageFromKey("DATABASE_NOT_UPTODATE"));
                    return false;
                }
                return true;
            }
        }

        #endregion
    }
}