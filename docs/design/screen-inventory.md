# Danh sách màn hình — CardSlot

> v1.0 · Bước 1 · đã duyệt (Phat, 06-10-2026). Loại theo framework: **Screen** là một đích điều hướng toàn màn hình (`game.scenes.json`), **Dialog** là popup (`game.dialogs.json`), **Widget** là phần tử nằm trong màn hình (`game.widgets.json`). Mọi màn đều được tạo bằng manifest + `Scaffold.Sync` (rule #13).

## Screen

| ID | Đi đến bằng | Trạng thái | Spec |
|---|---|---|---|
| `MainScreen` (Home) | sau boot; Home từ Pause/Win/Lose | bình thường · đã hết 30 level (nút Play ghi "Level 30") | `features/level-progression.md` |
| `GameplayScreen` | Play, Next, Retry, Restart | đang chơi · đang có FTUE · đang hoạt ảnh kết thúc | `features/core-gameplay.md` |

## Dialog

| ID | Mở khi | Trạng thái | Spec |
|---|---|---|---|
| `WinDialog` | R-11 | bình thường · level khó (thêm thưởng) · level cuối ("More levels coming soon") | `features/level-progression.md` |
| `LoseDialog` | R-12 | `offer` (Continue bằng xu / ad) · `offer` không đủ xu (nút xu bị khoá) · `failed` (Retry / Home) | `features/continue.md` |
| `PauseDialog` | nút II trên HUD; app về nền giữa level | bình thường | `features/pause-settings.md` |
| `SettingsDialog` | nút trong Pause hoặc Home | bình thường | `features/pause-settings.md` |
| `BoosterBuyDialog` | chạm booster khi số lượng = 0 | đủ xu · không đủ xu (chỉ còn nút ad) | `features/boosters.md` |
| `BoosterUnlockDialog` | đầu Level 4 và Level 7 | Undo · Extra Space | `features/ftue.md` |

## Widget

| ID | Nằm trong | Trạng thái |
|---|---|---|
| `TopBar` | Gameplay (lớp `Ui`) | xu · "Level N" · nút II · nút ↻ |
| `CoinCounter` | Home, TopBar, Win | bình thường · đang cộng (đếm lên) |
| `BoosterButton` | Gameplay (dưới cùng) | khoá (chưa mở) · có số lượng · hết (hiện giá) · không dùng được (ví dụ Undo khi chưa có tap) |
| `BufferBar` | Gameplay | bình thường · sắp đầy (≤ 2 chỗ, nhấp nháy đỏ) · đầy |
| `TargetSlot` | Gameplay | trống · có đích · đang hoàn thành |
| `TutorialHand` | Gameplay (lớp `Overlay`) | ẩn · đang chỉ |

## Nơi đặt (rule #16)

| Đối tượng | Bám vào | Lớp |
|---|---|---|
| TopBar, BoosterButton, HUD | mép màn hình | uGUI dưới `GetHost(Ui)` |
| Dialog | mép/giữa màn hình | `Popup` |
| `stack`, `card`, `target`, `buffer` | thế giới (bàn chơi) | renderer dưới `WorldRoot` |
| `TutorialHand` | vật thể trong thế giới | quyết định ở Bước 2 |
