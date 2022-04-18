using System.Windows;
using Serilog;
using Serilog.Exceptions;

namespace MultiLauncher {
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : Application {
        public App() { 
            Log.Logger = new LoggerConfiguration()
                .MinimumLevel.Information()
                .Enrich.WithExceptionDetails()
                .WriteTo.File("multilauncher_log.txt")
                .CreateLogger();
        }
        
    }
}