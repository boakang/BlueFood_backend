# BlueFoodSCM Database ERD and Data Dictionary

Generated from live SQL Server metadata on 2026-04-07.
Source: BAKHANG\\SQLEXPRESS, database BlueFoodSCM.

## ERD

```mermaid
erDiagram
    Partners ||--o{ Batches : "FarmPartnerId"
    Batches ||--o{ BatchEvents : "BatchId"
    Partners ||--o{ BatchEvents : "FromPartnerId"
    Partners ||--o{ BatchEvents : "ToPartnerId"
    Batches ||--|| BatchQRCodes : "BatchId"
    Batches ||--o{ BatchCertificates : "BatchId"
    Certificates ||--o{ BatchCertificates : "CertificateId"

    Partners {
        int PartnerId PK
        tinyint PartnerType
        nvarchar(50) PartnerCode UK
        nvarchar(200) PartnerName
        bit IsActive
        datetime2(3) CreatedAt
    }

    Batches {
        uniqueidentifier BatchId PK
        nvarchar(40) BatchCode UK
        nvarchar(200) ProductName
        int FarmPartnerId FK
        nvarchar(30) CurrentStatus
        date ProductionDate
        date ExpiryDate
        nvarchar(100) CreatedBy
        datetime2(3) CreatedAt
    }

    BatchEvents {
        bigint BatchEventId PK
        uniqueidentifier BatchId FK
        int EventNo
        nvarchar(30) EventType
        int FromPartnerId FK
        int ToPartnerId FK
        nvarchar(200) LocationText
        nvarchar(500) NoteText
        datetime2(3) EventTime
        nvarchar(100) CreatedBy
        ## Sơ đồ ERD

        Các tên bảng và tên cột trong sơ đồ giữ nguyên theo schema SQL, chỉ Việt hóa phần mô tả quan hệ.

    BatchQRCodes {
        uniqueidentifier BatchId PK, FK
            Partners ||--o{ Batches : "Đối tác nông trại"
        nvarchar(500) TraceUrl
            Batches ||--o{ BatchEvents : "Lô hàng"
    }
            Partners ||--o{ BatchEvents : "Đối tác nguồn"
    Certificates {
            Partners ||--o{ BatchEvents : "Đối tác đích"
        nvarchar(60) CertificateCode UK
            Batches ||--|| BatchQRCodes : "Mã lô"
        nvarchar(200) IssuedBy
            Batches ||--o{ BatchCertificates : "Mã lô"
        date ExpiredDate
            Certificates ||--o{ BatchCertificates : "Mã chứng chỉ"
        datetime2(3) CreatedAt
    }

    BatchCertificates {
        bigint BatchCertificateId PK
        uniqueidentifier BatchId FK
        bigint CertificateId FK
        datetime2(3) AttachedAt
        nvarchar(100) AttachedBy
    }

    AuditLogs {
        bigint AuditId PK
        nvarchar(100) EntityName
        nvarchar(100) EntityId
        nvarchar(30) ActionType
        datetime2(3) ActionAt
        nvarchar(100) Actor
        nvarchar(max) PayloadText
        varbinary(32) PrevHash
        varbinary(32) ThisHash
    }
```

## Business Rules and Integrity

- Append-only history:
  - `scm.BatchEvents` blocked from UPDATE/DELETE by trigger `TR_BatchEvents_BlockUpdateDelete`.
  - `scm.BatchCertificates` blocked from UPDATE/DELETE by trigger `TR_BatchCertificates_BlockUpdateDelete`.
  - `audit.AuditLogs` blocked from UPDATE/DELETE by trigger `TR_AuditLogs_BlockUpdateDelete`.
- Unique business keys:
  - `scm.Partners.PartnerCode`
  - `scm.Batches.BatchCode`
  - `scm.BatchQRCodes.QRToken`
  - `scm.Certificates.CertificateCode`
  - `scm.BatchEvents (BatchId, EventNo)`
  - `scm.BatchCertificates (BatchId, CertificateId)`

## Table Descriptions

### `scm.Partners`
Mục đích: Dữ liệu danh mục của các tác nhân trong chuỗi cung ứng (nông trại, vận chuyển, kho, cửa hàng).

| Column | Type | Null | Key | Description |
|---|---|---|---|---|
| PartnerId | int | No | PK | Mã định danh nội bộ của đối tác. |
| PartnerType | tinyint | No |  | Loại đối tác (nông trại/vận chuyển/kho/cửa hàng). |
| PartnerCode | nvarchar(50) | No | UK | Mã đối tác nghiệp vụ. |
| PartnerName | nvarchar(200) | No |  | Tên hiển thị. |
| IsActive | bit | No |  | Cờ trạng thái đang hoạt động. |
| CreatedAt | datetime2(3) | No |  | Thời điểm tạo. |

### `scm.Batches`
Mục đích: Thực thể lô hàng trung tâm phục vụ truy xuất nguồn gốc.

