//  __  _  __    __   ___ __  ___ ___
// |  \| |/__\ /' _/ / _//__\| _ \ __|
// | | ' | \/ |`._`.| \_| \/ | v / _|
// |_|\__|\__/ |___/ \__/\__/|_|_\___|
//

using System;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace NosCore.GameObject.InterChannelCommunication;

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
