# Design token — CardSlot

> v1.0 · Bước 2 · đã duyệt cùng mock-up (D-018). Hướng **B · Warm Toybox** (D-017).
> Nguồn: `_bmad-output/planning-artifacts/ux-designs/ux-cardslot/.working/screens.html`. Ở Bước 6, bảng này được chép thành **một** file `Assets/CardSlot/Views/DesignTokens.cs` (rule #17, G21). View không được viết màu cứng.
> Đơn vị: px tham chiếu = đơn vị thế giới (1 px = 1 unit trên rig), khung 1080×1920, safe rect x ±540 · y ±960.

## 1. Màu nền và bề mặt

| Token | Hex | Dùng cho |
|---|---|---|
| `GroundTop` / `GroundBottom` | `#FFF6E6` / `#FFD9A8` | nền màn hình, gradient tròn từ trên xuống |
| `Surface` | `#FFF8EC` | pill HUD, panel dialog, nút booster |
| `SurfaceSunken` | `#F2C68C` | ô tạm trống, hàng công tắc, ô chứa icon |
| `TrayRim` | `#B9733A` | viền khay bài |
| `TrayInner` | `#F6CF98` | lòng khay bài |
| `Rail` | `#C98B52` | thanh ô tạm |
| `Ink` | `#5A3415` | chữ chính |
| `InkSoft` | `#9A6A3F` | chữ phụ, nhãn |
| `Primary` / `PrimaryEdge` | `#21B8A6` / `#138A7B` | nút chính (Play, Next, Continue free, Got it), huy hiệu số |
| `Secondary` / `SecondaryEdge` | `#E8613C` / `#B7411F` | nút phụ, nút tròn HUD, ribbon dialog |
| `Danger` | `#E2604A` | ô tạm sắp đầy, ribbon thua |
| `Disabled` / `DisabledEdge` | `#CDBFAE` / `#A8977F` | nút bị khoá |
| `Scrim` | `rgba(70,35,10,.55)` | lớp mờ sau dialog |
| `TutorialScrim` | `rgba(70,35,10,.45)` | lớp mờ FTUE |

## 2. Màu lá bài (`color_0…5`, dùng chung cho mọi màn)

| ID | Mặt | Cạnh (đáy 3D) | Hoạ tiết |
|---|---|---|---|
| `color_0` | `#F2475B` | `#B92639` | trái tim |
| `color_1` | `#3D8BFF` | `#1F5FCC` | giọt nước |
| `color_2` | `#FFC93C` | `#D99A0B` | ngôi sao |
| `color_3` | `#3CC26B` | `#22924A` | lá cây |
| `color_4` | `#9B5CF6` | `#6C35C4` | mặt trăng |
| `color_5` | `#FF8A3D` | `#D35F14` | hình tròn |

- **Lá bị che:** độ sáng 68% + sọc chéo 135° (đen 10%) — đúng Visual Contract "≤ 70%", và vẫn nhận ra được màu.
- Mặt lá: gradient dọc, sáng hơn 30% ở đỉnh → màu gốc ở 38%. Viền trong trắng 55%.

## 3. Chữ

Font: **Fredoka** (SIL OFL 1.1, Google Fonts — G9: license cho phép nhúng vào app). Bước 6 tạo TMP font asset.

| Token | Cỡ (px) | Độ đậm | Dùng cho |
|---|---|---|---|
| `Logo` | 150 | 700, viền 6 px `SecondaryEdge` | logo Home |
| `Display` | 84 | 700 | nút Play ở Home |
| `Title` | 76 (tự thu nhỏ: `min(76, 1500 / số ký tự)`) | 700 | ribbon dialog |
| `ButtonL` | 64–72 | 700 | nút chính trong dialog |
| `Heading` | 54–60 | 600 | dòng phụ dialog ("Level 25"), tên booster |
| `Body` | 46–50 | 500–600 | mô tả, hàng cài đặt, bong bóng FTUE |
| `Hud` | 44 | 700 | số xu, "Level N" |
| `Label` | 28–30 | 600 | nhãn khu vực, tên booster dưới nút |

## 4. Khoảng cách, bo góc, độ nổi

- **Lưới 2 px, nhịp 20 px.** Lề màn hình 40 px. Khoảng giữa hai vùng gameplay 50 px.
- **Bo góc:** lá bài 24 · ô tạm 16 · khay 48 (lòng 34) · ô đích 40 · panel 64 · nút HUD 32 · pill = nửa chiều cao.
- **Độ nổi 3D-look:** mọi khối có "cạnh đáy" đặc cùng hue tối hơn (lá 10 px, nút 12–14 px, panel 18 px) + bóng mềm `0 16–40px` đen 10–30%. Nút có vệt sáng trắng 28% ở 1/3 trên.

## 5. Bố cục Gameplay (khung 1080×1920)

| Vùng | y (từ đỉnh) | Cao |
|---|---|---|
| TopBar | 66 | 104 |
| Hàng đích (3 ô, rộng 300, cách 40) | 200 | 400 |
| Nhãn ô tạm | 610 | — |
| Thanh ô tạm | 650 | 200 |
| Khay bài | 900 | 650 (+ toàn bộ phần cao thêm ở máy 9:20) |
| Booster (đáy) | cách đáy 110 | 170 |

Máy 9:20 (1080×2400): 480 px dư đều dồn cho khay bài; TopBar, đích, ô tạm giữ nguyên toạ độ.
