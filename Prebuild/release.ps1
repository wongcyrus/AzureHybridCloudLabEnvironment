cd ..\PollingLoginSshWorkerService\
dotnet publish -p:PublishProfile=FolderProfile
cd ..\Prebuild
Copy ..\PollingLoginSshWorkerService\bin\Debug\net6.0\win-x64\publish\win-x64\* .
del appsettings.json
$file=Get-ChildItem -Path . -Exclude "release.ps1"
# del PollingLoginSshWorkerService.zip
$currentDir = Get-Location
# Compress-Archive -path $file -DestinationPath $currentDir\PollingLoginSshWorkerService.zip -CompressionLevel Optimal -Force
Start-job -scriptblock {Compress-Archive -path $file -DestinationPath $currentDir\PollingLoginSshWorkerService.zip -CompressionLevel Optimal -Force} -name "Zippityzip"
Wait-job -name "Zippityzip"
(Get-FileHash PollingLoginSshWorkerService.zip -Algorithm MD5).Hash > hash.txt