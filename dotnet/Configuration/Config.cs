using Geste.Attributes;
using Newtonsoft.Json;

namespace Geste.Configuration
{
    public partial class Config
    {
        [JsonProperty("video_source", Required = Required.Always)]
        [LoggableSetting("Izvor videa")]
        public string VideoSource { get; set; }

        [JsonProperty("show_feed", Required = Required.Always)]
        [LoggableSetting("Pokaži feed")]
        public bool ShowFeed { get; set; }

        [JsonProperty("port_name", Required = Required.Always)]
        [LoggableSetting("Ime porta")]
        public string PortName { get; set; }

        [JsonProperty("baud_rate", Required = Required.Always)]
        [LoggableSetting("Baud rate")]
        public int BaudRate { get; set; }

        [JsonProperty("skin_hsl_low", Required = Required.Always)]
        [LoggableSetting("HSL za kožu (donja granica)")]
        public int[] HSLSkinLow { get; set; }

        [JsonProperty("skin_hsl_high", Required = Required.Always)]
        [LoggableSetting("HSL za kožu (gornja granica)")]
        public int[] HSLSkinHigh { get; set; }
    }
}