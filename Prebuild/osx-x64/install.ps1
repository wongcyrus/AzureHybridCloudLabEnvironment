$servicesExeFilePath = Join-Path $PSScriptRoot "IoTSshClientService.exe"
Write-Host "Install auto-start IoTSshClientService"
sc.exe create "IoTSshClientService" start=auto binpath="$servicesExeFilePath"
Start-Sleep -Seconds 3
Write-Host "Change IoTSshClientService failure to keep retrying."
sc.exe failure "IoTSshClientService" reset=0 actions=restart/60000/restart/60000/restart/60000
Write-Host "Start IoTSshClientService Services."
sc.exe start "IoTSshClientService"
Start-Sleep -Seconds 3
Write-Host "Check IoTSshClientService Services Status."
sc.exe query "IoTSshClientService"
sc.exe qfailure "IoTSshClientService"
