using System.Diagnostics;
using System.Windows;
using MahApps.Metro.Controls;

namespace MultiLauncher; 

public partial class Settings : MetroWindow {
    public Settings() {
        InitializeComponent();
    }

    private void Close_OnClick(object sender, RoutedEventArgs e) {
        this.Close();
    }

    private void OpenApplicationsJson_OnClick(object sender, RoutedEventArgs e) {
        var startInfo = new ProcessStartInfo(App.ApplicationConfigPath);
        startInfo.UseShellExecute = true;
        Process.Start(startInfo);
    }

    private void RefreshApplicationsJson_OnClick(object sender, RoutedEventArgs e) {
        ((MainWindow)Owner).RefreshConfig();
    }
}