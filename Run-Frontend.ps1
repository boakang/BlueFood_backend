$ErrorActionPreference = 'Stop'

$frontendPath = Join-Path $PSScriptRoot '..\BlueFood_frontend'
Set-Location $frontendPath

npm run dev -- --host 0.0.0.0 --port 5173