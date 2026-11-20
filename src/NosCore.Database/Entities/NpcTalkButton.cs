//  __  _  __    __   ___ __  ___ ___
// |  \| |/__\ /' _/ / _//__\| _ \ __|
// | | ' | \/ |`._`.| \_| \/ | v / _|
// |_|\__|\__/ |___/ \__/\__/|_|_\___|
//
using NosCore.Data.DataAttributes;
using NosCore.Data.Enumerations.I18N;
using NosCore.Database.Entities.Base;
using NosCore.Packets.Enumerations;
using System;
using System.ComponentModel.DataAnnotations;

namespace NosCore.Database.Entities
{
    // A button of an npctalk.dat dialog: it either runs an n_run or opens another dialog.
    [StaticMetaData(LoadedMessage = LogLanguageKey.NPC_TALK_BUTTONS_LOADED)]
    public class NpcTalkButton : IStaticEntity
    {
        [Key]
        public Guid NpcTalkButtonId { get; set; }

        public virtual NpcTalk NpcTalk { get; set; }

        public short DialogId { get; set; }

        public NrunRunnerType? Runner { get; set; }

        public short Argument { get; set; }

        public short? NextDialogId { get; set; }
    }
}
