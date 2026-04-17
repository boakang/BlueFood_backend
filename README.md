# IS208.Q21 - Nhóm Horizon

Thành viên: Bá Khang, Duy Tài, Mậu Anh, Phương Anh, Quốc Đạt (Nhóm trưởng).
[Website giới thiệu nhóm (vào bằng mail trường)](https://sites.google.com/gm.uit.edu.vn/horizon/trang-ch%E1%BB%A7?authuser=2&pli=1)

## 1. Tên đề tài

Hệ thống quản lý chuỗi cung ứng thực phẩm sạch BlueFood SCM.

## 2. Nội dung

BlueFood SCM là hệ thống quản lý và truy xuất nguồn gốc lô hàng từ nông trại đến các khâu trung gian, cửa hàng và trang tra cứu công khai qua QR. Dữ liệu được lưu trong SQL Server, backend cung cấp REST API, frontend dùng React để thao tác quản lý và theo dõi trace.

## 3. Chức năng chính

- Quản lý lô hàng: tạo lô, sinh QR token, lưu trace URL và xem danh sách quản lý
- Theo dõi chuỗi cung ứng: ghi nhận các sự kiện như CREATED, SHIPPED, RECEIVED
- Tra cứu truy xuất: tìm theo batch code hoặc QR token
- Quản lý chứng chỉ: tạo chứng chỉ, gắn hoặc thay thế chứng chỉ cho lô hàng
- Audit log: ghi nhận thay đổi theo hướng append-only
- Trang công khai cho điện thoại: xem trace theo QR token

## 4. Công nghệ sử dụng

- Backend: ASP.NET Core 8 Web API
- Frontend: React + TypeScript + Vite
- Database: SQL Server
- Mobile tùy chọn: Flutter trong thư mục bluefood_scan_app

## 5. Cấu hình khi clone lần đầu

Backend dùng connection string SQL Server trong BlueFood_Api/appsettings.json hoặc appsettings.Development.json.
- Database mặc định: BlueFoodSCM
- Server mặc định: BAKHANG\SQLEXPRESS
- Xác thực: Windows Integrated Security

Frontend cần cấu hình endpoint API:
1. Copy .env.example thành .env
2. Sửa giá trị endpoint cho đúng máy chạy backend:
  ```
  VITE_API_BASE_URL=http://localhost:5085
  ```

Nếu chạy trong cùng máy và dùng Vite proxy, có thể để trống VITE_API_BASE_URL.

## 6. Chạy demo

**Bước 1: Backend** (PowerShell tại BlueFood_backend)
```powershell
cd BlueFood_Api
dotnet run
```

**Bước 2: Frontend** (PowerShell tại BlueFood_frontend)
```powershell
npm install
npm run dev -- --host 0.0.0.0 --port 5173
```

**Bước 3: Truy cập**
- Web: http://localhost:5173
- Swagger: http://localhost:5085/swagger

**Bước 4: Demo workflow**
1. Tạo batch và QR trên web
2. Ghi nhận các sự kiện vận chuyển
3. Tạo chứng chỉ và gắn vào batch
4. Mở trang quản lý để xem dữ liệu, trace và chứng chỉ
5. Dùng QR để truy cập trang công khai trên điện thoại

## 7. Lưu ý khi quét QR bằng điện thoại

- Điện thoại và máy tính phải cùng mạng
- Backend phải chạy ở địa chỉ có thể truy cập từ thiết bị quét QR
- Nếu đổi máy hoặc đổi mạng, cần cập nhật lại base URL trong trace URL

## 8. Endpoint chính

- POST /api/batches - Tạo lô hàng
- POST /api/batches/{batchCode}/events - Thêm sự kiện
- POST /api/batches/{batchCode}/certificates - Gán hoặc thay đổi chứng chỉ cho lô hàng
- GET /api/batches/{batchCode}/trace - Lấy timeline truy xuất
- GET /api/batches/{batchCode}/certificates - Lấy chứng chỉ của lô hàng
- GET /api/management/batches - Danh sách lô hàng quản lý
- GET /api/management/certificates - Danh sách chứng chỉ quản lý
- GET /api/management/certificates/{certificateId}/batches - Danh sách lô hàng theo chứng chỉ
- GET /api/trace/{qrToken} - Lấy trace theo QR token
- GET /api/trace/{qrToken}/qrcode - Sinh ảnh QR code

## 9. Các Trang khác
[Frontend](https://github.com/boakang/BlueFood_frontend)
