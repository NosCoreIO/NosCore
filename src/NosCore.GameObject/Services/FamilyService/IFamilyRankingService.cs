//  __  _  __    __   ___ __  ___ ___
// |  \| |/__\ /' _/ / _//__\| _ \ __|
// | | ' | \/ |`._`.| \_| \/ | v / _|
// |_|\__|\__/ |___/ \__/\__/|_|_\___|
//
using NosCore.Packets.Enumerations;
using System.Collections.Generic;

namespace NosCore.GameObject.Services.FamilyService
{
    public interface IFamilyRankingService
    {
        /// <summary>Families sharing a value share a rank; a tab with nothing to rank on is empty.</summary>
        IReadOnlyList<FamilyRankingEntry> GetRanking(FamilyRankingType type);
    }
}
