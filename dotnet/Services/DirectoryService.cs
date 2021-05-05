using System;
using System.Diagnostics;
using System.IO;

namespace Geste.Services
{
    public class DirectoryService
    {
        // helper klasa ako je aplikacija self-contained i single-file
        // koristimo ju da bi kreirali konfiguraciju na pravom mjestu

        public DirectoryService()
        {
            ExtractPath = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "."));
            SingleFilePath = Path.GetFullPath(Path.Combine(Process.GetCurrentProcess().MainModule.FileName, ".."));
            IsBundled = ExtractPath != SingleFilePath;
        }

        public bool IsBundled { get; }
        public string ExtractPath { get; }
        public string SingleFilePath { get; }
    }
}