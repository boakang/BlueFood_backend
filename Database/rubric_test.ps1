$ErrorActionPreference = 'Stop'
$timestamp = Get-Date -Format 'yyyyMMdd-HHmmss'
$batchCode = "BF-DEMO-$timestamp"
$actor = 'BAKHANG\Administrator'

$createBody = @{ batchCode=$batchCode; productName='Xoai Cat Chu'; farmPartnerId=1; productionDate='2026-04-07'; expiryDate='2026-04-21'; actor=$actor; traceBaseUrl='https://bluefood.local/trace/' } | ConvertTo-Json
$createResp = Invoke-RestMethod -Method Post -Uri 'http://localhost:5085/api/batches' -ContentType 'application/json' -Body $createBody

$eventBody = @{ eventType='SHIPPED'; fromPartnerId=1; toPartnerId=2; locationText='Dong Thap'; noteText='Demo rubric shipment'; actor=$actor } | ConvertTo-Json
Invoke-RestMethod -Method Post -Uri ("http://localhost:5085/api/batches/$batchCode/events") -ContentType 'application/json' -Body $eventBody | Out-Null

$sw = [System.Diagnostics.Stopwatch]::StartNew()
$traceByBatch = Invoke-RestMethod -Method Get -Uri ("http://localhost:5085/api/batches/$batchCode/trace")
$sw.Stop()
$traceMs = $sw.ElapsedMilliseconds

$traceByQr = Invoke-RestMethod -Method Get -Uri ("http://localhost:5085/api/trace/$($createResp.qrToken)")

$certCode = "CERT-DEMO-$timestamp"
$certBody = @{ certificateCode=$certCode; certificateName='VietGAP'; issuedBy='Bo NNPTNT'; issuedDate='2026-03-10'; expiredDate='2027-03-10'; fileUrl='https://files.local/cert/vietgap.pdf'; actor=$actor } | ConvertTo-Json
$certResp = Invoke-RestMethod -Method Post -Uri 'http://localhost:5085/api/certificates' -ContentType 'application/json' -Body $certBody

$attachBody = @{ certificateId=$certResp.certificateId; actor=$actor } | ConvertTo-Json
Invoke-RestMethod -Method Post -Uri ("http://localhost:5085/api/batches/$batchCode/certificates") -ContentType 'application/json' -Body $attachBody | Out-Null

$batchCerts = Invoke-RestMethod -Method Get -Uri ("http://localhost:5085/api/batches/$batchCode/certificates")
$auditRows = Invoke-RestMethod -Method Get -Uri ("http://localhost:5085/api/batches/$batchCode/audit")

$immutabilityCheck = ''
$sqlcmdPath = 'C:\Program Files\Microsoft SQL Server\Client SDK\ODBC\180\Tools\Binn\sqlcmd.exe'
if (Test-Path $sqlcmdPath) {
  $sql = "USE BlueFoodSCM; UPDATE TOP(1) scm.BatchEvents SET NoteText='HACK' WHERE BatchId = '$($createResp.batchId)';"
  $tmp = New-TemporaryFile
  Set-Content -Path $tmp -Value $sql -Encoding ascii
  try {
    $out = & $sqlcmdPath -S "BAKHANG\SQLEXPRESS" -E -b -C -i $tmp 2>&1
    if ($LASTEXITCODE -ne 0) {
      $immutabilityCheck = ($out | Out-String).Trim()
    } else {
      $immutabilityCheck = 'Unexpected: update succeeded'
    }
  } catch {
    $immutabilityCheck = $_.Exception.Message
  } finally {
    Remove-Item $tmp -Force
  }
} else {
  $immutabilityCheck = 'sqlcmd not found on machine'
}

$result = [pscustomobject]@{
  testedAt = (Get-Date).ToString('yyyy-MM-dd HH:mm:ss')
  batchCode = $batchCode
  batchId = $createResp.batchId
  qrToken = $createResp.qrToken
  traceUrl = $createResp.traceUrl
  traceEventCountByBatch = $traceByBatch.Count
  traceEventCountByQr = $traceByQr.Count
  traceResponseMs = $traceMs
  traceUnder2s = ($traceMs -lt 2000)
  certificateId = $certResp.certificateId
  certificateCountOnBatch = $batchCerts.Count
  auditRowCount = $auditRows.Count
  latestAuditAction = if ($auditRows.Count -gt 0) { $auditRows[0].actionType } else { $null }
  immutableUpdateAttempt = $immutabilityCheck
}

$result | ConvertTo-Json -Depth 6
