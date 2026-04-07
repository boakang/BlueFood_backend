# BlueFood SCM - ITPJ2604

Tài liệu tổng hợp duy nhất cho đồ án.

## 1. Tên đề tài

Hệ thống quản lý chuỗi cung ứng thực phẩm sạch (BlueFood SCM).

## 2. Nội dung

Hệ thống truy xuất nguồn gốc lô hàng từ nông trại đến cửa hàng, sử dụng QR để truy cập công khai, ghi nhận lịch sử sự kiện và audit log theo hướng append-only.

## 3. Chức năng chính

- Quản lý lô hàng: tạo lô, sinh QR token và trace URL
- Theo dõi chuỗi cung ứng: thêm sự kiện (CREATED, SHIPPED, RECEIVED...)
- Truy cứu truy xuất: theo batch code hoặc QR token
- Quản lý chứng chỉ: tạo và gán vào lô hàng
- Audit log: ghi nhận thay đổi, không cho sửa/xóa qua ứng dụng
- Trang công khai cho điện thoại: `/trace/public/{qrToken}`

## 4. Công nghệ sử dụng

- Backend: ASP.NET Core 8 Web API
- Frontend: React + TypeScript + Vite
- Database: SQL Server
- Mobile (tùy chọn): Flutter (`bluefood_scan_app`)

## 5. Cấu hình khi clone lần đầu

**Backend**: Tự động detect LAN IP, không cần cấu hình.
- Nếu muốn override, tạo `.env` với:
  ```
  BLUEFOOD_PUBLIC_BASE_URL=http://{YOUR_LAN_IP}:5085/t/
  ```

**Frontend**: Cần cấu hình endpoint:
1. Copy `.env.example` → `.env`
2. Sửa giá trị với IP của bạn:
   ```
   VITE_API_BASE_URL=http://{YOUR_LAN_IP}:5085
   ```

## 6. Chạy demo

**Bước 1: Backend** (PowerShell tại `BlueFood_backend`)
```powershell
dotnet run 
```

**Bước 2: Frontend** (PowerShell khác tại `BlueFood_frontend`)
```powershell
npm install
npm run dev -- --host 0.0.0.0 --port 5173
```

**Bước 3: Truy cập**
- Web: `http://localhost:5173`
- Swagger: `http://localhost:5085/swagger`

**Bước 4: Demo workflow**
1. Tạo batch + QR trên web
2. Điện thoại cùng Wi-Fi, quét QR trên màn hình laptop
3. Điện thoại mở URL public trace để xem thông tin lô hàng
4. (Tùy chọn) Thêm sự kiện SHIPPED, xem timeline cập nhật
5. (Tùy chọn) Tạo chứng chỉ, gán vào batch, xem audit log

## 7. Lưu ý khi quét QR bằng điện thoại

- Điện thoại và máy tính phải cùng Wi-Fi
- Backend phải chạy với `0.0.0.0:5085` (không chỉ localhost)
- Firewall Windows cần mở cổng TCP 5085
- Khi đổi mạng, cập nhật IP mới trong trace URL

## 8. Endpoint chính

- `POST /api/batches` - Tạo lô hàng
- `POST /api/batches/{batchCode}/events` - Thêm sự kiện
- `POST /api/certificates` - Tạo chứng chỉ
- `GET /api/batches/{batchCode}/trace` - Lấy timeline
- `GET /api/batches/{batchCode}/audit` - Audit log
- `GET /trace/public/{qrToken}` - Trang công khai cho điện thoại

## 9. Các Trang khác
[Frontend](https://github.com/boakang/BlueFood_frontend)
