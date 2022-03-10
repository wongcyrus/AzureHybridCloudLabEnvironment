Write-Host "Stop IoTSshClientService Services."
sc.exe stop "IoTSshClientService"
Start-Sleep -Seconds 3
Write-Host "Delete IoTSshClientService Services."
sc.exe delete "IoTSshClientService"
