$jsonpayload = [Console]::In.ReadLine()
$json = ConvertFrom-Json $jsonpayload
$resourceGroup = $json.resourceGroup
$functionAppName = $json.functionAppName
$functionName = $json.functionName

$output = az functionapp function keys list -g $resourceGroup -n $functionAppName --function-name $functionName
$result = $output | ConvertFrom-Json
$functionKey = $result.default
Write-Output "{""FunctionKey"" : ""$functionKey""}"