using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using BoggedFinanceBot;
using Geste.Messaging;
using Geste.Services;
using Microsoft.Extensions.DependencyInjection;
using Serilog;
using Serilog.Core;
using Serilog.Sinks.SystemConsole.Themes;

namespace Geste
{
    internal class Program
    {
        public static SystemConsoleTheme SerilogTheme = new(
            new Dictionary<ConsoleThemeStyle, SystemConsoleThemeStyle>
            {
                [ConsoleThemeStyle.Text] = new() {Foreground = ConsoleColor.Gray},
                [ConsoleThemeStyle.SecondaryText] =
                    new() {Foreground = ConsoleColor.DarkGray},
                [ConsoleThemeStyle.TertiaryText] = new() {Foreground = ConsoleColor.DarkGray},
                [ConsoleThemeStyle.Invalid] = new() {Foreground = ConsoleColor.Yellow},
                [ConsoleThemeStyle.Null] = new() {Foreground = ConsoleColor.Cyan},
                [ConsoleThemeStyle.Name] = new() {Foreground = ConsoleColor.Cyan},
                [ConsoleThemeStyle.String] = new() {Foreground = ConsoleColor.Cyan},
                [ConsoleThemeStyle.Number] = new() {Foreground = ConsoleColor.Cyan},
                [ConsoleThemeStyle.Boolean] = new() {Foreground = ConsoleColor.Cyan},
                [ConsoleThemeStyle.Scalar] = new() {Foreground = ConsoleColor.Cyan},
                [ConsoleThemeStyle.LevelVerbose] = new()
                    {Foreground = ConsoleColor.Gray, Background = ConsoleColor.DarkGray},
                [ConsoleThemeStyle.LevelDebug] = new()
                    {Foreground = ConsoleColor.White, Background = ConsoleColor.DarkGray},
                [ConsoleThemeStyle.LevelInformation] = new()
                    {Foreground = ConsoleColor.White, Background = ConsoleColor.Blue},
                [ConsoleThemeStyle.LevelWarning] = new()
                    {Foreground = ConsoleColor.DarkGray, Background = ConsoleColor.Yellow},
                [ConsoleThemeStyle.LevelError] = new()
                    {Foreground = ConsoleColor.White, Background = ConsoleColor.Red},
                [ConsoleThemeStyle.LevelFatal] = new()
                    {Foreground = ConsoleColor.White, Background = ConsoleColor.Red}
            });

        private DirectoryService _dir;
        private bool _isDebug;
        private Logger _logger;

        private IServiceProvider _services;

        public static void Main()
        {
            // započinjemo u asinkronosnom kontekstu
            new Program().MainAsync().GetAwaiter().GetResult();
        }

        // "pravi" ulaz u program
        public async Task MainAsync()
        {
            // jesmo li u debug načinu?
#if DEBUG
            _isDebug = true;
#else
            _isDebug = false;
#endif

            _dir = new DirectoryService();

            var logsPath = Path.Combine(_dir.SingleFilePath, "logs");

            // kreiranje logging patha

            if (!Directory.Exists(Path.Combine(logsPath)))
                Directory.CreateDirectory(logsPath);

            // kreiranje loggera

            _logger = new LoggerConfiguration()
                .WriteTo.Console(theme: SerilogTheme)
                .WriteTo.File(Path.Combine(logsPath, $"log_{DateTime.Now:yyyyMMdd_HHmm_ss}.txt"))
                .CreateLogger();

            // konfiguriranje dependency injection (https://docs.microsoft.com/en-us/aspnet/core/fundamentals/dependency-injection?view=aspnetcore-5.0)

            _services = new ServiceCollection()
                .AddSingleton<ConfigService>()
                .AddSingleton<DirectoryService>()
                .AddSingleton<ArduinoService>()
                .AddSingleton<OpenCVService>()
                .AddSingleton(_dir)
                .AddSingleton(_logger)
                .BuildServiceProvider();

            var configService = _services.GetService<ConfigService>();

            _logger.Information("");
            _logger.Information("Program pokrenut u {Mode} načinu.", _isDebug ? "DEBUG" : "RELEASE");
            _logger.Information("Path za konfiguraciju: {ConfigDir}", configService._configDir);
            _logger.Information("Path za logging: {LogsPath}", logsPath);
            _logger.Information("");


            // čitamo konfiguraciju u pretvaramo ju u objekt u memoriji
            var config = await configService.LoadAsync();

            // logging konfiguracije (metoda za logging je locirana u Extensions.cs)
            config.Log(_logger);


            var arduinoService = _services.GetService<ArduinoService>();
            if (await arduinoService.InitializeAsync())
            {
                arduinoService.Run();

                _logger.Information("Šaljem HELLO.");
                arduinoService.Write(new HelloMessage());

                var openCV = _services.GetService<OpenCVService>();
                await openCV.StartAsync();
            }

            await Task.Delay(-1);
        }
    }
}