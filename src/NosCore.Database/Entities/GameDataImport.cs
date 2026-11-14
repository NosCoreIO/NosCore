//  __  _  __    __   ___ __  ___ ___
// |  \| |/__\ /' _/ / _//__\| _ \ __|
// | | ' | \/ |`._`.| \_| \/ | v / _|
// |_|\__|\__/ |___/ \__/\__/|_|_\___|
//

using NosCore.Database.Entities.Base;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace NosCore.Database.Entities
{
    // One row, written by the parser once a full import has finished.
    public class GameDataImport : IEntity
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.None)]
        public int GameDataImportId { get; set; }

        // GameDataFingerprint.OfParser of the parser that ran the import.
        [Required]
        public required string ParserBuild { get; set; }

        // GameDataFingerprint.OfClientFiles of the folder it read.
        [Required]
        public required string ClientFiles { get; set; }

        // Where the parser read the client files, so the world server can import them again.
        [Required]
        public required string Folder { get; set; }
    }
}
