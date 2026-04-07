# BlueFood SCM - ITPJ2604

Tai lieu tong hop duy nhat cho do an.

## 1. Ten de tai

He thong quan ly chuoi cung ung thuc pham sach (BlueFood SCM).

## 2. Noi dung

He thong truy xuat nguon goc lo hang tu nong trai den cua hang, su dung QR de tra cuu cong khai, ghi nhan lich su su kien va audit log theo huong append-only.

## 3. Chuc nang chinh

- Quan ly lo hang: tao lo, sinh QR token va trace URL.
- Theo doi chuoi cung ung: them su kien (CREATED, SHIPPED, RECEIVED...).
- Tra cuu truy xuat: theo batch code hoac theo QR token.
- Quan ly chung chi: tao chung chi va gan vao lo hang.
- Audit log: ghi nhan thay doi, khong cho sua/xoa bang chuc nang ung dung.
- Trang public cho dien thoai: `/trace/public/{qrToken}`.

## 4. Cong nghe su dung

- Backend: ASP.NET Core 8 Web API.
- Frontend: React + TypeScript + Vite.
- Database: SQL Server.
- Mobile scan module (tu chon): Flutter (`bluefood_scan_app`) dung lai API hien tai.

## 5. Cau truc thu muc

- Backend API: `BlueFood_Api/`
- Frontend web: `../BlueFood_frontend/`
- SQL scripts: `Database/`

## 6. Cau hinh khi dao tao/dua len git (quan trong)

Neu ban clone project tren may tinh khac:

**Backend**: Tu dong detect LAN IP, khong can cau hinh.
- Neu muon chi dinh URL, tao file `.env` voi:
  ```
  BLUEFOOD_PUBLIC_BASE_URL=http://{YOUR_LAN_IP}:5085/t/
  ```

**Frontend**: Phai cau hinh API endpoint theo may tinh moi:
1. Tao file `.env` tu `.env.example`
2. Sua gia tri dung IP cua ban:
   ```
   VITE_API_BASE_URL=http://{YOUR_LAN_IP}:5085
   ```
   (Vi du: `http://192.168.1.5:5085`)

## 7. Cach mo demo (ngan gon)

### Buoc 1: Chay backend

Mo PowerShell tai thu muc `BlueFood_backend`:

```powershell
dotnet restore BlueFood_Api/BlueFood.Api.csproj
$env:ASPNETCORE_ENVIRONMENT='Development'
dotnet run --project BlueFood_Api/BlueFood.Api.csproj --urls "http://0.0.0.0:5085"
```

### Buoc 2: Chay frontend

Mo PowerShell khac tai thu muc `BlueFood_frontend`:

```powershell
npm install
npm run dev -- --host 0.0.0.0 --port 5173
```

### Buoc 3: Mo man hinh demo

- Web: `http://localhost:5173`
- Swagger: `http://localhost:5085/swagger`

### Buoc 4: Luong demo nhanh

1. Tao batch + QR tren web.
2. Dung camera dien thoai quet QR hien tren man hinh laptop.
3. Dien thoai mo URL public trace va hien thong tin lo hang.
4. (Tuy chon) Bam `Ghi nhan SHIPPED`, sau do tai trace de thay timeline cap nhat.
5. (Tuy chon) Tao chung chi, gan vao batch, xem danh sach va audit.

## 8. Luu y khi quet QR bang dien thoai

- Dien thoai va may tinh phai cung Wi-Fi.
- Backend phai chay voi `0.0.0.0:5085` (khong chi localhost).
- Firewall Windows can mo cong TCP 5085.
- Neu doi mang, dung LAN IP moi trong trace URL.

## 9. Endpoint chinh

- `POST /api/batches`
- `POST /api/batches/{batchCode}/events`
- `POST /api/certificates`
- `POST /api/batches/{batchCode}/certificates`
- `GET /api/batches/{batchCode}/trace`
- `GET /api/batches/{batchCode}/audit`
- `GET /api/trace/{qrToken}`
- `GET /api/trace/{qrToken}/qrcode`
- `GET /trace/public/{qrToken}`

## 10. Giao diện
