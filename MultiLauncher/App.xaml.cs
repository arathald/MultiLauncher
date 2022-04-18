using System.IO;
using System.Reflection;
using Serilog;
using Serilog.Exceptions;

namespace MultiLauncher {
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App {
        public static readonly string ApplicationConfigPath = "applications.json";
        
        public App() {
            Log.Logger = new LoggerConfiguration()
                .MinimumLevel.Information()
                .Enrich.WithExceptionDetails()
                .WriteTo.File("multilauncher_log.txt")
                .CreateLogger();
        
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