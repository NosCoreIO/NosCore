using System.Globalization;
using System.Reflection;
using System.Resources;

namespace NosCore.Core.Logger
{
    public class Language //TODO replace by a session language system
    {
        #region Instantiation

        private Language()
        {
            _resourceCulture = new CultureInfo("en-en");
            if (Assembly.GetEntryAssembly() != null)
            {
                //_manager = new ResourceManager(Assembly.GetExecutingAssembly().GetName().Name + ".Ressource.LocalizedResources", Assembly.GetExecutingAssembly());
            }
        }

        #endregion

        #region Properties

        public static Language Instance
        {
            get { return instance ?? (instance = new Language()); }
        }

        #endregion

        #region Methods

        public string GetMessageFromKey(string message)
        {
            string resourceMessage = _manager != null && message != null ? _manager.GetString(message, _resourceCulture) : string.Empty;

            return string.Empty; //!string.IsNullOrEmpty(resourceMessage) ? resourceMessage : $"#<{message}>";
        }

        #endregion

        #region Members

        private static Language instance;
        private readonly ResourceManager _manager;
        private readonly CultureInfo _resourceCulture;

        #endregion
    }
}