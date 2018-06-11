using NosCore.Shared;
using System;
using System.Globalization;
using System.Reflection;
using System.Resources;

namespace NosCore.Shared.Logger
{
    public class LogLanguage
    {
        private LogLanguage()
        {
            _resourceCulture = new CultureInfo(Language);
            if (Assembly.GetExecutingAssembly() != null)
            {
                _manager = new ResourceManager(Assembly.GetExecutingAssembly().GetName().Name + ".Resource.LocalizedResources", Assembly.GetExecutingAssembly());
            }
        }

        public static LogLanguage Instance
        {
            get { return instance ?? (instance = new LogLanguage()); }
        }

        public string GetMessageFromKey(LanguageKey messageKey, string culture = null)
        {
            CultureInfo cult = culture != null ? new CultureInfo(culture) : _resourceCulture;
            string resourceMessage = _manager != null && messageKey.ToString() != null ? _manager.GetResourceSet(cult, true, cult.TwoLetterISOLanguageName == default(RegionType).ToString().ToLower())?.GetString(messageKey.ToString()) : string.Empty;

            return !string.IsNullOrEmpty(resourceMessage) ? resourceMessage : $"#<{messageKey.ToString() }>";
        }

        public ResourceSet GetRessourceSet(string culture = null)
        {
            return _manager?.GetResourceSet(culture != null ? new CultureInfo(culture) : _resourceCulture, true, true);
        }

        private static LogLanguage instance;
        private readonly ResourceManager _manager;
        private static CultureInfo _resourceCulture;
        public static string Language = "en";
    }
}