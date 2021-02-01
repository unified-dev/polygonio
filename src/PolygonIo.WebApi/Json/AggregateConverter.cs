namespace PolygonIo.WebApi.Json
{
    /*
    public class AggregateConverter : JsonConverter
    {
        public override object ReadJson(JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer)
        {
            JObject obj = JObject.Load(reader);

            var timestampStart = DateTimeOffset
                                    .FromUnixTimeMilliseconds((long)obj["s"])
                                    .UtcDateTime;

            var timestampEnd = DateTimeOffset
                                    .FromUnixTimeMilliseconds((long)obj["e"])
                                    .UtcDateTime;

            return new Aggregate()
            {
                Symbol = (string)obj["sym"],
                DateTime = timestampStart,
                EndDateTime = timestampEnd,
                Open = (decimal)obj["o"],
                High = (decimal)obj["h"],
                Low = (decimal)obj["l"],
                Close = (decimal)obj["c"],
                Volume = (int)obj["v"]
            };
        }

        public override bool CanConvert(Type objectType) => objectType == typeof(Aggregate);

        public override bool CanWrite => false;

        public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer) => throw new NotImplementedException();
    }*/

}
