using Newtonsoft.Json;

namespace Geste.Messaging.Models
{
    public class HandUpdateMessageModel
    {
        [JsonProperty("fingers")] public int Fingers { get; set; }

        [JsonProperty("contour_available")] public bool ContourAvailable { get; set; }

        [JsonProperty("indices_available")] public bool IndicesAvailable { get; set; }
    }
}