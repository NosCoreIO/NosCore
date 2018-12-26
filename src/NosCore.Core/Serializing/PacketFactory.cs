//  __  _  __    __   ___ __  ___ ___  
// |  \| |/__\ /' _/ / _//__\| _ \ __| 
// | | ' | \/ |`._`.| \_| \/ | v / _|  
// |_|\__|\__/ |___/ \__/\__/|_|_\___| 
// 
// Copyright (C) 2018 - NosCore
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

using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;
using NosCore.Core.Extensions;
using NosCore.Shared.I18N;
using Serilog;

namespace NosCore.Core.Serializing
{
    public static class PacketFactory
    {
        private static readonly ILogger _logger = Logger.GetLoggerConfiguration().CreateLogger();

        private static Dictionary<Tuple<Type, string>, Dictionary<PacketIndexAttribute, PropertyInfo>>
            _packetSerializationInformations;

        public static bool IsInitialized { get; set; }

        #region Methods
        
        /// <summary>
        ///     Deserializes a string into a PacketDefinition
        /// </summary>
        /// <param name="packetContent">The content to deseralize</param>
        /// <param name="packetType">The type of the packet to deserialize to</param>
        /// <param name="includesKeepAliveIdentity">
        ///     Include the keep alive identity or exclude it
        /// </param>
        /// <returns>The deserialized packet.</returns>
        public static PacketDefinition Deserialize(string packetContent, Type packetType) =>
            Deserialize(packetContent, packetType, false);

        public static PacketDefinition Deserialize(string packetContent, Type packetType,
            bool includesKeepAliveIdentity)
        {
            try
            {
                var serializationInformation = GetSerializationInformation(packetType);
                var deserializedPacket = packetType.CreateInstance<PacketDefinition>();
                SetDeserializationInformations(deserializedPacket, packetContent, serializationInformation.Key.Item2);
                return Deserialize(packetContent, deserializedPacket, serializationInformation,
                    includesKeepAliveIdentity);
            }
            catch (Exception e)
            {
               _logger.Warning(LogLanguage.Instance.GetMessageFromKey(LanguageKey.PACKET_WRONG_FORMAT), e);
                return null;
            }
        }

        public static PacketDefinition Deserialize(string packetContent)
        {
            try
            {
                var packetstring = packetContent.Replace('^', ' ').Replace("#", "").TrimEnd();
                var packetsplit = packetstring.Split(' ');
                if (packetsplit.Length <= 1)
                {
                    throw new InvalidOperationException();
                }

                if (packetsplit[1].Length >= 1
                    && (packetsplit[1][0] == '/' || packetsplit[1][0] == ':' || packetsplit[1][0] == ';'))
                {
                    packetsplit[1] = packetsplit[1][0].ToString();
                }

                var packetType = _packetSerializationInformations.Keys.FirstOrDefault(t => t.Item2 == packetsplit[0])
                    ?.Item1;
                if (packetType != null)
                {
                    return Deserialize(packetContent, packetType);
                }

                throw new InvalidOperationException();
            }
            catch (Exception e)
            {
               _logger.Warning(LogLanguage.Instance.GetMessageFromKey(LanguageKey.PACKET_WRONG_FORMAT), e);
                return null;
            }
        }

        /// <summary>
        ///     Initializes the PacketFactory and generates the serialization informations based on the
        ///     given BaseType.
        /// </summary>
        /// <typeparam name="TBaseType">The BaseType to generate serialization informations</typeparam>
        public static void Initialize<TBaseType>() where TBaseType : PacketDefinition
        {
            if (!IsInitialized)
            {
                GenerateSerializationInformations<TBaseType>();
                IsInitialized = true;
            }
        }

