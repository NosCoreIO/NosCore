using System.Globalization;
using System.Reflection;
using System.Resources;
using NosCore.Shared.Enumerations;

namespace NosCore.Shared.I18N
{
    public sealed class LogLanguage
    {
        private static LogLanguage _instance;
        private static CultureInfo _resourceCulture;
        public static RegionType Language { get; set; }
        private readonly ResourceManager _manager;

        private LogLanguage()
        {
            _resourceCulture = new CultureInfo(Language.ToString());
            if (Assembly.GetExecutingAssembly() != null)
            {
                _manager = new ResourceManager(
                    Assembly.GetExecutingAssembly().GetName().Name + ".Resource.LocalizedResources",
                    Assembly.GetExecutingAssembly());
            }
        }

        public static LogLanguage Instance => _instance ?? (_instance = new LogLanguage());

        public string GetMessageFromKey(LanguageKey messageKey, string culture = null)
        {
            var cult = culture != null ? new CultureInfo(culture) : _resourceCulture;
            var resourceMessage = _manager != null && messageKey.ToString() != null
                ? _manager.GetResourceSet(cult, true,
                        cult.TwoLetterISOLanguageName == default(RegionType).ToString().ToLower())
                    ?.GetString(messageKey.ToString()) : string.Empty;

            return !string.IsNullOrEmpty(resourceMessage) ? resourceMessage : $"#<{messageKey.ToString()}>";
        }

        public ResourceSet GetRessourceSet(string culture = null)
        {
            return _manager?.GetResourceSet(culture != null ? new CultureInfo(culture) : _resourceCulture, true, true);
        }
    }
}