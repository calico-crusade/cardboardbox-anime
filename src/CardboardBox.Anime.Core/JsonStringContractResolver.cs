namespace CardboardBox.Anime.Core;

public class JsonStringContractResolver : JsonConverter<string>
{
    public override string? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        if (reader.TokenType == JsonTokenType.Number)
            return reader.TryGetInt64(out long value) 
                ? value.ToString() 
                : reader.GetDouble().ToString();

        if (reader.TokenType == JsonTokenType.String)
            return reader.GetString();

        using var document = JsonDocument.ParseValue(ref reader);
        return document.RootElement.ToString();
    }

    public override void Write(Utf8JsonWriter writer, string value, JsonSerializerOptions options)
    {
        writer.WriteStringValue(value.ToString());
    }
}