        /// <summary>
        ///     Serializes a PacketDefinition to string.
        /// </summary>
        /// <typeparam name="TPackets">The type of the PacketDefinition IEnumerable</typeparam>
        /// <param name="packets">The object reference of the PacketDefinition</param>
        /// <returns>The serialized string.</returns>
        public static string Serialize<TPackets>(TPackets packets) where TPackets : IEnumerable<PacketDefinition>
        {
            var deserializedPackets = new StringBuilder();
            foreach (var packet in packets)
            {
                try
                {
                    // load pregenerated serialization information
                    var serializationInformation = GetSerializationInformation(packet.GetType());

                    var deserializedPacket = new StringBuilder(serializationInformation.Key.Item2); // set header

                    var lastIndex = 0;
                    foreach (var packetBasePropertyInfo in serializationInformation.Value)
                    {
                        // check if we need to add a non mapped values (pseudovalues)
                        if (packetBasePropertyInfo.Key.Index > lastIndex + 1)
                        {
                            var amountOfEmptyValuesToAdd = packetBasePropertyInfo.Key.Index - (lastIndex + 1);

                            for (var i = 0; i < amountOfEmptyValuesToAdd; i++)
                            {
                                deserializedPacket.Append(" 0");
                            }
                        }

                        // add value for current configuration
                        deserializedPacket.Append(SerializeValue(packetBasePropertyInfo.Value.PropertyType,
                            packetBasePropertyInfo.Value.GetValue(packet),
                            packetBasePropertyInfo.Value.GetCustomAttributes<ValidationAttribute>(),
                            packetBasePropertyInfo.Key));

                        // check if the value should be serialized to end
                        if (packetBasePropertyInfo.Key.SerializeToEnd)
                        {
                            // we reached the end
                            break;
                        }

                        // set new index
                        lastIndex = packetBasePropertyInfo.Key.Index;
                    }

                    if (deserializedPackets.Length != 0)
                    {
                        deserializedPackets.Append('\uffff');
                    }

                    deserializedPackets.Append(deserializedPacket);
                }
                catch (Exception e)
                {
                   _logger.Warning(LogLanguage.Instance.GetMessageFromKey(LanguageKey.PACKET_WRONG_FORMAT), e);
                    return string.Empty;
                }
            }

            return deserializedPackets.ToString();
        }

        private static PacketDefinition Deserialize(string packetContent, PacketDefinition deserializedPacket,
            KeyValuePair<Tuple<Type, string>,
                Dictionary<PacketIndexAttribute, PropertyInfo>> serializationInformation,
            bool includesKeepAliveIdentity)
        {
            var matches = Regex.Matches(packetContent,
                @"([^\040]+[\.][^\040]+[\040]?)+((?=\040)|$)|([^\040]+)((?=\040)|$)");
            if (matches.Count > 0)
            {
                foreach (var packetBasePropertyInfo in serializationInformation.Value)
                {
                    var currentIndex =
                        packetBasePropertyInfo.Key.Index
                        + (includesKeepAliveIdentity ? 2
                            : 1); // adding 2 because we need to skip incrementing number and packet header

                    if (currentIndex < matches.Count + (includesKeepAliveIdentity ? 1 : 0))
                    {
                        if (packetBasePropertyInfo.Key.SerializeToEnd)
                        {
                            // get the value to the end and stop deserialization
                            var index = matches.Count > currentIndex ? matches[currentIndex].Index
                                : packetContent.Length;
                            var valueToEnd =
                                packetContent.Substring(index, packetContent.Length - index);
                            packetBasePropertyInfo.Value.SetValue(deserializedPacket,
                                DeserializeValue(packetBasePropertyInfo.Value.PropertyType, valueToEnd,
                                    packetBasePropertyInfo.Key,
                                    packetBasePropertyInfo.Value.GetCustomAttributes<ValidationAttribute>(), matches,
                                    includesKeepAliveIdentity));
                            break;
                        }

                        var currentValue = (currentIndex >= matches.Count) ? null : matches[currentIndex].Value;

                        // set the value & convert currentValue
                        packetBasePropertyInfo.Value.SetValue(deserializedPacket,
                            DeserializeValue(packetBasePropertyInfo.Value.PropertyType, currentValue,
                                packetBasePropertyInfo.Key,
                                packetBasePropertyInfo.Value.GetCustomAttributes<ValidationAttribute>(), matches,
                                includesKeepAliveIdentity));
                    }
                    else
                    {
                        break;
                    }
                }
            }

            return deserializedPacket;
        }

        private static IList DeserializeSimpleList(string currentValues, Type genericListType)
        {
            var subpackets = (IList)Convert.ChangeType(genericListType.CreateInstance<IList>(), genericListType);
            IEnumerable<string> splittedValues = currentValues.Split('.');

            foreach (var currentValue in splittedValues)
            {
                var value = DeserializeValue(genericListType.GenericTypeArguments[0], currentValue, null,
                    genericListType.GenericTypeArguments[0].GetCustomAttributes<ValidationAttribute>(), null);
                subpackets.Add(value);
            }

            return subpackets;
        }

