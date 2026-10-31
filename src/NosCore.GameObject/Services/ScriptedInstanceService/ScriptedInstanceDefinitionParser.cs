//  __  _  __    __   ___ __  ___ ___
// |  \| |/__\ /' _/ / _//__\| _ \ __|
// | | ' | \/ |`._`.| \_| \/ | v / _|
// |_|\__|\__/ |___/ \__/\__/|_|_\___|
//

using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Xml.Linq;

namespace NosCore.GameObject.Services.ScriptedInstanceService
{
    public static class ScriptedInstanceDefinitionParser
    {
        public static ScriptedInstanceDefinition? Parse(string? script)
        {
            if (string.IsNullOrWhiteSpace(script))
            {
                return null;
            }

            var definition = XDocument.Parse(script).Element("Definition")
                ?? throw new FormatException("An instance script must have a Definition element at its root.");

            var globals = definition.Element("Globals");

            return new ScriptedInstanceDefinition
            {
                Id = Value<byte>(globals, "Id"),
                Label = Text(globals, "Label"),
                Title = Text(globals, "Title"),
                LevelMinimum = Value<byte>(globals, "LevelMinimum"),
                LevelMaximum = Value<byte>(globals, "LevelMaximum"),
                Lives = Value<byte>(globals, "Lives"),
                StartX = Value<short>(globals, "StartX"),
                StartY = Value<short>(globals, "StartY"),
                Gold = Value<long>(globals, "Gold"),
                Reputation = Value<int>(globals, "Reputation"),
                FamilyExperience = Value<int>(globals, "Fxp"),
                RequiredItems = Gifts(globals, "RequieredItems"),
                DrawItems = Gifts(globals, "DrawItems"),
                SpecialItems = Gifts(globals, "SpecialItems"),
                GiftItems = Gifts(globals, "GiftItems"),
                Rooms = Rooms(definition)
            };
        }

        private static IReadOnlyList<InstanceRoom> Rooms(XElement definition)
        {
            var rooms = definition.Element("InstanceEvents")?.Elements("CreateMap").ToList();
            if (rooms == null)
            {
                return [];
            }

            return rooms.Select(room => new InstanceRoom(
                    Attribute<int>(room, "Map"),
                    Attribute<short>(room, "VNum"),
                    Attribute<byte>(room, "IndexX"),
                    Attribute<byte>(room, "IndexY")))
                .ToList();
        }

        private static IReadOnlyList<InstanceGift> Gifts(XElement? globals, string listName)
        {
            var list = globals?.Element(listName);
            if (list == null)
            {
                return [];
            }

            return list.Elements().Select(item => new InstanceGift(
                    Attribute<short>(item, "VNum"),
                    Attribute<short>(item, "Amount"),
                    Attribute<short>(item, "Design"),
                    Attribute<bool>(item, "IsRandomRare"),
                    Attribute<bool>(item, "IsHeroic")))
                .ToList();
        }

        private static T Value<T>(XElement? globals, string name) where T : struct
        {
            return Attribute<T>(globals?.Element(name), "Value");
        }

        private static string? Text(XElement? globals, string name)
        {
            return globals?.Element(name)?.Attribute("Value")?.Value;
        }

        private static T Attribute<T>(XElement? element, string name) where T : struct
        {
            var raw = element?.Attribute(name)?.Value;
            if (string.IsNullOrEmpty(raw))
            {
                return default;
            }

            return typeof(T) == typeof(bool)
                ? (T)(object)bool.Parse(raw)
                : (T)Convert.ChangeType(raw, typeof(T), CultureInfo.InvariantCulture);
        }
    }
}
