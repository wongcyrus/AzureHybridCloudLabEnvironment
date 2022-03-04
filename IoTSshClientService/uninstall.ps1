Write-Host "Stop PollingLoginSshWorkerService Services."
sc.exe stop "PollingLoginSshWorkerService"
Start-Sleep -Seconds 3
Write-Host "Delete PollingLoginSshWorkerService Services."
sc.exe delete "PollingLoginSshWorkerService"
