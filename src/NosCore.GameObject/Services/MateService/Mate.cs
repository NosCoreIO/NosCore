//  __  _  __    __   ___ __  ___ ___
// |  \| |/__\ /' _/ / _//__\| _ \ __|
// | | ' | \/ |`._`.| \_| \/ | v / _|
// |_|\__|\__/ |___/ \__/\__/|_|_\___|
//

using NosCore.Data.Dto;
using NosCore.Data.Enumerations.Character;
using NosCore.Data.StaticEntities;
using NosCore.Packets.ServerPackets.Mates;
using NosCore.Shared.Enumerations;

namespace NosCore.GameObject.Services.MateService
{
    /// <summary>
    /// A pet or a partner a character owns: the stored row plus what it takes to talk about it
    /// to the client.
    /// </summary>
    /// <remarks>
    /// CaptureService has been writing these rows to the database for a while and nothing ever
    /// read them back, so a captured pet disappeared the moment the fight ended. This type is
    /// what reads them.
    /// </remarks>
    public class Mate : MateDto
    {
        /// <summary>The static description of the creature this mate is an instance of.</summary>
        public NpcMonsterDto NpcMonster { get; set; } = null!;

        /// <summary>
        /// The id the client uses to address this mate — the same role a map monster's visual id
        /// plays, and the reason it has to be unique across the world rather than per character.
        /// </summary>
        public long MateTransportId { get; set; }

        /// <summary>
        /// The slot the mate occupies in its own list. Pets and partners are numbered separately,
        /// each from zero: the capture shows sc_p slots 0..7 alongside sc_n slots 0..1 in the
        /// same login burst.
        /// </summary>
        public byte PetSlot { get; set; }

        /// <summary>
        /// MAX HP AND MP ARE THE CREATURE'S OWN, NOT A LEVEL CURVE.
        ///
        /// The capture proves a mate's maximum grows with its level — the same chicken shows
        /// 156 HP at level 1 and 195 at level 3 — but it does not reveal the curve, and the one
        /// the older emulators ship does not reproduce a single observed row. Rather than scale
        /// by a formula that is already known to be wrong, this reports the creature's declared
        /// maximum, which is exactly right at level 1 and too low above it.
        ///
        /// The observations are written down in docs/design/nosmate-cattura.md so the curve can
        /// be settled from data instead of guessed. Same story for damage, concentration and the
        /// defences below.
        /// </summary>
        public int MaxHp => NpcMonster.MaxHp;

        /// <inheritdoc cref="MaxHp" />
        public int MaxMp => NpcMonster.MaxMp;

        /// <summary>Experience needed to reach the next level; also what sc_p/sc_n report.</summary>
        public long XpLoad => MateXpTable.RequiredXp(Level, MateType);

        public ScpPacket GenerateScp(RegionType language)
        {
            return new ScpPacket
            {
                PetId = PetSlot,
                NpcMonsterVNum = VNum,
                TransportId = MateTransportId,
                Level = Level,
                Loyalty = Loyalty,
                Experience = Experience,
                Unknow1 = 0,
                AttackUpgrade = NpcMonster.AttackUpgrade,
                DamageMinimum = NpcMonster.DamageMinimum,
                DamageMaximum = NpcMonster.DamageMaximum,
                Concentrate = NpcMonster.Concentrate,
                CriticalChance = NpcMonster.CriticalChance,
                CriticalRate = NpcMonster.CriticalRate,
                DefenceUpgrade = NpcMonster.DefenceUpgrade,
                CloseDefence = NpcMonster.CloseDefence,
                DefenceDodge = NpcMonster.DefenceDodge,
                DistanceDefence = NpcMonster.DistanceDefence,
                DistanceDefenceDodge = NpcMonster.DistanceDefenceDodge,
                MagicDefence = NpcMonster.MagicDefence,
                Element = NpcMonster.Element,
                FireResistance = NpcMonster.FireResistance,
                WaterResistance = NpcMonster.WaterResistance,
                LightResistance = NpcMonster.LightResistance,
                DarkResistance = NpcMonster.DarkResistance,
                Hp = Hp,
                MaxHp = MaxHp,
                Mp = Mp,
                MaxMp = MaxMp,
                IsTeamMember = IsTeamMember,
                XpLoad = XpLoad,
                CanPickUp = CanPickUp,
                Name = DisplayName(language),
                IsSummonable = IsSummonable
            };
        }

