using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Management;
using Serilog;

namespace MultiLauncher; 

public static class ProcessHelpers {
    public static Dictionary<int, Process?> GetChildProcesses(int processId) {
        var children = new Dictionary<int, Process?>();
        var objectSearcher = new ManagementObjectSearcher($"Select * From Win32_Process Where ParentProcessID={processId}");

        foreach (var managementObject in objectSearcher.Get()) {
            var childProcessId = Convert.ToInt32(managementObject["ProcessID"]);
            try {
                children.Add(childProcessId, Process.GetProcessById(childProcessId));
            } catch (ArgumentException) {
                Log.Warning("Got exited process {child} as child of {parent}. Returning processId with no Process handle.", 
                    managementObject["ProcessID"], processId);
                children.Add(childProcessId, null);
            }
        }

        return children;
    }

    public static void getCurrentProcessData() {
        var processId = Process.GetCurrentProcess().Id;
        Dictionary<string, object> properties = new();
        Dictionary<string, object> systemProperties = new();
        var objectSearcher2 = new ManagementObjectSearcher($"Select * From Win32_Process Where ProcessID={processId}");
        foreach (var managementBaseObject in objectSearcher2.Get()) {
            
            foreach (var propertyData in managementBaseObject.Properties) {
                properties.Add(propertyData.Name, propertyData.Value);
            }
            
            foreach (var systemProperty in managementBaseObject.SystemProperties) {
                systemProperties.Add(systemProperty.Name, systemProperty.Value);
            }
        }

        var a = 2;
    }
    
    public static bool IsAssociated(Process process) {
        // For some reason, can't access process.Associated here, so instead catch and check the error :(
        try {
            var id = process.Id;
        } catch (InvalidOperationException e) {
            if (e.Message == "No process is associated with this object.") {
                return false;
            }
            
            throw;
        }

        return true;
    }
}