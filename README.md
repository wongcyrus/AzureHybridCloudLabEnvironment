# Azure Hybrid Cloud Lab Environment

[Please read this Microsoft Tech Blog for more details](https://techcommunity.microsoft.com/t5/educator-developer-blog/azure-hybrid-cloud-lab-environment/ba-p/3251405)

## How to deploy PcReservationFunctionApp
1. Install Terramform and Azure Cli in Windows
2. Run ```terraform.exe apply -auto-approve```. If there is error related to function keys, delete func/GetDeviceConnectionString.json and func/AddSshConnectionFunction.json re-run the command as it has the timing issue.
3. Run ```terraform.exe refresh```, and it will populate the AzureFunctionBaseUrl, LifeCycleHookUrl and GetDeviceConnectionStringKey.

## How to build IoTSshClientService?
1. Update appsettings.json by replacing <AzureFunctionBaseUrl> and <GetDeviceConnectionStringKey>.
2. Open powershell in AzureHybridCloudLabEnvironment\IoTSshClientService.
3. Run ```dotnet publish -p:PublishProfile=FolderProfileWinx64``` for Windows or ```dotnet publish -p:PublishProfile=FolderProfileMacOSx64``` for MacOS
4. Go to AzureHybridCloudLabEnvironment\IoTSshClientService\bin\Debug\net6.0\win-x64\publish\win-x64\ or \AzureHybridCloudLabEnvironment\IoTSshClientService\bin\Debug\net6.0\osx-x64\publish\osx-x64
5. Copy all files to lab PC that students need to remote.

## How to deploy IoTSshClientService in Windows?
1. Open Powershell as Administrator
2. Run ```Set-ExecutionPolicy -Scope Process -ExecutionPolicy Bypass```
3. Copy the deployment package into a folder.
4. To deploy the windows services  ```deploy.ps1```, and undeploy the windows services  ```deploy.ps1```.


## How to deploy IoTSshClientService ansible?
check for connectivity ```ansible windows -m win_ping```
Run ```ansible-playbook InstallIoTSshClientService.yaml```

## How to Connect from MacOS?
1. Open terminal and create SSH tunnel. ```ssh bastion@<IP> -p22 -L 3389:0.0.0.0:3389```
2. Enter the SSH server password.
3. Open Remote Desktop client and connect to localhost.
4. Enter the Windows username and passowrd.


## PC Revervation 
You can set SshConnction.Variables
With MachineName
{"Email":"cywong@vtc.edu.hk","MachineName":"L332-A0","SeatNumber":"0"}
With just seat number, and it pick from the list computer in lab
{"Email":"cywong@vtc.edu.hk","SeatNumber":"0"}
