# Thuật ngữ và ID chuẩn — CardSlot

> v1.0 · Bước 1 · đã duyệt (Phat, 06-10-2026). **v2.0 nháp (CR-012 giai đoạn A, chờ duyệt):** chồng một màu, che theo màu, 8 màu, `pending`, `stuck`. Tài liệu, code, level data và key text đều dùng **ID** ở cột đầu, không dùng tên hiển thị.

## Vật thể trong level

| ID | Tên hiển thị (EN) | Định nghĩa |
|---|---|---|
| `card` | Card | Một lá bài; chỉ có một thuộc tính là màu (`color_*`). |
| `stack` | — | Một chồng lá bài **cùng một màu** nằm trên `board` (`color`, `count`), có vị trí, kích thước và `layer` (v2.0, R-1c). |
| ~~`run`~~ | — | (bỏ ở v2.0: chồng một màu, một lần `tap` gửi **cả chồng**.) |
| `board` | — | Khay bài phía dưới màn hình, chứa mọi `stack`. |
| `layer` | — | Số nguyên ≥ 0 của một `stack`; lớn hơn là nằm trên. |
| `covered` | — | Một `stack` bị che khi có `stack` **khác màu** còn lá, `layer` cao hơn, chồng lên hình chữ nhật của nó (v2.0, R-3). Chồng bị che thì không chạm được. |
| `open` | — | `stack` không bị che, còn lá: chạm được. |
| `target` | Pole | Đích ở phía trên: một màu và sức chứa `capacity` (số lá). Đủ lá thì `target` hoàn thành và biến mất. |
| `target_slot` | — | Cọc ở đáy mỗi cột — chỉ cọc này nhận lá (mặc định 3 cột, đánh số trái → phải 0..n−1). |
| `target_queue` | — | Hàng cọc, chia theo cột: `target` thứ i thuộc cột i mod `n_slots`; cọc đầy thì cọc phía sau cùng cột tiến lên (CR-009). |
| `buffer` | Waiting slots | Hàng ô chờ (ô tạm) ở giữa màn hình, chứa lá chưa có đích. Sức chứa `buffer_capacity` tính theo **số lá**. |
| `tap` | — | Một lần người chơi chạm vào một `stack` đang `open`. |
| `settle` | — | Bước tự xử lý sau mỗi lá được đặt: dọn `target` đầy, kéo `target` mới, cho lá trong `buffer` bay lên (GDD §2.4). |
| `overflow` | — | Một lá phải vào `buffer` khi `buffer` đã đầy, dẫn tới thua. |
| `pending` | — | Các lá của lần chạm còn chưa đặt được khi `overflow`; Continue / Revive / RV Slot đặt lại chúng (v2.0). |
| `stuck` | — | Thua vì kẹt: không còn `tap` hợp lệ, hoặc mọi `tap` hợp lệ đều dẫn tới `overflow` (v2.0, R-13). |

## Màu

Mỗi màu có thêm một **hoạ tiết** riêng để người mù màu vẫn phân biệt được (D-011). Mã màu cụ thể chốt ở Bước 2/3 và ghi vào `DesignTokens.cs`.

| ID | Tên hiển thị | Hoạ tiết |
|---|---|---|
| `color_0` | Red | trái tim |
| `color_1` | Blue | giọt nước |
| `color_2` | Yellow | ngôi sao |
| `color_3` | Green | lá cây |
| `color_4` | Purple | mặt trăng |
| `color_5` | Orange | hình tròn |
| `color_6` | Pink | bông hoa (v2.0) |
| `color_7` | Cyan | bông tuyết (v2.0) |

## Tiến trình, kinh tế và tính năng

| ID | Tên hiển thị | Định nghĩa |
|---|---|---|
| `coin` | Coins | Tiền mềm duy nhất trong MVP. |
| `booster_undo` | Undo | Hoàn tác `tap` gần nhất. |
| `booster_add_slot` | Extra Space | Tăng `buffer_capacity` trong level hiện tại. |
| `continue` | Continue | Sau khi thua: tăng `buffer_capacity` rồi chơi tiếp, tối đa 1 lần mỗi lượt chơi. |
| `attempt` | — | Một lượt chơi level, từ lúc bắt đầu tới Thắng, Thua (đã từ chối Continue) hoặc thoát. |
| `level_index` | Level N | Số thứ tự level, bắt đầu từ 1; hiển thị "Level N". |
