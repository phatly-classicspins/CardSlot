# CardSlot Level Builder — bản thử

Mở `index.html` bằng Chrome/Edge. Không cần cài package, không cần internet.
Hoặc chạy `node serve.cjs`, rồi mở http://127.0.0.1:8767.

- Chọn một trong 5 màn mẫu, kéo thả cọc, chọn màu để thêm cọc.
- Chỉnh tầng, vùng chặn W/H, số lá, kiểu xòe thẳng/vòng, chiều thuận/ngược và hướng xòe.
- Cọc phía trên tự tìm cọc đỡ theo `StackPlacementRules`; có thể chọn `on_stack` rõ ràng khi cần. Tầng phải thấp hơn, giao vùng và cùng góc xòe.
- Đường tím là quan hệ nối xòe. Bật **Vùng chặn** để xem rectangle gameplay và các đường chặn màu cam. Quan hệ chặn được suy ra theo rule hiện tại của game, không lưu trường BlockedNodes không được game hỗ trợ.
- **Dàn cọc như game** mô phỏng bước tránh va chạm giữa các nhóm cọc của Board3DView. Tắt để xem hình học trước khi dàn. Đây là preview SVG mặt trên; không phải render 3D/camera Unity chính xác từng pixel.
- **Bấm thử** mô phỏng lấy cả cọc, chặn theo tầng/màu, hàng đợi chia theo từng cột đích, buffer, thắng/thua và kiểm tra nước đi an toàn. Không có booster/revive. Khi thắng có thể lưu `solution`.
- Ctrl+Z / Ctrl+Y, Ctrl+D, Delete; F vừa khung. Kéo nền để pan, lăn chuột zoom.
- JSON giữ định dạng CardSlot hiện có; giữ các trường bổ sung. Chỉnh config sẽ bỏ solution cũ để tránh lưu lời giải đã lỗi thời.
- **Lưu bản nháp** lưu trên trình duyệt, không ghi file project. Luôn xuất JSON để có bản độc lập.

## Đưa config vào Unity

1. Sửa hết lỗi trong mục Kiểm tra config và xuất JSON.
2. Thay file tương ứng trong `Assets/CardSlot/Content/Levels/` (sao lưu trước nếu muốn giữ phiên bản cũ).
3. Với màn mới, chạy menu `CardSlot/Content/Build` để cập nhật LevelCatalog. Màn thay thế dùng catalog hiện có.
4. Mở màn trong Unity để kiểm tra hình ảnh và gameplay thực tế.

Khoảng cách xòe hiện dùng quy tắc toàn game: 18 units/lá, bước vòng 12°, độ mở cọc gốc tối đa 60°. Tool không xuất trường khoảng cách tùy ý vì runtime chưa đọc trường đó. Cọc trên tiếp tục chuỗi lá của cọc gốc nên có thể mở vượt góc gốc.

Màu lấy từ `DesignTokens.CardFace`: 0 đỏ, 1 xanh dương, 2 vàng, 3 xanh lá, 4 tím, 5 cam, 6 hồng, 7 cyan.

## Kiểm tra

`node core.test.cjs`

Các màn mẫu là snapshot của 5 file level hiện tại. Import JSON để mở phiên bản mới hơn.
