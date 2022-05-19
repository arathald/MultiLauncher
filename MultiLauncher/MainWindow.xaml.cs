using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using System.Windows;
using System.Windows.Controls;
using Serilog;

namespace MultiLauncher {
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow {
        private readonly Dictionary<string, TargetApplication> _applications = new ();
        private readonly string? AppVersion = ((App) Application.Current).AppVersion;
        
        public MainWindow() {
            CheckForUpdate();
            
            try {
                foreach (var config in ReadConfig()) {
                    var application = new TargetApplication(config);
                    _applications.Add(config.path, application);
                }

                Initialize();
            } catch (Exception e) {
                Log.Error(e, "Uncaught exception");
            }
        }

        private MainWindow(List<TargetApplication> applications) {
            try {
                foreach (var application in applications) {
                    _applications.Add(application.Config.path, application);
                    application.ReinitializeDisplayElements();
                }
                
                Initialize();
            } catch (Exception e) {
                Log.Error(e, "Uncaught exception");
            }
        }

        private void CheckForUpdate() {
            var updater = new Updater(AppVersion);
            var status = updater.CheckLastUpdateStatus(out var message);
            var checkForUpdate = true;
            
            if (status == Updater.UpdateResult.Succeeded) {
                MessageBox.Show(message, $"Updated to {AppVersion}", MessageBoxButton.OK);
                checkForUpdate = false;
            } else if (status == Updater.UpdateResult.Failed || status == Updater.UpdateResult.Unknown) {
                var icon = status == Updater.UpdateResult.Failed ? MessageBoxImage.Exclamation : MessageBoxImage.Question;
                var caption = status == Updater.UpdateResult.Failed ? "Update Failed" : "There may have been an issue with an update";
                var answer = MessageBox.Show(message, caption, MessageBoxButton.YesNo, icon);
                if (answer == MessageBoxResult.No) checkForUpdate = false;
            }

            if (checkForUpdate && updater.HasUpdate()) {
                var dialogResult = MessageBox.Show(
                    $"There is an update available to version {updater.NewVersion} (current version: {AppVersion}). Would you like to install it now? MultiLauncher will automatically restart.",
                    "Update available", MessageBoxButton.YesNo);
                if (dialogResult == MessageBoxResult.Yes) {
                    updater.PerformUpdate();
                }
            }
        }

        private void Initialize() {
            InitializeComponent();
            
            if (AppVersion != null) {
                Version.Text = "Version " + AppVersion;
            }
            
            Start();
        }

        private List<TargetApplication.ApplicationConfig> ReadConfig() {
            var text = File.ReadAllText(App.ApplicationConfigPath);
            var config = JsonSerializer.Deserialize<List<TargetApplication.ApplicationConfig>>(text); 
            return config ?? new List<TargetApplication.ApplicationConfig>();
        }

        public void RefreshConfig() {
            var applications = new List<TargetApplication>();

            foreach (var config in ReadConfig()) {
                TargetApplication application;
                if (_applications.ContainsKey(config.path)) {
                    application = _applications[config.path];
                    application.Config = config;
                    _applications.Remove(config.path);
                } else {
                    application = new TargetApplication(config);
                }
                
                applications.Add(application);
            }

            foreach (var (_, application) in _applications) {
                application.Dispose();
            }

            var newMainWindow = new MainWindow(applications);
            Application.Current.MainWindow = newMainWindow;
            newMainWindow.Top = this.Top;
            newMainWindow.Left = this.Left;
            newMainWindow.Show();
            this.Close();
        }

        private void Start() {
            var marginWidth = 5;
            var elementMargin = new Thickness(marginWidth);
            var rowHeight = new GridLength(25 + 2 * marginWidth);

            foreach (var application in _applications.Values) {
                var row = new RowDefinition {
                    Height = rowHeight
                };

                ContentGrid.RowDefinitions.Add(row);
                var currentRowIdx = ContentGrid.RowDefinitions.Count - 1;
                
                var newElements = new FrameworkElement[] {
                    application.Indicator,
                    application.DisplayText,
                    application.StartButton,
                    application.StopButton
                };

                for (var i = 0; i < newElements.Length; i++) {
                    ContentGrid.Children.Add(newElements[i]);
                    Grid.SetColumn(newElements[i], i);
                    Grid.SetRow(newElements[i], currentRowIdx);
                    newElements[i].Margin = elementMargin;
                }
            }

            ResizeScrollArea();
        }

        private void Settings_OnClick(object sender, RoutedEventArgs e) {
            var settings = new Settings();
            settings.Owner = this;
            settings.Left = this.Left + this.Width / 2 - settings.Width / 2;
            settings.Top = this.Top + this.Height / 2 - settings.Height / 2;
            settings.ShowDialog();
        }

        private void MainWindow_OnSizeChanged(object sender, SizeChangedEventArgs e) {
            ResizeScrollArea();
        }

        private void ResizeScrollArea() {
            ScrollArea.Height = this.Height - this.TitleBarHeight - Footer.ActualHeight - Footer.Margin.Top - Footer.Margin.Bottom - 5;
        }
    }
}