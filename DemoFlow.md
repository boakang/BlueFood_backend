# BlueFood SCM - Kịch bản demo 5 phút

## 1. Mục tiêu demo

Chứng minh đúng luồng của project hiện tại theo thứ tự: Swagger API, database, frontend và public QR.

Phạm vi cần thể hiện:

1. Quản lý lô hàng
2. Quản lý chứng chỉ
3. Theo dõi vận chuyển
4. Audit log và dữ liệu lịch sử
5. QR Code truy xuất

Tiêu chí cần nói rõ trong phần demo:

1. Lô hàng tạo ra có QR và trace URL
2. Dữ liệu tạo trên frontend lưu được xuống SQL Server
3. API Swagger trả về đúng dữ liệu đang lưu trong database
4. Trang public trace mở được từ QR trên điện thoại

## 2. Chuẩn bị trước khi demo

### A. Chạy Backend
```powershell
$env:ASPNETCORE_ENVIRONMENT='Development'
dotnet run --project BlueFood_Api/BlueFood.Api.csproj --urls http://0.0.0.0:5085
```

- Backend dùng SQL Server `BAKHANG\SQLEXPRESS`
- Database mặc định: `BlueFoodSCM`
- Khi chạy ở Development, Swagger sẽ mở được ở `/swagger`

### B. Chạy Frontend
```powershell
npm run dev -- --host 0.0.0.0 --port 5173
```

Kiểm tra file `BlueFood_frontend\.env`:
- Nếu demo trên cùng máy và chỉ dùng trình duyệt desktop, có thể để trống `VITE_API_BASE_URL`
- Nếu demo có điện thoại hoặc máy khác truy cập, nên đặt:
  ```
  VITE_API_BASE_URL=http://{YOUR_LAN_IP}:5085
  ```

### C. Mở các trang cần thiết
- Frontend: `http://localhost:5173`
- Swagger API: `http://localhost:5085/swagger`
- SQL Server Management Studio: kết nối `BAKHANG\SQLEXPRESS`, database `BlueFoodSCM`

### D. URL public trace
- URL ngắn khi quét QR: `http://{YOUR_LAN_IP}:5085/t/{qrToken}`
- URL public trace thực tế: `http://{YOUR_LAN_IP}:5085/trace/public/{qrToken}`

### E. Reset dữ liệu
Nếu cần làm sạch dữ liệu trước khi demo lại, dùng `Database/ResetDemoData_TruncateSafe.sql`.

## 3. Kịch bản demo đề xuất

### Bước 1: Kiểm tra API bằng Swagger

1. Mở Swagger.
2. Gọi `GET /api/management/batches` để xem danh sách lô hàng.
3. Gọi `GET /api/management/certificates` để xem danh sách chứng chỉ.
4. Chọn một `batchCode` bất kỳ rồi gọi tiếp:
	- `GET /api/batches/{batchCode}/trace`
	- `GET /api/batches/{batchCode}/certificates`
	- `GET /api/batches/{batchCode}/audit`
5. Nếu muốn kiểm tra quản lý chứng chỉ theo lô, gọi thêm:
	- `GET /api/management/certificates/{certificateId}/batches`

Khi trình bày, nhấn mạnh rằng Swagger là lớp kiểm tra đầu tiên để xác nhận backend đang trả dữ liệu đúng.

### Bước 2: Kiểm tra dữ liệu trong database

Mở SSMS và chạy:

```sql
SELECT TOP (20) * FROM scm.Batches ORDER BY CreatedAt DESC;
SELECT TOP (20) * FROM scm.BatchEvents ORDER BY EventNo DESC;
SELECT TOP (20) * FROM scm.Certificates ORDER BY CertificateId DESC;
SELECT TOP (20) * FROM scm.BatchCertificates ORDER BY AttachedAt DESC;
SELECT TOP (20) * FROM audit.AuditLogs ORDER BY AuditId DESC;
```

Giải thích ngắn:
1. `scm.Batches` lưu lô hàng
2. `scm.BatchEvents` lưu lịch sử vận chuyển
3. `scm.Certificates` lưu chứng chỉ
4. `scm.BatchCertificates` lưu quan hệ gắn chứng chỉ vào lô
5. `audit.AuditLogs` lưu lịch sử thao tác hệ thống

### Bước 3: Thao tác trên frontend

1. Mở tab `Tạo mới lô hàng`.
2. Nhập một batch code mới, product name, nông trại và người thao tác.
3. Bấm tạo lô để sinh QR token và trace URL.
4. Vào tab `Quản lý lô hàng` để thấy lô vừa tạo xuất hiện trong danh sách.
5. Vào tab `Quản lý chứng chỉ` để tạo chứng chỉ và theo dõi các lô đang gắn chứng chỉ.
6. Nếu cần cập nhật trạng thái, quay lại workflow và ghi nhận event mới.

