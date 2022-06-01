$jsonpayload = [Console]::In.ReadLine()
$json = ConvertFrom-Json $jsonpayload
$resourceGroup = $json.resourceGroup
$functionAppName = $json.functionAppName
$functionName = $json.functionName

$cmdOutput = &"az functionapp function keys list -g $resourceGroup -n $functionAppName --function-name $functionName" 2>&1
Write-Output $cmdOutput