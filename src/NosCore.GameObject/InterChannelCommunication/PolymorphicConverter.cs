//  __  _  __    __   ___ __  ___ ___
// |  \| |/__\ /' _/ / _//__\| _ \ __|
// | | ' | \/ |`._`.| \_| \/ | v / _|
// |_|\__|\__/ |___/ \__/\__/|_|_\___|
//

using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace NosCore.GameObject.InterChannelCommunication;

public class PolymorphicJsonConverter<T> : JsonConverter<T>
{
    //Inspired by https://learn.microsoft.com/en-us/dotnet/standard/serialization/system-text-json/converters-how-to?pivots=dotnet-6-0#support-polymorphic-deserialization
    private JsonSerializerOptions? _payloadOptions;

    private readonly Dictionary<string, Type> _allowedTypes;

    public PolymorphicJsonConverter() : this(typeof(T).Assembly)
    {
    }

    // Only a generic parameter has no full name, and those are filtered out before this is called.
    private static string NameOf(Type type) => type.FullName ?? type.Name;

    // Only concrete implementations of T declared in these assemblies can be named by a payload.
    public PolymorphicJsonConverter(params Assembly[] messageAssemblies)
    {
        _allowedTypes = messageAssemblies
            .SelectMany(assembly => assembly.GetTypes())
            .Where(type => typeof(T).IsAssignableFrom(type) && type is { IsAbstract: false, IsInterface: false, ContainsGenericParameters: false })
            .ToDictionary(NameOf, StringComparer.Ordinal);
    }

    // The payload keeps the caller's converters (NodaTime among them) but not this one, which would wrap it again.
    private JsonSerializerOptions PayloadOptions(JsonSerializerOptions options)
    {
        if (_payloadOptions != null)
        {
            return _payloadOptions;
        }

        var payloadOptions = new JsonSerializerOptions(options);
        payloadOptions.Converters.Remove(this);
        return _payloadOptions = payloadOptions;
    }

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
        // Assembly-qualified names from older nodes are matched on their type name alone.
        var typeName = reader.GetString()?.Split(',')[0];
        if (typeName == null || !_allowedTypes.TryGetValue(typeName, out var tp))
        {
            throw new JsonException($"{typeName} is not a {typeof(T).Name} this node accepts");
        }

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

        var rt = (T?)JsonSerializer.Deserialize(ref reader, tp, PayloadOptions(options));

        while (reader.Read())
        {
            if (reader.TokenType == JsonTokenType.EndObject) return rt;
        }
        return rt;
    }

    public override void Write(Utf8JsonWriter writer, T value, JsonSerializerOptions options)
    {
        var type = value?.GetType() ?? throw new InvalidOperationException();
        var name = NameOf(type);
        if (!_allowedTypes.ContainsKey(name))
        {
            throw new JsonException($"{name} is not a {typeof(T).Name} this node accepts");
        }

        writer.WriteStartObject();
        writer.WriteString("_type", name);
        writer.WritePropertyName("_object");
        JsonSerializer.Serialize(writer, value, type, PayloadOptions(options));
        writer.WriteEndObject();
    }
}
