using System;
using System.Diagnostics;
using System.IO;
using System.Net.Http;
using System.Reflection;
using System.Text.Json;
using System.Threading;
using Serilog;

namespace MultiLauncher; 

public class Updater {
    public string? NewVersion => RemoteLatest?.ToString();
    public enum UpdateResult { None, Succeeded, Failed, Unknown };

    private const string LatestReleasePath = "https://api.github.com/repos/arathald/MultiLauncher/releases/latest";
    private const string UpdateHelperName = "update_helper.ps1";
    private Version CurrentVersion;
    private Version? RemoteLatest;

    private string? RemoteLatestUrl;
    private long RemoteLatestSize;
    private string? RemoteReleaseNotes;

    private static readonly HttpClient client = new();
    
    public Updater(string? currentVersion) {
        CurrentVersion = new Version(currentVersion);
        
        client.DefaultRequestHeaders.Add("Accept", "application/vnd.github.v3+json");
        client.DefaultRequestHeaders.Add("User-Agent", $"arathald MultiLauncher/{CurrentVersion}");
        client.Timeout = TimeSpan.FromSeconds(45);
    }

    
    public UpdateResult CheckLastUpdateStatus(out string message) {
        if (File.Exists(UpdateHelperName)) {
            File.Delete(UpdateHelperName);
            
            if (File.Exists("updated")) {
                message = $"Updated to version {CurrentVersion}\n\nRelease Notes:\n{File.ReadAllText("updated")}";
                File.Delete("updated");
                return UpdateResult.Succeeded;
            }

            if (File.Exists("updateFailed")) {
                File.Delete("updateFailed");
                message = "Update failed. See multilauncher_log.txt and update_helper_log.txt for more details.\n\n"
                          + "Would you like to try checking for an update again?";
                return UpdateResult.Failed;
            }
            
            if (File.Exists("updating")) File.Delete("updating");

            message = "It looks like MultiLauncher may have been trying to update, but something went wrong."
                      + " See multilauncher_log.txt and update_helper_log.txt for more details.\n\n"
                      + "Would you like to try checking for an update again?";
            return UpdateResult.Unknown;
        } 
            
        message = "";
        return UpdateResult.None;
    }

    public bool HasUpdate() {
        try {
            HttpResponseMessage message;

            using (var tokenSource = new CancellationTokenSource(TimeSpan.FromSeconds(5)))
            using (var requestMessage = new HttpRequestMessage(HttpMethod.Get, LatestReleasePath)) {
                message = client.Send(requestMessage, tokenSource.Token);
            }

            using (var document = JsonDocument.Parse(message.Content.ReadAsStream())) {
                var root = document.RootElement;

                RemoteLatest = new Version(root.GetProperty("name").GetString());

                var asset = root.GetProperty("assets")[0];
                var name = asset.GetProperty("name").GetString();
                if (name != "MultiLauncher.exe") {
                    Log.Error($"Unexpected asset found: {name}");
                    return false;
                }

                RemoteLatestUrl = asset.GetProperty("browser_download_url").GetString();
                RemoteLatestSize = asset.GetProperty("size").GetInt64();
                RemoteReleaseNotes = root.GetProperty("body").GetString();
            }

            return RemoteLatest > CurrentVersion;
        }
        catch (Exception e) {
            Log.Warning(e, "Could not get latest version information");
            return false;
        }
    }

    public void PerformUpdate() {
        if (RemoteLatest == null) {
            if (!HasUpdate()) return;
        }
        
        Log.Information($"Updating from version {CurrentVersion} to {RemoteLatest}");
        var task = client.GetByteArrayAsync(RemoteLatestUrl);
        task.Wait();
        
        if (task.IsCompletedSuccessfully) {
            var payload = task.Result;
            var fileSize = payload.LongLength;
            if (payload.LongLength != RemoteLatestSize) {
                Log.Error($"Payload downloaded from {RemoteLatestUrl} was {fileSize} bytes, but expected {RemoteLatestSize} bytes");
                return;
            }

            File.WriteAllBytes("MultiLauncher.exe.new", payload);
            File.WriteAllText("updating", RemoteReleaseNotes);
            
            LaunchUpdateHelper();
        } else if (task.IsFaulted) {
            Log.Error(task.Exception, $"Exception encountered while downloading {RemoteLatestUrl}");
        } else if (task.IsCanceled) {
            Log.Error($"Autoupdate timed out on downloading {RemoteLatestUrl}");
        }
    }

    private void LaunchUpdateHelper() {
        var assembly = Assembly.GetExecutingAssembly();
        using (var stream = assembly.GetManifestResourceStream("MultiLauncher.update_helper.ps1")) {
            using (var fileStream = File.CreateText(UpdateHelperName)) {
                stream.CopyTo(fileStream.BaseStream);
            }
        }

        var processStartInfo = new ProcessStartInfo("powershell.exe", $"-ExecutionPolicy ByPass -File {UpdateHelperName} -MultiLauncherPid {Process.GetCurrentProcess().Id}");
        processStartInfo.UseShellExecute = true;
        Process.Start(processStartInfo);
    }

    private class Version : IComparable<Version> {
        private readonly int _major;
        private readonly int _minor;
        private readonly int _rev;

        public Version(string? versionString) {
            var versionParts = versionString?.Trim('v').Split('.');
            if (versionParts?.Length != 3
                    || !int.TryParse(versionParts[0], out _major)
                    || !int.TryParse(versionParts[1], out _minor)
                    || !int.TryParse(versionParts[2], out _rev)) {
                Log.Error($"Version number {versionString} is not valid");
            }
        }

        public override string ToString() {
            return $"{_major}.{_minor}.{_rev}";
        }

        public int CompareTo(Version? other) {
            if (other == null) return 1;
            if (_major != other._major) return _major.CompareTo(other._major);
            if (_minor != other._minor) return _minor.CompareTo(other._minor);
            return _rev.CompareTo(other._rev);
        }

        public static bool operator <(Version left, Version right) => left.CompareTo(right) < 0;
        public static bool operator >(Version left, Version right) => left.CompareTo(right) > 0;
        public static bool operator <=(Version left, Version right) => left.CompareTo(right) <= 0;
        public static bool operator >=(Version left, Version right) => left.CompareTo(right) >= 0;
}
}