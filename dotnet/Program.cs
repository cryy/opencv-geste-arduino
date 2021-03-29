using Newtonsoft.Json;
using System;
using System.IO.Ports;
using System.Threading.Tasks;

namespace Geste
{
    class Program
    {
        static SerialPort _serialPort;
        public static void Main()
            => MainAsync().GetAwaiter().GetResult();

        public static async Task MainAsync()
        {
            _serialPort = new SerialPort();
            _serialPort.PortName = "COM3";
            _serialPort.BaudRate = 9600;
            _serialPort.Open();

            _ = Task.Run(() =>
            {
                _serialPort.WriteLine(JsonConvert.SerializeObject(new
                {
                    t = 5,
                    d = new {}
                }));
            });

            await Task.Delay(-1);
        }
    }
}
