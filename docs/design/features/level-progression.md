# Spec: Home, thắng và tiến trình level

> v1.0 · Bước 1 · đã duyệt (Phat, 06-10-2026). Con số: `economy-sheet.md` §2.

**Mục đích.** Đưa người chơi vào level kế tiếp nhanh nhất có thể và thưởng cho mỗi lần thắng.

## Luồng người chơi

1. **Home (`MainScreen`):** logo, `CoinCounter`, nút Play lớn ghi "Level N" (`current_level`), nút Settings. Chạm Play → `GameplayScreen`.
2. **Thắng (R-11):**
   1. Tính thưởng: `win_reward` (+ `win_reward_hard_bonus` ở level khó).
   2. **Lưu trước**: `coins += thưởng`, `current_level = min(level + 1, 30)`, `highest_cleared = max(highest_cleared, level)`, `attempts = 0` → commit nguyên tử (G18, G19).
   3. Phát `level_won` → mở `WinDialog`: tiêu đề, xu đếm lên, nút **Next** (và **Home**).
3. Next → (có thể có interstitial, `features/ads.md`) → level tiếp theo.
4. **Level 30 thắng:** `WinDialog` ghi "More levels coming soon", nút Next đổi thành **Play again** (chơi lại Level 30, thưởng `replay_reward`).

## Trạng thái

Home: bình thường · đã hết level. WinDialog: thường · level khó · level cuối.

## Dữ liệu lưu

`current_level`, `highest_cleared`, `attempts`, `coins` (v1.1, CR-002).

## Trường hợp biên

- Tắt app trong lúc xu đang đếm lên: xu đã được lưu; mở lại thì thấy đủ số xu, không cộng lần hai.
- Lưu lỗi khi thắng: không hiện thưởng, ghi log, cho chơi lại level đó (G18).

## Key text

`home.play` ("Level {0}") · `home.settings` · `win.title` ("Level Complete!") · `win.title.hard` ("Hard Level Cleared!") · `win.next` ("Next") · `win.home` ("Home") · `win.final` ("More levels coming soon") · `win.play_again` ("Play again")

## Visual Contract

- Home có nền được trang trí theo hướng art (D-011), không phải nền trơn mặc định của rig (rule #17).
- Nút Play là phần tử nổi bật nhất Home: to nhất, màu nhấn, có nhịp "thở" nhẹ.
- WinDialog: confetti, các xu bay vào `CoinCounter`, nút Next hiện sau khi đếm xong (≤ 1.2 giây).
- Level khó có huy hiệu "Hard" ở nút Play (Home) và trên TopBar.

## Acceptance

- [ ] Test: thắng → `coins` và `current_level` được lưu trước khi dialog được yêu cầu mở.
- [ ] Test: thắng Level 30 không làm `current_level` vượt 30.
- [ ] Test: lưu lỗi → `coins`/`current_level` trong bộ nhớ về giá trị cũ.