        private static object DeserializeSubpacket(string currentSubValues, Type packetBasePropertyType,
            KeyValuePair<Tuple<Type, string>, Dictionary<PacketIndexAttribute, PropertyInfo>>
                subpacketSerializationInfo, bool isReturnPacket = false)
        {
            var subpacketValues = currentSubValues.Split(isReturnPacket ? '^' : '.');
            var newSubpacket = packetBasePropertyType.CreateInstance<object>();
            foreach (var subpacketPropertyInfo in subpacketSerializationInfo.Value)
            {
                var currentSubIndex = isReturnPacket ? subpacketPropertyInfo.Key.Index + 1
                    : subpacketPropertyInfo.Key.Index; // return packets do include header
                var currentSubValue = subpacketValues[currentSubIndex];

                subpacketPropertyInfo.Value.SetValue(newSubpacket,
                    DeserializeValue(subpacketPropertyInfo.Value.PropertyType, currentSubValue,
                        subpacketPropertyInfo.Key,
                        subpacketPropertyInfo.Value.GetCustomAttributes<ValidationAttribute>(), null));
            }

            return newSubpacket;
        }

        private static IList DeserializeSubpackets(string currentValue, Type packetBasePropertyType,
            bool shouldRemoveSeparator, MatchCollection packetMatchCollections, int? currentIndex,
            bool includesKeepAliveIdentity)
        {
            // split into single values
            var splittedSubpackets = currentValue.Split(' ').ToList();
            // generate new list
            var subpackets =
                (IList)Convert.ChangeType(packetBasePropertyType.CreateInstance<object>(), packetBasePropertyType);

            var subPacketType = packetBasePropertyType.GetGenericArguments()[0];
            var subpacketSerializationInfo = GetSerializationInformation(subPacketType);

            // handle subpackets with separator
            if (shouldRemoveSeparator)
            {
                if (!currentIndex.HasValue || packetMatchCollections == null)
                {
                    return subpackets;
                }

                var splittedSubpacketParts = packetMatchCollections.Select(m => m.Value).ToList();
                splittedSubpackets = new List<string>();

                var generatedPseudoDelimitedString = new StringBuilder();
                var subPacketTypePropertiesCount = subpacketSerializationInfo.Value.Count;

                // check if the amount of properties can be serialized properly
                if ((splittedSubpacketParts.Count + (includesKeepAliveIdentity ? 1 : 0))
                    % subPacketTypePropertiesCount
                    == 0) // amount of properties per subpacket does match the given value amount in %
                {
                    for (var i = currentIndex.Value + 1 + (includesKeepAliveIdentity ? 1 : 0);
                        i < splittedSubpacketParts.Count;
                        i++)
                    {
                        int j;
                        for (j = i; j < i + subPacketTypePropertiesCount; j++)
                        {
                            // add delimited value
                            generatedPseudoDelimitedString.Append(splittedSubpacketParts[j]).Append(".");
                        }

                        i = j - 1;

                        //remove last added separator
                        generatedPseudoDelimitedString.TrimEnd('.');

                        // add delimited values to list of values to serialize
                        splittedSubpackets.Add(generatedPseudoDelimitedString.ToString());
                        generatedPseudoDelimitedString = new StringBuilder();
                    }
                }
                else
                {
                    throw new ArgumentNullException(LogLanguage.Instance.GetMessageFromKey(LanguageKey.AMOUNT_SPLITTED_SUBPACKET_INCORRECT));
                }
            }

            foreach (var subpacket in splittedSubpackets)
            {
                subpackets.Add(DeserializeSubpacket(subpacket, subPacketType, subpacketSerializationInfo));
            }

            return subpackets;
        }

