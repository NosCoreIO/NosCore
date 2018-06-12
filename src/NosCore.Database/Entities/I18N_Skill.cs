using NosCore.Shared.Enumerations;

namespace NosCore.Database.Entities
{
	public class I18N_Skill
	{
		public int I18N_SkillId { get; set; }
		public string Key { get; set; }
		public RegionType RegionType { get; set; }
		public string Text { get; set; }
	}
}