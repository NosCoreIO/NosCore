//  __  _  __    __   ___ __  ___ ___
// |  \| |/__\ /' _/ / _//__\| _ \ __|
// | | ' | \/ |`._`.| \_| \/ | v / _|
// |_|\__|\__/ |___/ \__/\__/|_|_\___|
//
using System.Collections.Generic;
using NosCore.Packets.Enumerations;

namespace NosCore.Parser.Parsers
{
    // The name the client shows above a partner transformed with each partner specialist card, keyed by card vnum.
    public static class PartnerSpecialistNames
    {
        public static readonly IReadOnlyDictionary<short, Game18NConstString> All = Build(
            (Game18NConstString.AegirAngry, [4800, 8224]),
            (Game18NConstString.BarniClever, [4802, 8225]),
            (Game18NConstString.FreyaFateful, [4803, 8226]),
            (Game18NConstString.Shinobi, [4804, 8227]),
            (Game18NConstString.Lotus, [4805, 8228]),
            (Game18NConstString.Orkani, [4806, 8229]),
            (Game18NConstString.Foxy, [4807, 8230]),
            (Game18NConstString.Maru, [4808, 8231]),
            (Game18NConstString.Undefined2127, [4809, 8232]),
            (Game18NConstString.ImpHongbi, [4810, 8233]),
            (Game18NConstString.ImpCheongbi, [4811, 8234]),
            (Game18NConstString.ArchangelLucifer, [4812, 8235]),
            // The client also has "White Witch Laurena"; the plain witch is the one numbered with the other early cards.
            (Game18NConstString.WitchLaurena, [4813, 8236]),
            (Game18NConstString.OverseerAmon, [4814, 8237]),
            (Game18NConstString.LucyLopears, [4815, 8238]),
            (Game18NConstString.CowgirlChloe, [4817, 8239]),
            (Game18NConstString.Fiona, [4818, 8240]),
            (Game18NConstString.Jinn, [4819, 8241]),
            (Game18NConstString.IcePrincessEliza, [4820, 8242]),
            (Game18NConstString.DanielDucats, [4821, 8243]),
            (Game18NConstString.PalinaPuppetMaster, [4822, 8244]),
            (Game18NConstString.Harlequin, [4823, 8245]),
            (Game18NConstString.NeliaNymph, [4824, 8246]),
            (Game18NConstString.LittlePrincessVenus, [4825, 8247]),
            (Game18NConstString.BoneWarriorRagnar, [4326, 8300]),
            (Game18NConstString.Undefined2363, [4324, 8356]),
            (Game18NConstString.Undefined2364, [4325, 8357]),
            (Game18NConstString.Undefined2365, [4349, 8358]),
            (Game18NConstString.MagicStudentYuna, [4405, 8367]),
            (Game18NConstString.Amora, [4413, 8375]),
            (Game18NConstString.MadMarchHare, [4417, 8379]),
            (Game18NConstString.AngelPerti, [4446, 8398]),
            (Game18NConstString.AkhenatonCursedPharaoh, [4547, 8420]),
            (Game18NConstString.AlitusXV, [8513, 8514]),
            (Game18NConstString.AlitusPrototype, [8515, 8516]),
            (Game18NConstString.WoodElfArcher, [8571, 8582]),
            (Game18NConstString.VampireMarie, [8718]),
            (Game18NConstString.WaspKnightFeraspa, [8796]),
            (Game18NConstString.BenevolentNezarun, [8877]),
            (Game18NConstString.CelineGrandMasterOfThePetal, [8895]));

        private static Dictionary<short, Game18NConstString> Build(params (Game18NConstString Name, short[] Cards)[] rows)
        {
            var all = new Dictionary<short, Game18NConstString>();
            foreach (var (name, cards) in rows)
            {
                foreach (var card in cards)
                {
                    all.Add(card, name);
                }
            }

            return all;
        }
    }
}
