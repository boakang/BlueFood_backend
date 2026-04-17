$ErrorActionPreference = 'Stop'

$processes = Get-CimInstance Win32_Process | Where-Object {
    $_.CommandLine -match 'BlueFood_Api' -or
    $_.CommandLine -match 'BlueFood_frontend\\node_modules' -or
    $_.CommandLine -match 'vite\\bin\\vite\.js' -or
    $_.CommandLine -match 'npm-cli\.js" run dev -- --host 0\.0\.0\.0 --port 5173'
}

foreach ($process in $processes) {
    Stop-Process -Id $process.ProcessId -Force -ErrorAction SilentlyContinue
}

Write-Host "Stopped $($processes.Count) BlueFood process(es)."