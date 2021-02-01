using Newtonsoft.Json;
using System;

namespace PolygonIo.WebApi.Json
{
    public class UppercaseStringConverter : JsonConverter
	{
		public override bool CanConvert(Type objectType)
		{
			return objectType == typeof(string);
		}

		public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer)
		{
			if (value is string str)
				writer.WriteValue(str.ToUpper());
			else
				throw new JsonSerializationException("Expected date object value.");
		}

		public override object ReadJson(JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer)
		{
			bool nullable = Nullable.GetUnderlyingType(objectType) != null;

			if (reader.TokenType == JsonToken.Null)
			{
				if (!nullable)
				{
					throw new JsonSerializationException($"Cannot convert null value to {objectType}.");
				}
				return null;
			}

			if (reader.TokenType == JsonToken.String)
				return ((string)reader.Value).ToUpper();
			else
				throw new JsonSerializationException($"Unexpected token parsing date. Expected String, got {reader.TokenType}.");
		}
	}
}
