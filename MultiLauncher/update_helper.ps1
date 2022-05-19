param($MultiLauncherPid)
Start-Transcript -UseMinimalHeader -Path ./update_helper_log.txt 

try {
    "Stopping MultiLauncher" 
    Stop-Process -Id $MultiLauncherPid
    Wait-Process -Id $MultiLauncherPid
    Write-Output "MultiLauncher stopped"
    
    Write-Output "Backing up current version"
    mv -Force MultiLauncher.exe MultiLauncher.exe.old
    
    Write-Output "Installing new version"
    mv MultiLauncher.exe.new MultiLauncher.exe
    
    Write-Output "Installing new version"
} finally {
    If (Test-Path -Path ./MultiLauncher.exe -PathType Leaf) {
        Write-Output "Update succeeded!"
        del Multilauncher.exe.old
        mv -Force updating updated
    } Else
    {
        Write-Output "Failed to update! Rolling back"
        mv Multilauncher.exe.old Multilauncher.exe
        mv updating updateFailed
    }

    Write-Output "Relaunching MultiLauncher"
    ./MultiLauncher.exe
}

Stop-Transcript

