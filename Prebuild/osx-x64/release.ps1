cd ..\..\IoTSshClientService\
dotnet publish -p:PublishProfile=FolderProfileMacOSx64
cd ..\Prebuild\osx-x64
Copy ..\..\IoTSshClientService\bin\Debug\net6.0\osx-x64\publish\osx-x64\* .
del appsettings.json
$file=Get-ChildItem -Path . -Exclude "release.ps1","IoTSshClientService.zip","hash.txt"
#del IoTSshClientService.zip
$currentDir = Get-Location
Compress-Archive -path $file -DestinationPath $currentDir\IoTSshClientService.zip -CompressionLevel Optimal -Force