using System;
using System.Collections.Generic;
using System.Text.Json;

namespace PolygonIo.WebSocket.Deserializers
{
    public partial class Utf8JsonDeserializer : IPolygonDeserializer
    {
        private decimal[] ExpectDecimalArray(ref Utf8JsonReader reader, string propertyName)
        {
            Expect(ref reader, JsonTokenType.StartArray);

            var items = new List<decimal>();

            while (Expect(ref reader, JsonTokenType.Number, JsonTokenType.EndArray))
            {
                items.Add(reader.GetDecimal());
            }
            return items.ToArray();
        }

        private int[] ExpectIntArray(ref Utf8JsonReader reader, string propertyName)
        {
            Expect(ref reader, JsonTokenType.StartArray);

            var items = new List<int>();

            while (Expect(ref reader, JsonTokenType.Number, JsonTokenType.EndArray))
            {
                items.Add(reader.GetInt32());
            }
            return items.ToArray();
        }

        private string ExpectString(ref Utf8JsonReader reader, string propertyName)
        {
            Expect(ref reader, JsonTokenType.String, propertyName);
            return reader.GetString();
        }

        private decimal ExpectDecimal(ref Utf8JsonReader reader, string propertyName)
        {
            Expect(ref reader, JsonTokenType.Number, propertyName);
            return reader.GetDecimal();
        }

        private DateTimeOffset ExpectUnixTimeMilliseconds(ref Utf8JsonReader reader, string propertyName)
        {
            Expect(ref reader, JsonTokenType.Number, propertyName);
            var count = reader.GetInt64();
            return DateTimeOffset.FromUnixTimeMilliseconds(count);
        }

        private int ExpectInt(ref Utf8JsonReader reader, string propertyName)
        {
            Expect(ref reader, JsonTokenType.Number, propertyName);
            return reader.GetInt32();
        }

        private void Expect(ref Utf8JsonReader reader, JsonTokenType t, string propertyName)
        {
            reader.Read();

            if (reader.TokenType != t)
            {
                throw new JsonException($"expected {t.ToString()} for property \"{propertyName}\" found {reader.TokenType.ToString()} at position {reader.Position.GetInteger()}");
            }
        }

        private void Expect(ref Utf8JsonReader reader, JsonTokenType t)
        {
            reader.Read();

            if (reader.TokenType != t)
            {
                throw new JsonException($"expected {t.ToString()} found {reader.TokenType.ToString()} at position {reader.Position.GetInteger()}");
            }
        }

        private void ExpectNamedProperty(ref Utf8JsonReader reader, string expectedPropertyName)
        {
            Expect(ref reader, JsonTokenType.PropertyName, expectedPropertyName);

            var foundPropertyName = reader.GetString();

            if (foundPropertyName != StreamFieldNames.Event)
                throw new JsonException($"expected {StreamFieldNames.Event} found {foundPropertyName} at position {reader.Position.GetInteger()}");
        }

        private bool Expect(ref Utf8JsonReader reader, JsonTokenType a, JsonTokenType b)
        {
            reader.Read();

            if (reader.TokenType == a)
                return true;
            if (reader.TokenType == b)
                return false;

            throw new JsonException($"expected {a.ToString()} found {b.ToString()} at position {reader.Position.GetInteger()}");
        }
    }
}
