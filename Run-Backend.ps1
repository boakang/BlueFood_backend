$ErrorActionPreference = 'Stop'

Set-Location $PSScriptRoot
$env:ASPNETCORE_ENVIRONMENT = 'Development'

dotnet run --project .\BlueFood_Api\BlueFood.Api.csproj --urls http://0.0.0.0:5085