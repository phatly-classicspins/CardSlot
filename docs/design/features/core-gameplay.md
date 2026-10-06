# Spec: Gameplay lõi + HUD

> v1.1 · Bước 1 · đã duyệt (Phat, 06-10-2026) · v1.1: CR-001 (Bước 2). Luật: `GDD.md` §2 (R-1…R-15).

**Mục đích.** Người chơi chạm các chồng bài để gửi lá lên đích cùng màu, quản lý ô tạm, dọn sạch bàn.

## Luồng người chơi

1. Vào `GameplayScreen` → model nạp level, kiểm tra R-1, khởi tạo theo R-2 → phát `level_started`.
2. Người chơi chạm một `stack` đang `open` → model xử lý `tap` (R-6…R-10) → view phát lại kết quả bằng hoạt ảnh.
3. Lặp lại cho tới R-11 (thắng → `WinDialog`) hoặc R-12 (thua → `LoseDialog`).
4. Nút ↻ trên TopBar: hỏi xác nhận → chơi lại level từ đầu (bắt đầu `attempt` mới; booster đã dùng không hoàn lại).

## Trạng thái

`loading` → `playing` ⇄ (`animating` chỉ là trạng thái của view, không chặn input — R-8) → `won` | `lost`.

## Dữ liệu lưu

Không lưu gì giữa level (GDD §5). Kết quả level được lưu ở `level-progression`.

## Trường hợp biên

- Chạm vào chồng bị che: chồng lắc nhẹ, phát `sfx_tap_invalid`, model không đổi (R-4).
- Chạm khi đã `won`/`lost`: bỏ qua.
- Chạm nhanh nhiều lần: mọi `tap` hợp lệ được xử lý theo thứ tự; hoạt ảnh xếp hàng nhưng không bao giờ lệch với model.
- App về nền: tự mở `PauseDialog`.

## Key text

`gameplay.level` ("Level {0}") · `gameplay.restart.confirm` ("Restart level?") · `gameplay.restart.note` ("Boosters used won't come back.", v1.1 CR-001) · `gameplay.restart.yes` · `gameplay.restart.no`

## Visual Contract

- Ba vùng tách bạch từ trên xuống: hàng `target` → `BufferBar` → `board`. Mỗi vùng có nền riêng, không chồng lên nhau ở 9:16 và 9:20.
- Người chơi **nhìn là biết** chồng nào chạm được: chồng bị che tối hơn rõ rệt (độ sáng ≤ 70%) và không có viền nổi.
- Mỗi lá hiển thị màu **và** hoạ tiết (`glossary.md`).
- Lá bay từ chồng tới đích theo đường cong, 0.25–0.4 giây mỗi lá, các lá trong một `run` bay cách nhau 0.05 giây.
- `target` hoàn thành: nảy lên rồi nổ thành hạt cùng màu; `target` mới trượt vào đúng vị trí đó.
- `BufferBar` cho thấy rõ số chỗ còn trống. Khi ≤ 2 chỗ thì nhấp nháy đỏ.

## Acceptance

- [ ] Test model cho mỗi luật R-1…R-15, mỗi luật có ít nhất một test, chạy bằng `dotnet test`.
- [ ] Phát lại cùng một chuỗi `tap` trên cùng level luôn cho cùng trạng thái cuối (R-15).
- [ ] Solver xác nhận cả 30 level đều giải được mà không cần booster.
- [ ] Ảnh chụp Gameplay ở 1080×1920 và 1080×2400 khớp mock-up đã duyệt (Bước 7).
- [ ] Từ lúc chạm tới lúc lá bắt đầu bay ≤ 1 khung hình trên máy thấp nhất mục tiêu.
