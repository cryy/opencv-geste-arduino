namespace Geste.Configuration
{
    public partial class Config
    {
        public static Config Default = new()
        {
            VideoSource = "",
            ShowFeed = true,
            PortName = "COM3",
            BaudRate = 9600,
            HSLSkinHigh = new[] {15, 80, 60},
            HSLSkinLow = new[] {0, 10, 5}
        };
    }
}