        private static object DeserializeValue(Type packetPropertyType, string currentValue,
            PacketIndexAttribute packetIndexAttribute, IEnumerable<ValidationAttribute> validationAttributes,
            MatchCollection packetMatches,
            bool includesKeepAliveIdentity = false)
        {
            var value = currentValue;
            validationAttributes.ToList().ForEach(s =>
            {
                if (!s.IsValid(value))
                {
                    throw new ValidationException(s.ErrorMessage);
                }
            });
            // check for empty value and cast it to null
            if (currentValue == "-1" || currentValue == "-" || currentValue == "NONE")
            {
                currentValue = null;
            }

            // enum should be casted to number
            if (packetPropertyType.BaseType?.Equals(typeof(Enum)) ?? false)
            {
                object convertedValue = null;
                try
                {
                    if (currentValue != null
                        && packetPropertyType.IsEnumDefined(Enum.Parse(packetPropertyType, currentValue)))
                    {
                        convertedValue = Enum.Parse(packetPropertyType, currentValue);
                    }
                }
                catch (Exception)
                {
                   _logger.Warning(LogLanguage.Instance.GetMessageFromKey(LanguageKey.NOT_CONVERT_VALUE));
                }

                return convertedValue;
            }

            if (packetPropertyType == typeof(bool)) // handle boolean values
            {
                return currentValue != "0";
            }

            if (packetPropertyType.BaseType?.Equals(typeof(PacketDefinition)) ?? false) // subpacket
            {
                var subpacketSerializationInfo = GetSerializationInformation(packetPropertyType);
                return DeserializeSubpacket(currentValue, packetPropertyType, subpacketSerializationInfo,
                    packetIndexAttribute?.IsReturnPacket ?? false);
            }

            if (packetPropertyType.IsGenericType
                && packetPropertyType.GetGenericTypeDefinition().IsAssignableFrom(typeof(List<>)) // subpacket list
                && packetPropertyType.GenericTypeArguments[0].BaseType == typeof(PacketDefinition))
            {
                return DeserializeSubpackets(currentValue, packetPropertyType,
                    packetIndexAttribute?.RemoveSeparator ?? false, packetMatches, packetIndexAttribute?.Index,
                    includesKeepAliveIdentity);
            }

            if (packetPropertyType.IsGenericType
                && packetPropertyType.GetGenericTypeDefinition().IsAssignableFrom(typeof(List<>))) // simple list
            {
                return DeserializeSimpleList(currentValue, packetPropertyType);
            }

            if (Nullable.GetUnderlyingType(packetPropertyType) != null && string.IsNullOrEmpty(currentValue)
            ) // empty nullable value
            {
                return null;
            }

            if (Nullable.GetUnderlyingType(packetPropertyType) != null) // nullable value
            {
                if (packetPropertyType.GenericTypeArguments[0]?.BaseType == typeof(Enum))
                {
                    return Enum.Parse(packetPropertyType.GenericTypeArguments[0], currentValue);
                }

                return TypeDescriptor.GetConverter(packetPropertyType.GenericTypeArguments[0])
                    .ConvertFromInvariantString(currentValue);
            }

            if (packetPropertyType == typeof(string) && string.IsNullOrEmpty(currentValue)
                && !packetIndexAttribute.SerializeToEnd)
            {
                throw new ArgumentNullException(nameof(currentValue));
            }

            if (packetPropertyType == typeof(string) && currentValue == null)
            {
                currentValue = string.Empty;
            }

            return Convert.ChangeType(currentValue, packetPropertyType); // cast to specified type
        }

        private static void GenerateSerializationInformations<TPacketDefinition>()
        where TPacketDefinition : PacketDefinition
        {
            _packetSerializationInformations =
                new Dictionary<Tuple<Type, string>, Dictionary<PacketIndexAttribute, PropertyInfo>>();

            // Iterate thru all PacketDefinition implementations
            foreach (var packetBaseType in typeof(TPacketDefinition).Assembly.GetTypes().Where(p =>
                !p.IsInterface && typeof(TPacketDefinition).BaseType.IsAssignableFrom(p)))
            {
                // add to serialization informations
                GenerateSerializationInformations(packetBaseType);
            }
        }

