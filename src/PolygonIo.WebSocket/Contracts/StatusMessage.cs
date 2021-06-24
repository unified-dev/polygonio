
using System.Text.Json.Serialization;

namespace PolygonIo.WebSocket.Contracts
{
    public struct StatusMessage
    {
        [JsonPropertyName("status")]
        public string Status { get; set; }
        [JsonPropertyName("message")]
        public string Message { get; set; }
    }
}
