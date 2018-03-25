using System;
using System.Globalization;
using System.Reflection;
using System.Resources;

namespace NosCore.Core.Logger
{
    public class LogLanguage
    {
        #region Instantiation

        private LogLanguage()
        {
            _resourceCulture = new CultureInfo("fr-fr"); //TODO Replace by configuration ConfigurationManager.AppSettings["Language"]
            if (Assembly.GetEntryAssembly() != null)
            {
                _manager = new ResourceManager(Assembly.GetExecutingAssembly().GetName().Name + ".Ressource.LocalizedResources", Assembly.GetExecutingAssembly());
            }
        }

        #endregion

        #region Properties

        public static LogLanguage Instance
        {
            get { return instance ?? (instance = new LogLanguage()); }
        }

        #endregion

        #region Methods

        public string GetMessageFromKey(LanguageKey messageKey)
        {
            string resourceMessage = _manager != null && messageKey.ToString() != null ? _manager.GetString(messageKey.ToString(), _resourceCulture) : string.Empty;

            return !string.IsNullOrEmpty(resourceMessage) ? resourceMessage : $"#<{messageKey.ToString() }>";
        }

        #endregion

        #region Members

        private static LogLanguage instance;
        private readonly ResourceManager _manager;
        private readonly CultureInfo _resourceCulture;

        #endregion
    }
}