# GDD "Card Slots" của anh Tánh — bản chụp

> Nguồn: https://hvtanh07.github.io/Card-Slot-GDD/ (tác giả hvtanh07) · đọc ngày 08-10-2026.
> Đây là **bản tóm tắt để đối chiếu**, không phải nguồn đã duyệt. Trang gốc có thể đổi; khi đổi thì đọc lại và ghi ngày mới.
> Dùng cho CR-012 (`docs/workflow/DECISIONS.md`).

## 1. Cơ chế

**Bố cục:** hàng đợi cọc (mỗi cột một hàng, mỗi cọc một màu) · cọc ở đáy hàng (chỉ cọc này nhận lá) · ô chờ · chồng mở · chồng bị che. Số cột, số cọc, số ô chờ, số chồng trong hình chỉ là ví dụ.

**Luật**
- Mỗi lá một màu. **Lá cùng màu nằm chung một chồng**; chồng có nhiều kích thước.
- Chồng nằm chồng lên nhau trên bàn. **Một chồng che mọi chồng khác màu nằm dưới nó.** Chỉ chạm được chồng không bị che.
- Chạm chồng → lá bay lên. Có cọc cùng màu ở đáy hàng thì bay vào cọc; không thì vào ô chờ.
- Lá trong ô chờ tự bay lên cọc cùng màu ngay khi có.
- Cọc xuống đáy hàng thì nhận lá; cọc đầy thì biến mất.
- **Thắng:** mọi chồng đã lên cọc cùng màu và mọi cọc đã được xoá.
- **Thua:** mọi ô chờ đã đầy, không lá nào trong ô chờ khớp cọc đang nhận, và không chồng mở nào khớp cọc đang nhận.
- **Revive:** dùng miễn phí 2 lần Remove lên các cọc ở đáy hàng có ít lá nhất; còn kẹt thì dùng tiếp tới khi chơi được. Không trừ Remove của người chơi.

**Tim:** tối đa 5 · hồi 1 tim / 20 phút khi dưới 5 · đủ 5 thì dừng đếm. Mất tim khi: thua rồi chơi lại · tự restart · thoát về Home.

**Thanh tiến độ mở element** (màn Victory): thanh ngang tô xanh, giữa ghi `{current}/{total}`, ô vuông bên phải hiện element sắp mở.
- Không còn element nào để mở → ẩn cả cụm, căn giữa phần còn lại.
- `total = level mở kế tiếp − level mở trước đó` · `current = level vừa xong − level mở trước đó + 1`.
- Ví dụ: mở ở 5 và 10 → sau level 5 hiện 1/5, …, sau level 9 hiện 5/5.

**RV Slot:** mỗi level có **2** lần, mỗi lần xem quảng cáo thưởng được **+8** ô chờ.

## 2. Element

| Element | Mở ở level | Luật |
|---|---|---|
| Hidden Card Stack | 7 | Giấu màu khi bị che; hết bị che thì lộ màu. |
| Hidden Color Pole | 12 | Giấu màu khi chưa ở đáy hàng; xuống đáy thì lộ màu. |
| Locked Card Stack | 17 | Hiện màu nhưng không chạm được. Có bộ đếm = số cọc phải xoá. Chỉ đếm sau khi chồng hết bị che; cọc xoá trước đó không tính. Mỗi cọc xoá −1. Nếu chỉ còn các cọc của lá bị khoá thì mở ngay. Mở rồi thì như chồng thường. |

## 3. Booster (mỗi loại tặng 3)

| Booster | Mở ở level | Luật |
|---|---|---|
| Hand | 5 | Chọn bất kỳ chồng nào và gửi lên, kể cả chồng bị che. Chọn được Hidden và Locked; Locked được chọn thì mở ngay. |
| Shuffle | 8 | 1) Lấy tối đa 3 màu từ lá trong ô chờ; thiếu thì lấy thêm từ chồng mở. 2) Đưa 3 cọc của các màu đó xuống đáy hàng (ưu tiên cọc thường rồi mới tới Hidden Color Pole). 3) Xáo ngẫu nhiên các cọc còn lại. |
| Remove | 10 | Chọn bất kỳ cọc nào trong hàng và xoá ngay. Cọc hút lá cùng màu trên bàn theo thứ tự: chồng thường không bị che → chồng bị che → Hidden Card Stack → Locked Card Stack (bị hút thì mở ngay). |

