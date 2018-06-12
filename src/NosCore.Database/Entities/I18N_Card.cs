using NosCore.Shared.Enumerations;

namespace NosCore.Database.Entities
{
	public class I18N_Card
	{
		public int I18N_CardId { get; set; }
		public string Key { get; set; }
		public RegionType RegionType { get; set; }
		public string Text { get; set; }
	}
}