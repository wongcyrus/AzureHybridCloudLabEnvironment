dotnet publish -p:PublishProfile=FolderProfile
$currentDir = Get-Location
$scriptblock = "Compress-Archive -path $currentDir\bin\Release\net6.0\publish\* -DestinationPath $currentDir\bin\Release\net6.0\deployment.zip -CompressionLevel Optimal -Force"
Write-Host "Create Zip"
function ZipFiles( $zipfilename, $sourcedir )
{
   Add-Type -Assembly System.IO.Compression.FileSystem
   $compressionLevel = [System.IO.Compression.CompressionLevel]::Optimal
   [System.IO.Compression.ZipFile]::CreateFromDirectory($sourcedir,
        $zipfilename, $compressionLevel, $false)
    Write-Host "Zipped"
}
rm "$currentDir\bin\Release\net6.0\deployment.zip"
ZipFiles "$currentDir\bin\Release\net6.0\deployment.zip" "$currentDir\bin\Release\net6.0\publish\"
Write-Host "Deploy Zip to Azure"
az functionapp deployment source config-zip --resource-group AzureHybridCloudLabEnvironment --name ive-lab --src bin/Release/net6.0/deployment.zip