using System;
using System.IO.Ports;
using System.Threading;
using System.Threading.Tasks;
using Geste.Messaging;
using Geste.Messaging.Models;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Serilog.Core;

namespace Geste.Services
{
    public class ArduinoService
    {
        private readonly ConfigService _configService;
        private readonly Logger _logger;

        private Task _readTask;

        private CancellationTokenSource _readTaskCTS;

        private SerialPort _serialPort;
        private Task _writeTask;
        private CancellationTokenSource _writeTaskCTS;

        public ArduinoService(ConfigService configService, Logger logger)
        {
            _configService = configService;
            _logger = logger;
        }

        private Task Read()
        {
            while (true)
            {
                var readResult = _serialPort.ReadLine();

                Console.WriteLine(readResult);
                if (!string.IsNullOrWhiteSpace(readResult)) HandleReadMessage(JObject.Parse(readResult));
            }
        }

        private void HandleReadMessage(JObject message)
        {
            var opcode = message["t"].ToObject<OpCode>();

            switch (opcode)
            {
                case OpCode.Hello:
                    _logger.Information("Arduino je poslao {OpCode}.", "HELLO");
                    break;
                case OpCode.ArduinoMessage:
                {
                    var arduinoMessage = message["d"].ToObject<ArduinoMessageModel>();
                    _logger.Information($"{{Prefix}}  {arduinoMessage.String}", "Arduino");
                    break;
                }
                default:
                    _logger.Warning("Nepoznata opcode valuta {Value}.", opcode);
                    break;
            }
        }

        private void StartReadTask()
        {
            if (_readTask != null)
            {
                _readTaskCTS.Cancel();
                _readTaskCTS.Dispose();
            }

            _readTaskCTS = new CancellationTokenSource();
            _readTask = Task.Run(Read, _readTaskCTS.Token);
        }


        // inicijalizacija SerialPorta        
        public async Task<bool> InitializeAsync()
        {
            try
            {
                var serialPort = new SerialPort
                {
                    PortName = _configService.Config.PortName,
                    BaudRate = _configService.Config.BaudRate
                };

                serialPort.Open();

                _serialPort = serialPort;

                _logger.Information("Veza s arduinom uspješno uspostavljena.");

                return true;
            }
            catch (Exception e)
            {
                _logger.Error(e, "Dogodio se error tokom otvaranja SerialPorta.");
                return false;
            }
        }

        public void Run()
        {
            StartReadTask();
            _logger.Information("Započinjem čitanje arduinovog memory streama.");
        }

        public void Write(Message message)
        {
            var serialized = JsonConvert.SerializeObject(message);
            _serialPort.WriteLine(serialized);
        }
    }
}