        private static KeyValuePair<Tuple<Type, string>, Dictionary<PacketIndexAttribute, PropertyInfo>>
            GenerateSerializationInformations(Type serializationType)
        {
            var header = serializationType.GetCustomAttribute<PacketHeaderAttribute>()?.Identification;

            if (string.IsNullOrEmpty(header))
            {
                throw new ArgumentNullException(LogLanguage.Instance.GetMessageFromKey(LanguageKey.PACKET_HEADER_CANNOT_EMPTY));
            }

            var packetsForPacketDefinition = new Dictionary<PacketIndexAttribute, PropertyInfo>();

            foreach (var packetBasePropertyInfo in serializationType.GetProperties()
                .Where(x => x.GetCustomAttributes(true).OfType<PacketIndexAttribute>().Any()))
            {
                var indexAttribute = packetBasePropertyInfo.GetCustomAttributes(true).OfType<PacketIndexAttribute>()
                    .FirstOrDefault();

                if (indexAttribute != null)
                {
                    packetsForPacketDefinition.Add(indexAttribute, packetBasePropertyInfo);
                }
            }

            var serializationInformatin =
                new KeyValuePair<Tuple<Type, string>, Dictionary<PacketIndexAttribute, PropertyInfo>>(
                    new Tuple<Type, string>(serializationType, header), packetsForPacketDefinition);
            _packetSerializationInformations.Add(serializationInformatin.Key, serializationInformatin.Value);

            return serializationInformatin;
        }

        private static KeyValuePair<Tuple<Type, string>, Dictionary<PacketIndexAttribute, PropertyInfo>>
            GetSerializationInformation(Type serializationType)
        {
            return _packetSerializationInformations.Any(si => si.Key.Item1 == serializationType)
                ? _packetSerializationInformations.SingleOrDefault(si => si.Key.Item1 == serializationType)
                : GenerateSerializationInformations(
                    serializationType); // generic runtime serialization parameter generation
        }

        private static string SerializeSimpleList(IList listValues, Type propertyType)
        {
            var resultListPacket = new StringBuilder();
            var listValueCount = listValues.Count;
            if (listValueCount > 0)
            {
                resultListPacket.Append(SerializeValue(propertyType.GenericTypeArguments[0], listValues[0],
                    propertyType.GenericTypeArguments[0].GetCustomAttributes<ValidationAttribute>()));

                for (var i = 1; i < listValueCount; i++)
                {
                    resultListPacket.Append(".")
                        .Append(SerializeValue(
                                    propertyType.GenericTypeArguments[0],
                                    listValues[i], propertyType.GenericTypeArguments[0].GetCustomAttributes<ValidationAttribute>())
                        .Replace(" ", ""));
                }
            }

            return resultListPacket.ToString();
        }

        //TODO move this to a proper extension
        private static StringBuilder TrimEnd(this StringBuilder sb, char character = ' ')
        {
            if (sb == null || sb.Length == 0)
            {
                return sb;
            }

            var i = sb.Length - 1;
            for (; i >= 0; i--)
            {
                if (character != sb[i])
                {
                    break;
                }
            }

            if (i < sb.Length - 1)
            {
                sb.Length = i + 1;
            }

            return sb;
        }

        private static string SerializeSubpacket(object value,
            KeyValuePair<Tuple<Type, string>, Dictionary<PacketIndexAttribute, PropertyInfo>>
                subpacketSerializationInfo, bool isReturnPacket,
            bool shouldRemoveSeparator, string specialSeparator)
        {
            var serializedSubpacket =
                new StringBuilder(isReturnPacket ? $" #{subpacketSerializationInfo.Key.Item2}^" : " ");

            // iterate thru configure subpacket properties
            foreach (var subpacketPropertyInfo in subpacketSerializationInfo.Value)
            {
                // first element
                if (subpacketPropertyInfo.Key.Index != 0)
                {
                    serializedSubpacket.Append(isReturnPacket ? "^" : shouldRemoveSeparator ? " "
                        : (specialSeparator != "." ? specialSeparator : subpacketPropertyInfo.Key.SpecialSeparator));
                }

                if (typeof(PacketDefinition).IsAssignableFrom(subpacketPropertyInfo.Value.PropertyType))
                {
                    var subpacketSerializationInfo2 =
                        GetSerializationInformation(subpacketPropertyInfo.Value.PropertyType);
                    var valuesub = subpacketPropertyInfo.Value.GetValue(value);
                    serializedSubpacket = serializedSubpacket.TrimEnd();
                    var subpacket = SerializeSubpacket(valuesub, subpacketSerializationInfo2, false,
                        subpacketPropertyInfo.Key.RemoveSeparator, isReturnPacket ? "^" :
                            subpacketPropertyInfo.Key.SpecialSeparator ?? specialSeparator);
                    if (isReturnPacket)
                    {
                        subpacket = subpacket.TrimStart();
                    }
                    serializedSubpacket.Append(subpacket);
                    continue;
                }

                serializedSubpacket.Append(SerializeValue(subpacketPropertyInfo.Value.PropertyType,
                    subpacketPropertyInfo.Value.GetValue(value),
                    subpacketPropertyInfo.Value.GetCustomAttributes<ValidationAttribute>()).Replace(" ", ""));
            }

            serializedSubpacket.Replace("^^", "^");
            return serializedSubpacket.ToString();
        }

