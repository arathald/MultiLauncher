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
        public MainWindow() {
            InitializeComponent();
            ProcessHelpers.getCurrentProcessData();
            try {
                Start();
            } catch (Exception e) {
                Log.Error(e, "Uncaught exception");
            }
        }

        private void Start() {
            var text = File.ReadAllText(App.ApplicationConfigPath);
            var applicationConfigs = JsonSerializer.Deserialize<List<TargetApplication.ApplicationConfig>>(text);

            var marginWidth = 5;
            var elementMargin = new Thickness(marginWidth);
            var rowHeight = new GridLength(25 + 2 * marginWidth);

            foreach (var config in applicationConfigs) {
                var application = new TargetApplication(config);

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
        }
        
        
    }
}