## 4. FTUE

- Input masking (lớp mờ chặn chạm ngoài vùng đích, HUD vẫn dùng được) · máy trạng thái từng bước · khung highlight · lưu tiến độ theo bước.
- Các bước ở level 1: vào level → đồng ý Terms & Conditions / Privacy Policy → chạm chồng 1, 2, 3 (highlight, chữ "Tap to send cards") → thắng level (**lưu**).
- Popup element: Hidden Card Stack "Unblock the stack to reveal its color." (7) · Hidden Color Pole "Move to the bottom to reveal its color." (12) · Locked Card Stack "Send cards up to unlock the stack." (17).
- Popup booster: Hand "Pick any stack of cards!" (5) · Shuffle "Shuffle the poles in queues!" (8) · Remove "Tap the pole to sort it instantly!" (10).
- Thứ tự mở: L1 FTUE · L5 Hand · L7 Hidden Card · L8 Shuffle · L10 Remove · L12 Hidden Pole · L17 Locked Card.

## 5. Vật thể và animation (đang viết dở)

- **8 màu** chung cho lá và cọc: Red, Blue, Green, Yellow, Pink, Orange, Purple, Cyan.
- Lá: bay từ bàn lên cọc · bay từ bàn vào ô chờ. Cọc: tiến xuống trong hàng · đầy rồi biến mất.
- Hidden Card / Hidden Pole: khói che lúc đổi sang dạng thường. Locked: băng vỡ khi mở.
- Shuffle: xáo cọc trong hàng. Remove: cọc bay về phía bàn rồi hút lá cùng màu.
- Concept (3.1): chưa viết.

## 6. SFX và rung

| Tên | Nhóm | Khi nào | Rung |
|---|---|---|---|
| BGM In Game | UI | Bắt đầu chơi | – |
| BGM Home | UI | Đang ở Home | – |
| Win Level | UI | Thắng | Mạnh |
| Lose Level | UI | Kẹt tạm thời | Mạnh |
| Tap | UI | Chạm UI | Nhẹ |
| Coin Reward | UI | Nhận xu | – |
| Booster Reward | UI | Nhận booster | – |
| Card Selected | Lõi | Chạm chồng | Vừa |
| Card Move to column | Lõi | Lá trượt vào cọc | – |
| Pole Clear | Lõi | Cọc đầy và biến mất | Vừa |
| Hidden Pole/Card Stack | Element | Lộ màu | – |
| Locked Card Stack Unlock | Element | Mở khoá | – |
| Shuffle | Booster | Xáo cọc | – |

## 7. Remote config (đang viết dở, chưa có giá trị)

- Thưởng: xu mỗi level · hệ số quảng cáo nhân thưởng (2×, 3×…) · số booster mỗi lần mua và giá xu, riêng từng booster. Danh sách booster bán: **Paper Box, Magnet, Remove** — trang gốc tự ghi là lệch với Hand / Shuffle / Remove, cần xác nhận.
- Quảng cáo: tối đa mỗi ngày cho nhân thưởng / revive / booster (0 = không giới hạn).
- Revive: giá lần đầu · hệ số tăng giá mỗi lần sau · số revive tối đa mỗi level (0 = không giới hạn).

## 8. Level config (chưa viết)

Nội dung dự kiến · định dạng JSON · hướng dẫn tool dựng level.

## Những điều GDD chưa nói

Sức chứa cọc · số cột · số ô chờ gốc · số level · số lá tối đa mỗi lần chạm · chuyện gì xảy ra khi một lần chạm có nhiều lá hơn số ô chờ còn trống · revive trả bằng gì (xu hay quảng cáo) · giá trị remote config.
