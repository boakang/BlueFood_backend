# Rubric Demo Evidence

## Test Scope

- Batch creation
- Event update
- Trace by batch code
- Trace by QR token
- Certificate create + attach
- Audit log verification
- Immutable history verification

## Execution

- Script: Database/rubric_test.ps1
- Command: powershell -ExecutionPolicy Bypass -File Database/rubric_test.ps1
- Tested at: 2026-04-07 10:41:20

## Result Snapshot

- BatchCode: BF-DEMO-20260407-104120
- BatchId: b1c9fa0e-98cf-4046-96ab-cdfad043f77c
- QRToken: 4629d7dc2acd4600a68fd95d3306d50d
- TraceUrl: https://bluefood.local/trace/4629d7dc2acd4600a68fd95d3306d50d
- Trace events by batch: 2
- Trace events by QR: 2
- Trace response time: 3 ms
- Trace under 2 seconds: true
- CertificateId: 7
- Certificate count on batch: 1
- Audit rows for batch: 3
- Latest audit action: ATTACH_CERT

## Immutable History Check

Direct update attempt on scm.BatchEvents was blocked by trigger:

- Msg 51001
- BatchEvents history is immutable. UPDATE/DELETE is not allowed.

This confirms append-only behavior for supply chain event history.

## Demo Screenshot Checklist

Capture the following screens for submission:

1. Frontend at localhost:5173 showing successful create batch and loaded trace timeline.
2. Frontend showing certificate created and attached to the same batch.
3. Swagger call results for:
   - POST /api/batches
   - POST /api/batches/{batchCode}/events
   - GET /api/batches/{batchCode}/trace
   - POST /api/certificates
   - POST /api/batches/{batchCode}/certificates
   - GET /api/batches/{batchCode}/audit
4. SSMS query result from:
   - select * from scm.Batches where BatchCode = 'BF-DEMO-20260407-104120'
   - select * from scm.BatchEvents where BatchId = 'b1c9fa0e-98cf-4046-96ab-cdfad043f77c'
   - select * from audit.AuditLogs where PayloadText like '%BF-DEMO-20260407-104120%'
5. SSMS/terminal output showing immutable update blocked with Msg 51001.
