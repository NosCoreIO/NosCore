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

using System;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace NosCore.Core.MessageQueue;
public class PolymorphicJsonConverter<T> : JsonConverter<T>
{
    //Inspired by https://learn.microsoft.com/en-us/dotnet/standard/serialization/system-text-json/converters-how-to?pivots=dotnet-6-0#support-polymorphic-deserialization
    public override bool CanConvert(Type typeToConvert)
    {
        return typeof(T).IsAssignableFrom(typeToConvert);
    }

    public override T? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        if (reader.TokenType != JsonTokenType.StartObject)
        {
            throw new JsonException();
        }

        reader.Read();
        if (reader.TokenType != JsonTokenType.PropertyName)
        {
            throw new JsonException();
        }

        string? propertyName = reader.GetString();

        if (propertyName != "_type")
        {
            throw new JsonException();
        }

        reader.Read();
        if (reader.TokenType != JsonTokenType.String)
        {
            throw new JsonException();
        }
        var type = reader.GetString();

        //Check type exists
        var tp = Type.GetType(type ?? throw new InvalidOperationException());

        reader.Read();
        if (reader.TokenType != JsonTokenType.PropertyName)
        {
            throw new JsonException();
        }

        propertyName = reader.GetString();

        if (propertyName != "_object")
        {
            throw new JsonException();
        }

        var rt = (T?)JsonSerializer.Deserialize(ref reader, tp ?? throw new InvalidOperationException(), new JsonSerializerOptions());

        while (reader.Read())
        {
            if (reader.TokenType == JsonTokenType.EndObject) return rt;
        }
        return rt;
    }

    public override void Write(Utf8JsonWriter writer, T value, JsonSerializerOptions options)
    {
        writer.WriteStartObject();
        writer.WriteString("_type", value?.GetType().AssemblyQualifiedName ?? throw new InvalidOperationException());
        writer.WritePropertyName("_object");
        JsonSerializer.Serialize(writer, value, value.GetType());
        writer.WriteEndObject();
    }
}