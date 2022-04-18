using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Shapes;
using System.Windows.Threading;
using Serilog;

namespace MultiLauncher; 
public class TargetApplication {
    public Ellipse Indicator { get; }
    public TextBlock DisplayText { get; }
    public Button StartButton { get; }
    public Button StopButton { get; }
    
    public Dispatcher Dispatcher { get; }

    private ApplicationConfig _config;
    
    private State _state = new();
    private Dictionary<int, Process> _activeProcesses = new();
    private HashSet<int> _processesCheckedThisCycle = new();

    public TargetApplication(ApplicationConfig config) {
        Dispatcher = Dispatcher.CurrentDispatcher;
        
        _config = config;
        
        Indicator = new Ellipse {
            Fill = Brushes.Gray,
            Stroke = Brushes.White,
            Width = 18,
            Height = 18,
            VerticalAlignment = VerticalAlignment.Center
        };

        DisplayText = new TextBlock {
            FontSize = 16,
            Text = config.displayName,
            //Foreground = Brushes.Beige
        };
            
        StartButton = new Button {
            Content = "Start",
            //Foreground = Brushes.Beige
        };
        StartButton.Click += StartProcess;
            
        StopButton = new Button {
            Content = "Stop",
            //Foreground = Brushes.Beige,
            IsEnabled = false
        };
        StopButton.Click += StopAllProcesses;

        if (_config.autoStart) {
            StartProcess();
        }
    }

    private void StartProcess(object? sender = null, RoutedEventArgs? e = null) {
        try {
            lock (_state) {
                _state.UserTriggeredStop = false;
                _state.EncounteredError = false;
            }

            Dispatcher.Invoke(() => StartButton.IsEnabled = false);

            StartRootProcess();

            Dispatcher.Invoke(() => {
                Indicator.Fill = Brushes.Green;
                StopButton.IsEnabled = true;
            });

            MonitorProcesses();
        }
        catch (Exception ex) {
            Log.Error(ex, "Exception encountered while running {appName}", _config.displayName);
        }
    }

    private void StartRootProcess() {
        if (_activeProcesses.Count != 0) {
            throw new InvalidOperationException("Cannot start a new process while there are active processes still running");
        }
        
        ProcessStartInfo processStartInfo = new ProcessStartInfo {
            FileName = _config.path,
            WorkingDirectory = _config.startIn,
            Arguments = _config.arguments,
            UseShellExecute = true,
            Verb = _config.requiresAdmin ? "runas" : null
        };
        var rootProcess = Process.Start(processStartInfo);
        if (rootProcess == null) {
            lock (_state) _state.EncounteredError = true;
            OnAllProcessesExited();
            throw new Exception("Root process could not be started");
        }

        lock (((ICollection)_activeProcesses).SyncRoot) {
            _activeProcesses.Add(rootProcess.Id, rootProcess);
        }

        Log.Information("Started Process id {pid} for application {displayName}", rootProcess.Id,  _config.displayName);
    }

    private void MonitorProcesses() {
        Task.Run(() => {
            while (_activeProcesses.Count > 0) {
                Thread.Sleep(1000);
                _processesCheckedThisCycle.Clear();
                var activeProcessSnapshot = new int[_activeProcesses.Count];

                lock (((ICollection) _activeProcesses).SyncRoot) {
                    _activeProcesses.Keys.CopyTo(activeProcessSnapshot, 0);
                }

                foreach (var processId in activeProcessSnapshot) {
                    MonitorProcessRec(processId);
                }
                
                Log.Information("Processes checked this cycle: {count}",  _processesCheckedThisCycle.Count);
                Log.Information("Current live processes: {count}", _activeProcesses.Count);
            }

            OnAllProcessesExited();
        }).ContinueWith(t => {
            if (t.Exception != null) {
                Exception e = t.Exception;
                while (e is AggregateException && e.InnerException is not null) {
                    e = e.InnerException;
                }
                
                Log.Error(e, "Exception encountered in process monitor loop for {appName}", _config.displayName);
            }
        }, TaskContinuationOptions.OnlyOnFaulted);
    }

