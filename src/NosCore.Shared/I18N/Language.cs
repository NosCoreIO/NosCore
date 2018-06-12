using System.Globalization;
using System.Reflection;
using System.Resources;
using NosCore.Shared.Enumerations;

namespace NosCore.Shared.I18N
{
	public sealed class Language
	{
		private static Language instance;
		private readonly ResourceManager _manager;

		private Language()
		{
			if (Assembly.GetExecutingAssembly() != null)
			{
				_manager = new ResourceManager(
					Assembly.GetExecutingAssembly().GetName().Name + ".Resource.LocalizedResources",
					Assembly.GetExecutingAssembly());
			}
		}

		public static Language Instance => instance ?? (instance = new Language());

		public string GetMessageFromKey(LanguageKey messageKey, RegionType culture)
		{
			var cult = new CultureInfo(culture.ToString());
			var resourceMessage = _manager != null && messageKey.ToString() != null
				? _manager.GetResourceSet(cult, true,
						cult.TwoLetterISOLanguageName == default(RegionType).ToString().ToLower())
					?.GetString(messageKey.ToString()) : string.Empty;

			return !string.IsNullOrEmpty(resourceMessage) ? resourceMessage : $"#<{messageKey.ToString()}>";
		}
	}
}