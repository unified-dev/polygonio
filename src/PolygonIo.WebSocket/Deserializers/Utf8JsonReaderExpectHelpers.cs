using System;
using System.Collections.Generic;
using System.Text.Json;

namespace PolygonIo.WebSocket.Deserializers
{
    static internal class Utf8JsonReaderExpectHelpers
    {
        static public decimal[] ExpectDecimalArray(this ref Utf8JsonReader reader, string propertyName)
        {
            reader.Expect(JsonTokenType.StartArray);

            var items = new List<decimal>();

            while (reader.Expect(JsonTokenType.Number, JsonTokenType.EndArray))
            {
                items.Add(reader.GetDecimal());
            }
            return items.ToArray();
        }

        static public int[] ExpectIntArray(this ref Utf8JsonReader reader, string propertyName)
        {
            reader.Expect(JsonTokenType.StartArray);

            var items = new List<int>();

            while (reader.Expect(JsonTokenType.Number, JsonTokenType.EndArray))
            {
                items.Add(reader.GetInt32());
            }
            return items.ToArray();
        }

        static public string ExpectString(this ref Utf8JsonReader reader, string propertyName)
        {
            reader.Expect(JsonTokenType.String, propertyName);
            return reader.GetString();
        }

        static public decimal ExpectDecimal(this ref Utf8JsonReader reader, string propertyName)
        {
            reader.Expect(JsonTokenType.Number, propertyName);
            return reader.GetDecimal();
        }

        static public DateTimeOffset ExpectUnixTimeMilliseconds(this ref Utf8JsonReader reader, string propertyName)
        {
            reader.Expect(JsonTokenType.Number, propertyName);
            var count = reader.GetInt64();
            return DateTimeOffset.FromUnixTimeMilliseconds(count);
        }

        static public int ExpectInt(this ref Utf8JsonReader reader, string propertyName)
        {
            reader.Expect(JsonTokenType.Number, propertyName);
            return reader.GetInt32();
        }

        static public void Expect(this ref Utf8JsonReader reader, JsonTokenType t, string propertyName)
        {
            reader.Read();

            if (reader.TokenType != t)
            {
                throw new JsonException($"expected {t} for property \"{propertyName}\" found {reader.TokenType} at position {reader.Position.GetInteger()}");
            }
        }

        static public void Expect(this ref Utf8JsonReader reader, JsonTokenType t)
        {
            reader.Read();

            if (reader.TokenType != t)
            {
                throw new JsonException($"expected {t} found {reader.TokenType} at position {reader.Position.GetInteger()}");
            }
        }

        static public void ExpectNamedProperty(this ref Utf8JsonReader reader, string expectedPropertyName)
        {
            reader.Expect(JsonTokenType.PropertyName, expectedPropertyName);

            var foundPropertyName = reader.GetString();

            if (foundPropertyName != StreamFieldNames.Event)
                throw new JsonException($"expected {StreamFieldNames.Event} found {foundPropertyName} at position {reader.Position.GetInteger()}");
        }

        static public bool Expect(this ref Utf8JsonReader reader, JsonTokenType a, JsonTokenType b)
        {
            reader.Read();

            if (reader.TokenType == a)
                return true;
            if (reader.TokenType == b)
                return false;

            throw new JsonException($"expected {a} found {b} at position {reader.Position.GetInteger()}");
        }

        static public bool SkipUpTo(this ref Utf8JsonReader reader, JsonTokenType a)
        {
            while (reader.Read())
            {
                if (reader.TokenType == a)
                    return true;
            }
            return false;
        }

        static public bool SkipTillExpected(this ref Utf8JsonReader reader, JsonTokenType a, JsonTokenType b)
        {
            while (reader.Read())
            {
                if (reader.TokenType == a)
                    return true;
            }

            if (reader.TokenType == b)
                return false;

            throw new JsonException($"expected {a} found {b} at position {reader.Position.GetInteger()}");
        }
    }
}
