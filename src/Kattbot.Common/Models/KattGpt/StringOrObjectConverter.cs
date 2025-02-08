using System;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Kattbot.Common.Models.KattGpt;

public class StringOrObjectConverter<T> : JsonConverter<StringOrObject<T>>
    where T : class, new()
{
    public override StringOrObject<T> Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        switch (reader.TokenType)
        {
            case JsonTokenType.String:
                return new StringOrObject<T>(reader.GetString());
            case JsonTokenType.StartObject:
            {
                var obj = JsonSerializer.Deserialize<T>(ref reader, options);
                return new StringOrObject<T>(obj);
            }

            default:
                throw new JsonException("Unexpected token type");
        }
    }

    public override void Write(Utf8JsonWriter writer, StringOrObject<T> value, JsonSerializerOptions options)
    {
        if (value.IsString)
        {
            writer.WriteStringValue(value.StringValue);
        }
        else if (value.IsObject)
        {
            JsonSerializer.Serialize(writer, value.ObjectValue, options);
        }
        else
        {
            throw new JsonException("Unexpected value type");
        }
    }
}
