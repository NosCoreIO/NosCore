//  __  _  __    __   ___ __  ___ ___
// |  \| |/__\ /' _/ / _//__\| _ \ __|
// | | ' | \/ |`._`.| \_| \/ | v / _|
// |_|\__|\__/ |___/ \__/\__/|_|_\___|
// 
// Copyright (C) 2019 - NosCore
// 
// NosCore is a free software: you can redistribute it and/or modify
// it under the terms of the GNU General Public License as published by
// the Free Software Foundation, either version 3 of the License, or any later version.
// 
// This program is distributed in the hope that it will be useful,
// but WITHOUT ANY WARRANTY; without even the implied warranty of
// MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
// GNU General Public License for more details.
// 
// You should have received a copy of the GNU General Public License
// along with this program.  If not, see <http://www.gnu.org/licenses/>.

namespace NosCore.Data.Enumerations.Items
{
    public enum ItemEffectType : ushort
    {
        NoEffect = 0,
        Teleport = 1,
        ApplyHairDie = 10,
        Speaker = 15,
        MarriageProposal = 34,
        Undefined = 69,
        SpCharger = 71,
        DroppedSpRecharger = 150,
        PremiumSpRecharger = 151,
        CraftedSpRecharger = 152,
        SpecialistMedal = 204,
        ApplySkinPartner = 305,
        ChangeGender = 651,
        PointInitialisation = 652,
        SealedTarotCard = 789,
        TarotCard = 790,
        RedAmulet = 791,
        BlueAmulet = 792,
        ReinforcementAmulet = 793,
        Heroic = 794,
        RandomHeroic = 795,
        AttackAmulet = 932,
        DefenseAmulet = 933,
        SpeedBooster = 998,
        BoxEffect = 999,
        Vehicle = 1000,
        GoldNosMerchantUpgrade = 1003,
        SilverNosMerchantUpgrade = 1004,
        InventoryUpgrade = 1005,
        PetSpaceUpgrade = 1006,
        PetBasketUpgrade = 1007,
        PetBackpackUpgrade = 1008,
        InventoryTicketUpgrade = 1009,
        BuffPotions = 6600,
        MarriageSeparation = 6969
    }
}