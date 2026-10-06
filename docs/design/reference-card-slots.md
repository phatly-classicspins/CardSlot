# Phân tích game tham khảo — Card Slots

> Nháp · 06-10-2026 · Bước 0. Tham khảo để học nhịp độ và cấu trúc, **không copy** art, âm thanh, text hay dữ liệu level (G8).

| | |
|---|---|
| Tên | Card Slots — "Tap. Sort. Fit." |
| Link | https://apps.apple.com/us/app/card-slots/id6757999544 |
| Nhà phát triển | Cigdem Baran (© BoostGames) — studio hyper-casual, nhiều game cùng khuôn "tap → sort" |
| Thể loại | Puzzle, 4+, chỉ tiếng Anh, iPhone + iPad, màn dọc |
| Phiên bản / dung lượng | 1.0.3, 242.5 MB |
| Đánh giá | chưa đủ lượt đánh giá để hiển thị — game mới, chưa có tín hiệu thị trường |

**Nguồn đã xem:** mô tả trên App Store và 4 ảnh chụp màn hình (Level 1, 2, 11, 23). **Chưa chơi thử, chưa xem video gameplay** — các điểm ghi "(suy ra)" là suy từ ảnh, cần xác minh.

## 1. Vòng chơi chính

1. Dưới cùng là một **khay bài** chứa các chồng bài nhiều màu, xếp **chồng lớp** lên nhau. Chỉ lá/chồng đang lộ ra mới chạm được.
2. Người chơi **chạm** một lá đang mở.
3. Nếu phía trên có **đích cùng màu** (cọc có vòng màu) còn chỗ, lá bay thẳng vào đích. Đích đầy thì biến mất, đích tiếp theo hiện ra (suy ra).
4. Nếu không có đích phù hợp, lá rơi vào **thanh ô tạm** ở giữa màn hình. Thanh này có số ô giới hạn.
5. Khi một đích mới cùng màu xuất hiện, các lá trong ô tạm tự bay lên (suy ra từ ảnh Level 11: thanh tạm có lá vàng/đỏ đang chờ).
6. **Thắng:** dọn hết bài. **Thua:** thanh ô tạm đầy, không còn nước đi.

Cốt lõi của độ khó là **thứ tự chạm**: nhìn trước lá nào sắp lộ ra và đích nào sắp đến, để ô tạm không bị lấp bằng lá sai màu.

## 2. Bố cục màn chơi (từ ảnh)

```
┌──────────────────────────────┐
│ (xu) 0              Level 11 │  HUD: xu, số level
│ (II)                     (↻) │  tạm dừng, chơi lại
│    ●        ●        ●       │  2–3 cọc đích, mỗi cọc một màu
│                              │
│ [▮▮▮▮▮▮▮▮▮▮░░░░░░░░░░░░░░░]  │  thanh ô tạm (lá xếp dọc, sát nhau)
│ ┌──────────────────────────┐ │
│ │  ▣ ▣      ▣ ▣            │ │  khay bài: chồng bài theo lưới,
│ │  ▣ ▣      ▣ ▣            │ │  có lớp che lớp
│ └──────────────────────────┘ │
└──────────────────────────────┘
```

- Level 1: 2 màu, 4 chồng, không có lớp che — dạy thao tác chạm.
- Level 2: 3 màu, lưới 2×3.
- Level 11–23: thêm lớp che, chồng xòe xéo, bố cục không còn là lưới đều.

## 3. Tính năng nhìn thấy

| Tính năng | Có? |
|---|---|
| Điều khiển một chạm | có |
| Level tăng dần độ khó | có (≥ 25 level) |
| Xu (hiển thị ở HUD) | có, cách kiếm/tiêu chưa rõ |
| Tạm dừng, chơi lại | có |
| Booster (undo, thêm ô, xáo bài) | chưa thấy trong ảnh |
| Bản đồ level, meta, sự kiện | chưa thấy |

## 4. Nhịp độ

- Một level ngắn, khoảng 30–90 giây (suy ra từ số lá trên màn).
- Hai level đầu gần như không thể thua: dạy luật bằng cách cho chơi, không có hướng dẫn chữ dài.
- Độ khó đến từ ba nút vặn: **số màu**, **độ sâu lớp che**, **số ô tạm**.

## 5. Kiếm tiền

- App Privacy khai báo "Data Used to Track You: Usage Data" ⇒ gần như chắc chắn có **quảng cáo** (interstitial giữa level, có thể có rewarded).
- Không thấy IAP trong thông tin trang.

## 6. Bài học cho CardSlot

- **Lấy:** vòng chơi một chạm; ô tạm có giới hạn làm nguồn căng thẳng; độ khó qua số màu, lớp che và số ô.
- **Làm khác (đề xuất):** booster rõ ràng (thêm ô tạm qua rewarded ad, undo) để có điểm kiếm tiền; level kiểm tra được là giải được (solver headless); art riêng.
- **Rủi ro:** tên "CardSlot" gần như trùng "Card Slots". Xem `concept.md` §7.
