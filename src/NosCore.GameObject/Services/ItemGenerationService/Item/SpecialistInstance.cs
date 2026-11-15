//  __  _  __    __   ___ __  ___ ___
// |  \| |/__\ /' _/ / _//__\| _ \ __|
// | | ' | \/ |`._`.| \_| \/ | v / _|
// |_|\__|\__/ |___/ \__/\__/|_|_\___|
//

using NosCore.Data.Dto;
using NosCore.Packets.Enumerations;
using System;

namespace NosCore.GameObject.Services.ItemGenerationService.Item
{
    public class SpecialistInstance : SpecialistInstanceDto, IItemInstance
    {
        public SpecialistInstance(Item item)
        {
            Id = Guid.NewGuid();
            Item = item;
            ItemVNum = item.VNum;
        }


        public Item Item { get; set; }

        // The card window sends it back when points are spent, naming the card it showed.
        public int TransportId => Id.GetHashCode() & int.MaxValue;

        public SpPartnerRank PartnerSkillRank(int slot) => slot switch
        {
            0 => FirstPartnerSkillRank,
            1 => SecondPartnerSkillRank,
            2 => ThirdPartnerSkillRank,
            _ => SpPartnerRank.NotIdentifiedYet
        };

        public void SetPartnerSkillRank(int slot, SpPartnerRank rank)
        {
            switch (slot)
            {
                case 0:
                    FirstPartnerSkillRank = rank;
                    break;
                case 1:
                    SecondPartnerSkillRank = rank;
                    break;
                case 2:
                    ThirdPartnerSkillRank = rank;
                    break;
            }
        }

        public object Clone()
        {
            return (SpecialistInstance)MemberwiseClone();
        }
    }
}
