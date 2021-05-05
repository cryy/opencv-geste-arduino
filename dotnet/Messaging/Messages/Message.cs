using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Geste.Messaging
{
    public class Message
    {
        [JsonProperty("t")] public OpCode OpCode { get; set; }

        [JsonProperty("d", NullValueHandling = NullValueHandling.Ignore)]
        public JToken Data { get; set; }
    }
}