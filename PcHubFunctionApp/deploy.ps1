dotnet publish -p:PublishProfile=FolderProfile
az functionapp deployment source config-zip --resource-group AzureHybridCloudLabEnvironment --name ive-lab --src bin/Release/net6.0/deployment.zip