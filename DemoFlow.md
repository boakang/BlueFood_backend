# BlueFood SCM - Hướng dẫn demo 5 phút

## Chuẩn bị

1. Chạy backend: `dotnet run --project BlueFood.Api/BlueFood.Api.csproj`
2. Chạy frontend: `npm run dev`
3. Mở `http://localhost:5173` và `http://localhost:5085/swagger`

## Luồng demo

1. Tạo lô hàng trên giao diện web, nhập mã lô và người thao tác, rồi bấm tạo batch + QR.
2. Thêm một sự kiện, ví dụ `SHIPPED`, để lịch sử được ghi nối tiếp, không sửa đè.
3. Tra cứu theo batch hoặc theo QR để hiển thị timeline với giờ Việt Nam.
4. Tạo chứng chỉ và gán vào lô hàng, sau đó kiểm tra chứng chỉ đã xuất hiện trong danh sách.
5. Mở API audit của lô hàng để chứng minh có nhật ký thao tác.

## Kiểm tra tính toàn vẹn

1. Trong SSMS, thử `UPDATE` một dòng của `scm.BatchEvents`.
2. Trình bày lỗi từ trigger chặn sửa/xóa.
3. Kết luận: lịch sử là append-only và không thể chỉnh sửa trực tiếp.

## Ảnh nên chụp

1. Màn hình web có timeline và chứng chỉ.
2. Kết quả audit của batch trên Swagger.
3. Kết quả truy vấn `Batches`, `BatchEvents`, `BatchCertificates`, `AuditLogs` trong SSMS.
4. Thông báo lỗi khi thử sửa dữ liệu lịch sử.