| Column | Type | Null | Key | Description |
|---|---|---|---|---|
| BatchId | uniqueidentifier | No | PK | Mã định danh nội bộ của lô hàng (GUID). |
| BatchCode | nvarchar(40) | No | UK | Mã lô hàng duy nhất, dễ đọc. |
| ProductName | nvarchar(200) | No |  | Tên sản phẩm của lô hàng. |
| FarmPartnerId | int | Yes | FK | Đối tác nông trại sản xuất. |
| CurrentStatus | nvarchar(30) | No |  | Trạng thái hiện tại gần nhất. |
| ProductionDate | date | Yes |  | Ngày sản xuất. |
| ExpiryDate | date | Yes |  | Ngày hết hạn. |
| CreatedBy | nvarchar(100) | No |  | Người/tài khoản tạo lô. |
| CreatedAt | datetime2(3) | No |  | Thời điểm tạo. |

### `scm.BatchEvents`
Mục đích: Các sự kiện theo dòng thời gian của từng lô hàng, không cho sửa/xóa.

| Column | Type | Null | Key | Description |
|---|---|---|---|---|
| BatchEventId | bigint | No | PK | Mã định danh dòng sự kiện. |
| BatchId | uniqueidentifier | No | FK | Lô hàng liên kết. |
| EventNo | int | No | UK(part) | Số thứ tự sự kiện trong từng lô. |
| EventType | nvarchar(30) | No |  | Loại sự kiện (CREATED, SHIPPED, RECEIVED, ...). |
| FromPartnerId | int | Yes | FK | Đối tác nguồn. |
| ToPartnerId | int | Yes | FK | Đối tác đích. |
| LocationText | nvarchar(200) | Yes |  | Địa điểm dạng tự do. |
| NoteText | nvarchar(500) | Yes |  | Ghi chú của sự kiện. |
| EventTime | datetime2(3) | No |  | Thời điểm xảy ra sự kiện. |
| CreatedBy | nvarchar(100) | No |  | Người thực hiện sự kiện. |

### `scm.BatchQRCodes`
Mục đích: Mỗi lô có một QR token và một đường dẫn truy xuất riêng.

| Column | Type | Null | Key | Description |
|---|---|---|---|---|
| BatchId | uniqueidentifier | No | PK, FK | Cùng mã lô, quan hệ 1-1. |
| QRToken | nvarchar(80) | No | UK | Token duy nhất được mã hóa trong QR. |
| TraceUrl | nvarchar(500) | No |  | Đường dẫn truy xuất công khai. |
| CreatedAt | datetime2(3) | No |  | Thời điểm tạo. |

### `scm.Certificates`
Mục đích: Danh mục chứng chỉ gốc (ví dụ: VietGAP).

| Column | Type | Null | Key | Description |
|---|---|---|---|---|
| CertificateId | bigint | No | PK | Mã định danh dòng chứng chỉ. |
| CertificateCode | nvarchar(60) | No | UK | Mã chứng chỉ nghiệp vụ. |
| CertificateName | nvarchar(200) | No |  | Tên hiển thị của chứng chỉ. |
| IssuedBy | nvarchar(200) | Yes |  | Đơn vị cấp. |
| IssuedDate | date | Yes |  | Ngày cấp. |
| ExpiredDate | date | Yes |  | Ngày hết hạn. |
| FileUrl | nvarchar(500) | Yes |  | URL lưu tệp chứng chỉ. |
| CreatedAt | datetime2(3) | No |  | Thời điểm tạo. |

### `scm.BatchCertificates`
Mục đích: Gắn chứng chỉ vào lô hàng theo kiểu không cho sửa/xóa.

| Column | Type | Null | Key | Description |
|---|---|---|---|---|
| BatchCertificateId | bigint | No | PK | Mã định danh dòng gắn kết. |
| BatchId | uniqueidentifier | No | FK, UK(part) | Lô hàng được gắn. |
| CertificateId | bigint | No | FK, UK(part) | Chứng chỉ được gắn. |
| AttachedAt | datetime2(3) | No |  | Thời điểm gắn. |
| AttachedBy | nvarchar(100) | No |  | Người thực hiện gắn chứng chỉ. |

### `audit.AuditLogs`
Mục đích: Nhật ký audit chỉ ghi thêm, có trường băm chuỗi để kiểm tra toàn vẹn.

| Column | Type | Null | Key | Description |
|---|---|---|---|---|
| AuditId | bigint | No | PK | Mã định danh dòng audit. |
| EntityName | nvarchar(100) | No |  | Loại thực thể logic (BATCH, BATCH_EVENT, ...). |
| EntityId | nvarchar(100) | No |  | Mã thực thể dưới dạng văn bản. |
| ActionType | nvarchar(30) | No |  | Hành động audit (INSERT, STATUS_CHANGE, ATTACH_CERT). |
| ActionAt | datetime2(3) | No |  | Thời điểm thực hiện. |
| Actor | nvarchar(100) | No |  | Người/tài khoản thực hiện. |
| PayloadText | nvarchar(max) | Yes |  | Dữ liệu payload đã tuần tự hóa. |
| PrevHash | varbinary(32) | Yes |  | Mã băm trước đó trong chuỗi. |
| ThisHash | varbinary(32) | No |  | Mã băm hiện tại. |

## View (Read Model)

### `scm.vw_BatchTrace`
Mục đích: Mô hình đọc đã tổng hợp để xem timeline truy xuất, ghép từ lô hàng, sự kiện, đối tác và dữ liệu QR.
Các cột chính: `BatchCode`, `ProductName`, `CurrentStatus`, `QRToken`, `TraceUrl`, `EventNo`, `EventType`, `EventTime`, tên đối tác, ghi chú và địa điểm sự kiện.
