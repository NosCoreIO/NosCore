using System.Globalization;
using System.Reflection;
using System.Resources;

namespace NosCore.Shared.Logger
{
    public class Language
    {
        private Language()
        {
            if (Assembly.GetExecutingAssembly() != null)
            {
                _manager = new ResourceManager(Assembly.GetExecutingAssembly().GetName().Name + ".Resource.LocalizedResources", Assembly.GetExecutingAssembly());
            }
        }

        public static Language Instance
        {
            get { return instance ?? (instance = new Language()); }
        }

        public string GetMessageFromKey(LanguageKey messageKey, RegionType culture)
        {
            CultureInfo cult = new CultureInfo(culture.ToString());
            string resourceMessage = _manager != null && messageKey.ToString() != null ? _manager.GetResourceSet(cult, true, cult.TwoLetterISOLanguageName == default(RegionType).ToString().ToLower())?.GetString(messageKey.ToString()) : string.Empty;

            return !string.IsNullOrEmpty(resourceMessage) ? resourceMessage : $"#<{messageKey.ToString() }>";
        }

        private static Language instance;
        private readonly ResourceManager _manager;

    }
}