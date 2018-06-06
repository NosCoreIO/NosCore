using NosCore.Shared;
using System;
using System.Globalization;
using System.Reflection;
using System.Resources;

namespace NosCore.Shared.Logger
{
    public class LogLanguage
    {
        #region Instantiation

        private LogLanguage()
        {
            _resourceCulture = new CultureInfo("en-en"); //TODO Replace by configuration ConfigurationManager.AppSettings["Language"]
            if (Assembly.GetExecutingAssembly() != null)
            {
                _manager = new ResourceManager(Assembly.GetExecutingAssembly().GetName().Name + ".Resource.LocalizedResources", Assembly.GetExecutingAssembly());
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

        public string GetMessageFromKey(LogLanguageKey messageKey, string culture = null)
        {
            string resourceMessage = _manager != null && messageKey.ToString() != null ? _manager.GetResourceSet(culture != null ? new CultureInfo(culture) : _resourceCulture, true, culture == default(RegionType).ToString())?.GetString(messageKey.ToString()) : string.Empty;

            return !string.IsNullOrEmpty(resourceMessage) ? resourceMessage : $"#<{messageKey.ToString() }>";
        }

        public ResourceSet GetRessourceSet(string culture = null)
        {
            return _manager?.GetResourceSet(culture != null ? new CultureInfo(culture) : _resourceCulture, true, true);
        }
        #endregion

        #region Members

        private static LogLanguage instance;
        private readonly ResourceManager _manager;
        private readonly CultureInfo _resourceCulture;

        #endregion
    }
}