        public ScnPacket GenerateScn(RegionType language)
        {
            return new ScnPacket
            {
                PetId = PetSlot,
                NpcMonsterVNum = VNum,
                TransportId = MateTransportId,
                Level = Level,
                Loyalty = Loyalty,
                Experience = Experience,
                // A partner's four equipment slots. Nothing wears anything yet.
                //
                // THIS IS THE ONE PLACE THAT KNOWINGLY DIVERGES FROM THE CAPTURE, and it is the
                // packet library's doing rather than a choice. The capture spells an empty slot
                // as a bare -1:
                //
                //     sc_n 1 319 26719 50 1000 1536 990.0.0 997.0.0 -1 -1 0 0 ...
                //
                // Leaving the sub-packet null is how one would say that, but the serializer then
                // drops the separating space and produces "1536-1-1-1" — a packet the client
                // cannot split. Filling all three numbers keeps the field count and the spacing
                // right at the cost of writing -1.0.0 where the real server writes -1.
                //
                // It costs nothing today: nothing in the server creates a partner yet, so this
                // branch is unreachable in practice. It has to be settled before one can be
                // created, either by teaching the serializer to space a null sub-packet or by
                // confirming the client reads -1.0.0 the same way.
                WeaponInstanceDetails = EmptySlot,
                ArmorInstanceDetails = EmptySlot,
                GauntletInstanceDetails = EmptySlot,
                BootsInstanceDetails = EmptySlot,
                AttackUpgrade = NpcMonster.AttackUpgrade,
                MinimumAttack = NpcMonster.DamageMinimum,
                MaximumAttack = NpcMonster.DamageMaximum,
                Precision = NpcMonster.Concentrate,
                CriticalRate = NpcMonster.CriticalChance,
                CriticalDamageRate = NpcMonster.CriticalRate,
                DefenceUpgrade = NpcMonster.DefenceUpgrade,
                Defence = NpcMonster.CloseDefence,
                DefenceDodge = NpcMonster.DefenceDodge,
                DistanceDefence = NpcMonster.DistanceDefence,
                DistanceDodge = NpcMonster.DistanceDefenceDodge,
                DodgeRate = NpcMonster.MagicDefence,
                ElementRate = NpcMonster.Element,
                FireResistance = NpcMonster.FireResistance,
                WaterResistance = NpcMonster.WaterResistance,
                LightResistance = NpcMonster.LightResistance,
                DarkResistance = NpcMonster.DarkResistance,
                Hp = Hp,
                HpMax = MaxHp,
                Mp = Mp,
                MpMax = MaxMp,
                IsTeamMember = IsTeamMember,
                LevelXp = (int)XpLoad,
                Name = DisplayName(language),
                // Morph: -1 while no specialist card is worn, which is what the capture shows
                // for both partners in it.
                MorphId = Skin != 0 ? Skin : -1,
                IsSummonable = IsSummonable,
                SpDetails = null,
                Skill1Details = null,
                Skill2Details = null,
                Skill3Details = null
            };
        }

        /// <summary>
        /// What the client should print above the mate.
        /// </summary>
        /// <remarks>
        /// Two things happen here. A mate that was never renamed falls back to the creature's
        /// own name, which is per-language — the same pet is a Poule to one player and a Chicken
        /// to another, and the packet carries whichever the account asked for. And every space
        /// becomes a caret, because the client splits a packet on spaces: "Joyeux Mouton" sent
        /// as-is would arrive as two fields and shift everything after it.
        /// </remarks>
        private string DisplayName(RegionType language)
        {
            var name = Name;
            if (string.IsNullOrEmpty(name))
            {
                // A creature with no entry for that language would otherwise be nameless; EN is
                // what the parser always fills, so it is the only safe fallback.
                name = NpcMonster.Name.TryGetValue(language, out var localized)
                    ? localized
                    : NpcMonster.Name[RegionType.EN];
            }

            return name.Replace(' ', '^');
        }

        private static ScnPacket.ScEquipmentDetails EmptySlot => new()
        {
            ItemId = -1,
            ItemRare = 0,
            ItemUpgrade = 0
        };
    }
}