        private static string SerializeSubpackets(IList listValues, Type packetBasePropertyType,
            bool shouldRemoveSeparator, string specialSeparator)
        {
            var serializedSubPacket = new StringBuilder();
            var subpacketSerializationInfo =
                GetSerializationInformation(packetBasePropertyType.GetGenericArguments()[0]);

            if (listValues.Count > 0)
            {
                foreach (var listValue in listValues)
                {
                    serializedSubPacket.Append(SerializeSubpacket(listValue, subpacketSerializationInfo, false,
                        shouldRemoveSeparator, specialSeparator));
                }
            }

            return serializedSubPacket.ToString();
        }

        private static string SerializeValue(Type propertyType, object value,
            IEnumerable<ValidationAttribute> validationAttributes, PacketIndexAttribute packetIndexAttribute = null)
        {
            if (propertyType == null || !validationAttributes.All(a => a.IsValid(value)))
            {
                return string.Empty;
            }

            if ((packetIndexAttribute?.IsOptional ?? false) && string.IsNullOrEmpty(Convert.ToString(value)))
            {
                return string.Empty;
            }

            // check for nullable without value or string
            if (propertyType == typeof(string) && string.IsNullOrEmpty(Convert.ToString(value)))
            {
                return " -";
            }

            if (Nullable.GetUnderlyingType(propertyType) != null && string.IsNullOrEmpty(Convert.ToString(value)))
            {
                return " -1";
            }

            // enum should be casted to number
            if ((propertyType.BaseType?.Equals(typeof(Enum)) ?? false) || (Nullable.GetUnderlyingType(propertyType)?.IsEnum ?? false))
            {
                return $" {Convert.ToInt16(value)}";
            }

            if (propertyType == typeof(bool))
            {
                // bool is 0 or 1 not True or False
                return Convert.ToBoolean(value) ? " 1" : " 0";
            }

            if (propertyType.BaseType?.Equals(typeof(PacketDefinition)) ?? false)
            {
                var subpacketSerializationInfo = GetSerializationInformation(propertyType);
                return SerializeSubpacket(value, subpacketSerializationInfo,
                    packetIndexAttribute?.IsReturnPacket ?? false, packetIndexAttribute?.RemoveSeparator ?? false,
                    packetIndexAttribute?.SpecialSeparator);
            }

            if (value is PacketDefinition)
            {
                var subpacketSerializationInfo = GetSerializationInformation(value.GetType());
                return SerializeSubpacket(value, subpacketSerializationInfo,
                    packetIndexAttribute?.IsReturnPacket ?? false, packetIndexAttribute?.RemoveSeparator ?? false,
                    packetIndexAttribute?.SpecialSeparator);
            }

            if (propertyType.IsGenericType
                && propertyType.GetGenericTypeDefinition().IsAssignableFrom(typeof(List<>))
                && propertyType.GenericTypeArguments[0].BaseType == typeof(PacketDefinition))
            {
                return SerializeSubpackets((IList)value, propertyType,
                    packetIndexAttribute?.RemoveSeparator ?? false, packetIndexAttribute?.SpecialSeparator);
            }

            if (propertyType.IsGenericType
                && propertyType.GetGenericTypeDefinition().IsAssignableFrom(typeof(List<>))) //simple list
            {
                return SerializeSimpleList((IList)value, propertyType);
            }

            return $" {value}";
        }

        private static void SetDeserializationInformations(PacketDefinition packetDefinition,
            string packetContent, string packetHeader)
        {
            packetDefinition.OriginalContent = packetContent;
            packetDefinition.OriginalHeader = packetHeader;
        }

        #endregion
    }
}