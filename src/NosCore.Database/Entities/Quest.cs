using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace NosCore.Database.Entities
{
	public class Quest
	{
		#region Properties

		public Quest()
		{
			CharacterQuest = new HashSet<CharacterQuest>();
		}

		[Key]
		[DatabaseGenerated(DatabaseGeneratedOption.None)]
		public short QuestId { get; set; }

		public int QuestType { get; set; }

		public HashSet<CharacterQuest> CharacterQuest { get; set; }

		public byte LevelMin { get; set; }

		public byte LevelMax { get; set; }

		public int? StartDialogId { get; set; }

		public int? EndDialogId { get; set; }

		public HashSet<QuestObjective> QuestObjective { get; set; }

		public short? TargetMap { get; set; }

		public short? TargetX { get; set; }

		public short? TargetY { get; set; }

		public int InfoId { get; set; }

		public long? NextQuestId { get; set; }

		public bool IsDaily { get; set; }

		public int? SpecialData { get; set; }

		#endregion
	}
}