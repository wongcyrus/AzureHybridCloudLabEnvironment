$servicesExeFilePath = Join-Path $PSScriptRoot "PollingLoginSshWorkerService.exe"
Write-Host "Install auto-start PollingLoginSshWorkerService"
sc.exe create "PollingLoginSshWorkerService" start=auto binpath="$servicesExeFilePath"
Start-Sleep -Seconds 3
Write-Host "Change PollingLoginSshWorkerService failure to keep retrying."
sc.exe failure "PollingLoginSshWorkerService" reset=0 actions=restart/60000/restart/60000/restart/60000
Write-Host "Start PollingLoginSshWorkerService Services."
sc.exe start "PollingLoginSshWorkerService"
Start-Sleep -Seconds 3
Write-Host "Check PollingLoginSshWorkerService Services Status."
sc.exe query "PollingLoginSshWorkerService"
sc.exe qfailure "PollingLoginSshWorkerService"
