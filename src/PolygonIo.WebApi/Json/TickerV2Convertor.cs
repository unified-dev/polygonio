using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using Newtonsoft.Json.Linq;
using PolygonIo.WebApi.Model;
using System;

namespace PolygonIo.WebApi.Json
{
    class TickerV2Convertor : CustomCreationConverter<TickerV2>
	{
		public override TickerV2 Create(Type objectType)
		{
			throw new NotImplementedException();
		}

		public TickerV2 Create(Type objectType, JObject jObject)
		{
			var type = (PrimaryExch)(int)jObject.Property("PrimaryExch");

			if (type == PrimaryExch.FX)
				return new ForeignExchange();
			else
				return new Stock();
		}

		public override object ReadJson(JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer)
		{
			
			JObject jObject = JObject.Load(reader); // Load JObject from stream 
			var target = Create(objectType, jObject); // Create target object based on JObject 
			serializer.Populate(jObject.CreateReader(), target); // Populate the object properties 

			return target;
		}
	}
}
