using Newtonsoft.Json;

namespace Geste.Messaging.Models
{
    public sealed class ArduinoMessageModel
    {
        [JsonProperty("m")] public string String { get; set; }
    }
}