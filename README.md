# <img src="https://github.com/arathald/MultiLauncher/blob/main/MultiLaunch%20Icon.png" width=50 alt="MultiLauncher Local"/> MultiLauncher


MultiLauncher is a utility application to start and monitor various tools and plugins from a single window. I built it for use in MS Flight Simulator 2020, but only the examples are specific to that game, it should work for many activities and workflows, particularly those with long-running processes that can be unstable. See the **Configuration** section below for a complete list of current features.
<br/>
<br/>
<img src="https://user-images.githubusercontent.com/6439881/164614336-214160ec-da67-49e5-9f06-d0ee303359d1.png" alt="MultiLauncher Screenshot"/>

## Installation
To install, download the appropriate executable from the releases and copy it to the location of your choosing. MultiLauncher requires .NET 6.0, which you may have to install yourself if the program exits immediately and you're not prompted to on first run.
### Upgrading
To Upgrade, simply copy the old executable over the new one. Make sure you save your configuration file (`application.json`).
## Use
### Configuration
When you first launch the application, it will create a default `applications.json` file in the same directory as the executable. You can also see the example called `applications_default.json` in the repository and create your own. Each entry has the following fields:
* **displayName**: The name shown in the UI and in the logs
* **path**: The full path to the executable or its launcher. *Note that for targets that chain two or more launchers together (like Air Manager), you should use the launcher before a long-running process for MultiLauncher to track it properly (see Known Issues below).*
* **arguments**: Any arguments you want passed in to the target in `path`
* **startIn**: The folder to start the target in. Usually the same folder the file in `path` is in.
* **requiresAdmin**: If set to true, MultiLauncher will begin this program as admin. If you launch MultiLauncher as a normal user (recommended), this will cause a UAT prompt each time it starts the target application.
* **keepAlive**: *!experimental!* If set to true and the target application exits in any way other than closing it through MultiLauncher, MultiLauncher will automatically start it back up again. Note that this includes if you close the target application manually. *Please be careful with this feature as MultiLauncher cannot properly detect all processes in some applications which can lead it to continue spawning new copies of them. See Known Issues below.*
* **autoStart**: Start the target application immediately when MultiLauncher starts.
### Interface
Click the Start button to start an application, and the Close button to close it. There's an Indicator to the left of each application name with the following meanings:
* **Green**: The application is running, and MultiLauncher is monitoring it.
* **Yellow**: MultiLauncher is closing the application due to clicking the Close button
* **Grey**: The application is not running
* **Red**: MultiLauncher experienced an error starting or closing the application. Look for errors in `multilauncher_log.txt` for more details.
## Known Issues
Because MultiLauncher uses polling to keep track of a process's tree, any application that starts through a chain of two or more launchers which exit before the polling period will not be able to be tracked, and MultiLauncher will believe the application has closed. This will prevent you from closing the app through the launcher, but more importantly, it will cause any such applications with `keepAlive` configured to true to spawn endlessly.

Air Manager is one such application: its default Launcher, `Bootloader.exe`, runs `air_manager.bat`, which then Launches the Air Manager executable. Pointing MultiLauncher to `air_manager.bat` works fine since that launcher is the immediate parent of another which stays alive long enough to be caught by the polling mechanism.
