using System.Diagnostics;
using System.IO;
using System.Reflection;
using Serilog;
using Serilog.Exceptions;

namespace MultiLauncher {
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App {
        public const string ApplicationConfigPath = "applications.json";
        public readonly string? AppVersion;
        
        public App() {
            Log.Logger = new LoggerConfiguration()
                .MinimumLevel.Information()
                .Enrich.WithExceptionDetails()
                .WriteTo.File("multilauncher_log.txt")
                .CreateLogger();
            
            var filename = Process.GetCurrentProcess().MainModule?.FileName;
            if (filename != null) {
                AppVersion = FileVersionInfo.GetVersionInfo(filename).ProductVersion;
            } else {
                Log.Warning("Could not get version number from running process");
            }

            if (!File.Exists(ApplicationConfigPath)) {
                CopyDefaultConfig();
            }
        }

        private void CopyDefaultConfig() {
            var assembly = Assembly.GetExecutingAssembly();
            using (var stream = assembly.GetManifestResourceStream("MultiLauncher.applications_default.json")) {
                using (var fileStream = File.CreateText(ApplicationConfigPath)) {
                    stream.CopyTo(fileStream.BaseStream);
                    Log.Information("Copied default config to {path}", ApplicationConfigPath);
                }
            }
        }
    }
}