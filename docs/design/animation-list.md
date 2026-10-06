# Danh sách animation và hiệu ứng — CardSlot

> v1.0 · Bước 4 · đã duyệt (Phat, 06-10-2026, D-022). Phạm vi MVP: P0 + P1 (D-021). Xem thử: `art/motion/preview.html`. Đầu vào: GDD v1.1 (Visual Contract trong `features/*.md`), art v1, mock-up v01.
> **Tên ở cột đầu là API**: code gọi đúng tên này; đổi tên phải qua yêu cầu thay đổi (quy trình Bước 4).

## Nguyên tắc

- **Gameplay không chờ animation.** Model xử lý `tap` tức thì (R-8); view phát lại kết quả. Không có logic nào nằm trong callback của animation.
- **Mọi animation có pose cuối.** Khi cờ config `motion.reduced` bật (D-021: chưa có công tắc trong Settings), animation nhảy thẳng tới pose cuối trong 0 giây (cột "Pose cuối").
- **Thời lượng và easing là token** (`DesignTokens.Motion.*`), không viết số trong View.
- **Cách làm:** T = tween bằng code (LitMotion, đã có trong framework) · P = ParticleSystem prefab qua `EffectService` của framework (tinh chỉnh qua Unity-MCP, rule #12) · F = transition có sẵn của framework.

## Gameplay

| Tên (API) | Thời lượng | Lặp | Kích hoạt | Pose cuối | Cách | Ưu tiên |
|---|---|---|---|---|---|---|
| `card_fly_to_target` | 0.32 s/lá, các lá cách nhau 0.05 s | 1 lần | lá vào `target` (R-7.1) | lá nằm trên đích, xếp chồng | T: đường cong bezier đỉnh 120 px, xoay ±8° → 0, `OutQuad` | P0 |
| `card_fly_to_buffer` | 0.28 s | 1 lần | lá vào `buffer` (R-7.2) | `card_mini` trong ô | T: bay + co dần, đổi sprite ở giữa đường | P0 |
| `buffer_release` | 0.32 s/lá, cách nhau 0.05 s | 1 lần | lá từ `buffer` lên đích (R-10) | lá trên đích; các lá còn lại dồn trái | T | P0 |
| `buffer_compact` | 0.15 s | 1 lần | sau `buffer_release` | ô liền nhau | T | P0 |
| `target_land_bump` | 0.12 s | 1 lần | mỗi lá chạm đích | bệ ở tỉ lệ 1 | T: bệ nén y 0.9 → 1 | P1 |
| `target_complete` | 0.35 s | 1 lần | `target` đầy (R-9) | ô đích trống | T (nảy 1 → 1.15 → 0) + P (16 hạt cùng màu, vòng sáng) | P0 |
| `target_enter` | 0.25 s | 1 lần | đích mới vào ô (R-9) | đích ở vị trí | T: trượt từ trên xuống 120 px + hiện dần, `OutBack` | P0 |
| `next_chip_shift` | 0.2 s | 1 lần | hàng đợi dịch | chip ở vị trí mới | T | P2 |
| `stack_reveal` | 0.18 s | 1 lần | chồng hết bị che (R-5) | sáng 100%, không sọc | T: tint 0.68 → 1, sọc mờ dần | P0 |
| `stack_press` | 0.08 s | 1 lần | ngón tay chạm chồng | tỉ lệ 1 | T: 0.94 → 1 | P1 |
| `stack_shake_invalid` | 0.25 s | 1 lần | chạm chồng bị che (R-4) | vị trí gốc | T: lắc ngang ±10 px × 3 | P1 |
| `level_intro` | 0.5 s (chồng rơi xuống, cách nhau 0.03 s) | 1 lần | vào level | bàn đầy đủ | T | P2 |
| `buffer_warn_pulse` | chu kỳ 0.8 s | lặp | còn ≤ 2 chỗ | màu cảnh báo đứng yên | T: thanh nhấp nháy đỏ | P0 |
| `buffer_overflow_bounce` | 0.4 s | 1 lần | `overflow` (R-12) | lá nằm trên thanh | T: lá nảy bật khỏi thanh | P1 |
| `buffer_expand` | 0.35 s | 1 lần | Extra Space / Continue (R-17, R-18) | thanh dài thêm, ô mới có sẵn | T: thanh dài ra + ô mới loé sáng | P0 |
| `undo_rewind` | 0.25 s | 1 lần | Undo (R-16) | lá về chồng cũ | T: chạy ngược đường bay | P0 |

## UI

| Tên (API) | Thời lượng | Lặp | Kích hoạt | Pose cuối | Cách | Ưu tiên |
|---|---|---|---|---|---|---|
| `dialog_show` / `dialog_hide` | 0.2 s | 1 lần | mở/đóng mọi dialog | panel ở tỉ lệ 1 / ẩn | F: `ScaleTransition(0.9, 0.2, OutBack)` + `FadeTransition` cho lớp mờ | P0 |
| `button_press` | 0.08 s nhấn, 0.15 s nhả | 1 lần | nhấn mọi nút | tỉ lệ 1 | T: 0.95 → 1 `OutBack` | P1 |
| `coin_count_up` | ≤ 1.2 s | 1 lần | `WinDialog` hiện, Continue/booster trả xu | số cuối | T: số chạy + 6 xu bay vào `CoinCounter` | P0 |
| `win_confetti` | 1.2 s | 1 lần | `WinDialog` hiện | không có hạt | P (40 hạt, `confetti` tô 6 màu) | P1 |
| `play_breathe` | chu kỳ 1.6 s | lặp | Home | tỉ lệ 1 | T: 1 → 1.04 | P2 |
| `tutorial_hand_tap` | chu kỳ 1.0 s | lặp | bước FTUE có bàn tay | bàn tay đứng yên, chỉ vào chồng | T: tay nhấn xuống + vòng `ftue_ring` lan rộng, mờ dần | P0 |
| `badge_pop` | 0.2 s | 1 lần | số booster đổi | tỉ lệ 1 | T: 1.3 → 1 | P2 |
| `unlock_icon_pop` | 0.4 s | 1 lần | `BoosterUnlockDialog` | icon và quà ở vị trí | T: icon 0 → 1 `OutBack`, quà nảy sau 0.15 s | P1 |
| `delayed_reveal` | 0.25 s, trễ 1 s | 1 lần | nút "No thanks" | nút hiện đủ | T: hiện dần | P0 |

**P0** = phải có trong MVP · **P1** = nên có · **P2** = có thời gian thì làm.

## Âm thanh (nhánh Âm thanh, theo GDD §9)

| ID | Gắn với animation | Ghi chú |
|---|---|---|
| `sfx_tap` | `stack_press` | |
| `sfx_tap_invalid` | `stack_shake_invalid` | |
| `sfx_card_land_target` | cuối `card_fly_to_target` / `buffer_release` | cao độ tăng dần +1 bán cung mỗi lá trong một `run` |
| `sfx_card_land_buffer` | cuối `card_fly_to_buffer` | |
| `sfx_target_complete` | `target_complete` | |
| `sfx_buffer_warning` | lần đầu vào `buffer_warn_pulse` | phát một lần, không lặp |
| `sfx_win` / `sfx_lose` | `WinDialog` / `buffer_overflow_bounce` | |
| `sfx_coin` | `coin_count_up` | |
| `sfx_booster` | `buffer_expand`, `undo_rewind` | |
| `sfx_ui_click` | `button_press` | |
| `music_home` / `music_gameplay` | — | lặp, âm lượng nền |

## Giới hạn hiệu năng (đề xuất)

- ≤ 60 tween chạy cùng lúc; ≤ 200 hạt sống cùng lúc. Mỗi atlas chỉ 1 draw call.
- Mọi tween và hạt chạy qua pause gate (rule #15): Pause là đứng hình.
- Âm thanh: OGG, mono cho SFX; chuẩn hoá −16 LUFS cho nhạc, −12 LUFS cho SFX; tổng dung lượng audio ≤ 5 MB.

## Hạt (ParticleSystem, tạo prefab ở Bước 6 qua Unity-MCP — rule #12)

| Prefab | Dùng cho | Thông số |
|---|---|---|
| `vfx_target_burst` | `target_complete` | 1 lần phát 16 hạt (`burstCount`); sprite tròn 18 px bo 6; màu = màu lá của đích; tốc độ 240–320 px/s toả tròn; sống 0.5 s; co 1 → 0.3 và mờ dần; kèm `ftue_ring` phóng 0.3 → 1.4 trong 0.4 s |
| `vfx_win_confetti` | `win_confetti` | 1 lần phát 40 hạt (`confettiCount`); sprite `confetti` tô 6 màu lá; bắn lên 300–560 px rồi rơi (trọng lực −981, ruler ×100), xoay 540–1200°; sống 1.2 s; nằm dưới panel dialog, trên lớp mờ |

## Token chuyển động

Nguồn: hằng `MOTION` trong `art/motion/preview.html`. Bước 6 chép thành `DesignTokens.Motion` (giây). Easing dùng tên của LitMotion: `OutQuad`, `OutBack`, `OutCubic`, `InQuad`, `InOutSine`.

| Token | Giá trị | Token | Giá trị |
|---|---|---|---|
| `fly` | 0.32 | `flyStagger` | 0.05 |
| `flyArc` (px) | 120 | `flyTilt` (°) | 8 |
| `toBuffer` | 0.28 | `release` | 0.32 |
| `compact` | 0.15 | `landBump` / `landBumpScale` | 0.12 / 0.9 |
| `complete` / `completeOvershoot` | 0.35 / 1.15 | `enter` / `enterOffset` (px) | 0.25 / 120 |
| `reveal` | 0.18 | `press` / `pressScale` | 0.08 / 0.94 |
| `shake` / `shakeAmp` (px) | 0.25 / 10 | `warnPeriod` | 0.8 |
| `overflow` | 0.4 | `expand` | 0.35 |
| `rewind` | 0.25 | `dialog` / `dialogFrom` | 0.2 / 0.9 |
| `btnPress` / `btnRelease` / `btnScale` | 0.08 / 0.15 / 0.95 | `countUp` | 1.2 |
| `confetti` | 1.2 | `handPeriod` | 1.0 |
| `unlockPop` / `unlockGiftDelay` | 0.4 / 0.15 | `revealDelay` / `revealDur` | 1.0 / 0.25 |