    private void MonitorProcessRec(int processId) {
        Log.Information("Checking process {pid}", processId);
        if (_processesCheckedThisCycle.Contains(processId)) {
            Log.Warning("Already checked this process this loop");
        }
        _processesCheckedThisCycle.Add(processId);

        var childProcesses = ProcessHelpers.GetChildProcesses(processId);
        foreach (var (childProcessId, childProcess) in childProcesses) {
            var newProcess = false;
            lock (((ICollection) _activeProcesses).SyncRoot) {
                if (!_activeProcesses.ContainsKey(childProcessId)) {
                    newProcess = true;
                    if (childProcess != null) _activeProcesses.Add(childProcessId, childProcess);
                }
            }

            if (newProcess) {
                var processActive = childProcess != null && ProcessHelpers.IsAssociated(childProcess) && !childProcess.HasExited; 
                var processName =  processActive ? childProcess.ProcessName : "[Exited Process]";
                Log.Information("Found new process {pid}: {name}", childProcessId, processName);
                MonitorProcessRec(childProcessId);
            }
        }

        EvaluateProcessForCleanup(processId);
    }

    private void EvaluateProcessForCleanup(int processId) {
        Process process;
        lock (((ICollection) _activeProcesses).SyncRoot) {
            if (_activeProcesses.ContainsKey(processId)) {
                process = _activeProcesses[processId];
            } else {
                // This process got cleaned up after snapshotting, nothing more to do
                return;
            }
        }
        if (_state.UserTriggeredStop) {
            Log.Information("Stopping process {pid}", processId);
            Task.Run(() => StopProcess(process, processId));
        } else if (!ProcessHelpers.IsAssociated(process) || process.HasExited) {
            Log.Information("Process {pid} has exited, cleaning up", processId);
            CleanUpExitedProcess(process, processId);
        }
    }

    private void OnAllProcessesExited() {
        Dispatcher.Invoke(() => StartButton.IsEnabled = true);
        Dispatcher.Invoke(() => StopButton.IsEnabled = false);
        if (_state.EncounteredError) {
            Indicator.Dispatcher.Invoke(() => Indicator.Fill = Brushes.Red);
        } else {
            Indicator.Dispatcher.Invoke(() => Indicator.Fill = Brushes.Gray);
        }
        
        if (!_state.UserTriggeredStop && _config.keepAlive) {
            Log.Warning("Application {appName} quit unexpectedly, restarting", _config.displayName);
            StartProcess();
        }
    }

    private void CleanUpExitedProcess(Process process, int processId) {
        process.Dispose();
        lock (((ICollection) _activeProcesses).SyncRoot) {
            _activeProcesses.Remove(processId);
        }
    }
    
    private void StopAllProcesses(object? sender = null, RoutedEventArgs? e = null) {
        lock (_state) _state.UserTriggeredStop = true;

        StopButton.IsEnabled = false;
        Indicator.Fill = Brushes.Yellow;
    }

    private void StopProcess(Process process, int processId) {
        if (process.CloseMainWindow() && !process.WaitForExit(10_000)) {
            lock (_state) _state.EncounteredError = true;
            KillProcess(process);
        }
        
        CleanUpExitedProcess(process, processId);
    }

    private void KillProcess(Process process) {
        process.Kill(true);
        process.WaitForExit();
    }

    public class ApplicationConfig {
        public string displayName { get; set; }
        public string path { get; set; }
        public string arguments { get; set; }
        public string startIn { get; set; }
        public bool requiresAdmin { get; set; }
        public bool keepAlive { get; set; }
        public bool autoStart { get; set; }
    }

    private class State {
        public bool UserTriggeredStop;
        public bool EncounteredError;
    }
}