Khi trình bày, nhấn mạnh frontend chỉ là lớp thao tác nghiệp vụ, dữ liệu cuối cùng vẫn phải khớp với Swagger và database.

### Bước 4: Quét QR để truy xuất công khai

1. Mở QR hoặc trace URL trên frontend.
2. Dùng điện thoại quét QR.
3. Điện thoại sẽ mở trang public trace qua `/t/{qrToken}` và chuyển sang trang truy xuất công khai.
4. Chỉ cho người xem thấy:
	- Batch code
	- Tên sản phẩm
	- Trạng thái hiện tại
	- Timeline vận chuyển
	- Chứng chỉ đính kèm

### Bước 5: Chứng minh audit và lịch sử dữ liệu

1. Quay lại Swagger hoặc SSMS.
2. Mở `GET /api/batches/{batchCode}/audit` để xem log thao tác.
3. Nhấn mạnh:
	- Tạo lô, thêm event, gắn chứng chỉ đều sinh dữ liệu mới
	- Dữ liệu lịch sử không nên được sửa trực tiếp từ ứng dụng
	- Audit log là phần ghi nhận thay đổi để phục vụ kiểm tra sau này

## 4. Demo trên SQL Server

### A. Kiểm tra dữ liệu lô hàng

Chạy trong SSMS:

```sql
SELECT TOP (20) * FROM scm.Batches ORDER BY CreatedAt DESC;
SELECT TOP (20) * FROM scm.BatchEvents ORDER BY EventNo DESC;
SELECT TOP (20) * FROM scm.Certificates ORDER BY CertificateId DESC;
SELECT TOP (20) * FROM scm.BatchCertificates ORDER BY AttachedAt DESC;
SELECT TOP (20) * FROM audit.AuditLogs ORDER BY AuditId DESC;
```

### B. Kiểm tra liên kết chứng chỉ

```sql
SELECT
	 b.BatchCode,
	 c.CertificateCode,
	 c.CertificateName,
	 bc.AttachedAt,
	 bc.AttachedBy
FROM scm.BatchCertificates bc
JOIN scm.Batches b ON b.BatchId = bc.BatchId
JOIN scm.Certificates c ON c.CertificateId = bc.CertificateId
ORDER BY bc.AttachedAt DESC;
```

### C. Kiểm tra dữ liệu trace

```sql
SELECT TOP (20)
	 BatchCode,
	 ProductName,
	 CurrentStatus,
	 QRToken,
	 TraceUrl
FROM scm.BatchQRCodes
ORDER BY CreatedAt DESC;
```

## 5. Trình bày theo yêu cầu đề tài

Khi thuyết trình, có thể map như sau:

1. Quản lý lô hàng
	- Tạo batch trên frontend
	- Kiểm tra lại bằng `GET /api/management/batches`
	- Đối chiếu với bảng `scm.Batches`
2. Quản lý chứng chỉ
	- Tạo certificate bằng frontend hoặc Swagger
	- Kiểm tra bằng `GET /api/management/certificates`
	- Đối chiếu với `scm.Certificates` và `scm.BatchCertificates`
3. Theo dõi vận chuyển
	- Ghi nhận event `SHIPPED` hoặc `RECEIVED`
	- Kiểm tra bằng `GET /api/batches/{batchCode}/trace`
	- Đối chiếu với `scm.BatchEvents`
4. Audit log
	- Xem `GET /api/batches/{batchCode}/audit`
	- Đối chiếu với `audit.AuditLogs`
5. QR Code truy xuất
	- Quét QR từ điện thoại
	- Mở trang public trace
	- Xem batch, timeline và certificate

## 6. Gắn với tiêu chí thành công

1. 100% lô hàng có mã QR và truy xuất được
	- Mỗi batch tạo mới đều sinh `qrToken` và `traceUrl`
2. Truy xuất dưới 2 giây
	- Demo trên mạng LAN nội bộ, quét QR hoặc mở public trace trực tiếp
3. Mỗi thay đổi trạng thái được ghi vào audit log
	- Kiểm tra qua Swagger và bảng `audit.AuditLogs`
4. Hệ thống quản lý được dữ liệu theo mô hình SQL Server
	- Đối chiếu bảng `scm.Batches`, `scm.BatchEvents`, `scm.Certificates`, `scm.BatchCertificates`
5. Không có dữ liệu lịch sử bị chỉnh sửa hoặc xóa từ giao diện
	- Trình bày theo hướng append-only và kiểm tra bằng API/DB

## 7. Ảnh nên chụp khi bảo vệ

1. Màn hình Swagger đang gọi được các API chính
2. Màn hình SSMS hiển thị dữ liệu các bảng chính
3. Màn hình frontend có danh sách lô hàng và chứng chỉ
4. Màn hình public trace mở từ QR trên điện thoại
5. Kết quả audit log hoặc trace trả về đúng dữ liệu vừa tạo
