# ActiveTunnelSshServices

## How to deploy PcReservationFunctionApp
1. Install Terramform and Azure Cli in Windows
2. Run ```terraform.exe apply -auto-approve```. If there is error related to function keys, delete func/GetDeviceConnectionString.json and func/AddSshConnectionFunction.json re-run the command as it has the timing issue.
3. Run ```terraform.exe refresh```, and it will populate the AzureFunctionBaseUrl, LifeCycleHookUrl and GetDeviceConnectionStringKey.

## How to build PollingLoginSshWorkerService
1. Update appsettings.json by replacing <AzureFunctionBaseUrl> and <GetDeviceConnectionStringKey>.
2. Open powershell in PollingLoginSshServices\PollingLoginSshWorkerService.
3. Run ```dotnet publish -p:PublishProfile=FolderProfile``` 
4. Go to PollingLoginSshServices\PollingLoginSshWorkerService\bin\Debug\net6.0\win-x64\publish\win-x64\
5. Copy all files to lab PC that students need to remote.

## How to deploy PollingLoginSshWorkerService
1. Open Powershell as Administrator
2. Run ```Set-ExecutionPolicy -Scope Process -ExecutionPolicy Bypass```
3. Copy the deployment package into a folder.
4. To deploy the windows services  ```deploy.ps1```, and undeploy the windows services  ```deploy.ps1```.


## How to deploy PollingLoginSshWorkerService ansible
check for connectivity ```ansible windows -m win_ping```
Run ```ansible-playbook InstallPollingLoginSshWorkerService.yaml```