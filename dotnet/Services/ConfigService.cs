using System;
using System.IO;
using System.Threading.Tasks;
using Geste.Configuration;
using Newtonsoft.Json;
using Serilog.Core;

namespace Geste.Services
{
    public class ConfigService
    {
        private readonly Logger _logger;


        public string _configDir;
        public string _configFileDir;

        public ConfigService(DirectoryService dir, Logger logger)
        {
            _logger = logger;

            _configDir = Path.Combine(dir.SingleFilePath, "konfiguracija");
            _configFileDir = Path.Combine(_configDir, "config.json");
        }

        public Config Config { get; private set; }

        public async Task<Config> LoadAsync()
        {
            if (!Directory.Exists(_configDir) || !File.Exists(_configFileDir))
            {
                _logger.Information("");
                _logger.Information("Detektirano prvo pokretanje.");
                _logger.Information("Konfigurirajte vrijednosti konfiguracije {ConfigFileName}.", "config.json");
                _logger.Information("Zatvorite ovaj program i ponovo pokrenite kada završite.");
                _logger.Information("");

                Directory.CreateDirectory(_configDir);
                await CreateDefaultAsync();


                Console.ReadKey();
                Environment.Exit(0);
                return null;
            }


            var contents = await File.ReadAllTextAsync(_configFileDir);
            var config = JsonConvert.DeserializeObject<Config>(contents);

            Config = config;

            return Config;
        }

        private async Task CreateDefaultAsync()
        {
            await File.WriteAllTextAsync(_configFileDir,
                JsonConvert.SerializeObject(Config.Default, Formatting.Indented));
        }
    }
}