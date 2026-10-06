# Mock-up — CardSlot

Hướng **B · Warm Toybox** (D-017). Làm bằng HTML/CSS → PNG qua Edge headless (D-016). Đây là **bố cục và design token**, chưa phải art cuối. Lá bài, đích và khay 3D-look thật làm ở Bước 3.

- **Nguồn (thay cho prompt):** `_bmad-output/planning-artifacts/ux-designs/ux-cardslot/.working/screens.html`. Render: `.working/render.ps1 -Page screens.html -OutDir full -Jobs "b:<screen>:<1920|2400>"`.
- **Ảnh so sánh 3 hướng:** `.working/directions-sheet.png`.
- **Token:** `docs/design/design-tokens.md`.
- **Quy ước:** `<screen>[-<state>][-9x20]-vNN.png`, khung 1080×1920 (9:16) hoặc 1080×2400 (9:20). Số liệu dùng giá trị lớn nhất có thể (xu 9,999; Level 30; text dài nhất).

## Biên bản duyệt

| Mock-up | Màn / trạng thái (`screen-inventory.md`) | Phiên bản | Trạng thái |
|---|---|---|---|
| `gameplay-v01` | GameplayScreen · đang chơi · Undo có số lượng · Extra Space hiện giá | v01 | đã duyệt (Phat, 06-10-2026) |
| `gameplay-9x20-v01` | GameplayScreen · máy dài | v01 | đã duyệt (Phat, 06-10-2026) |
| `gameplay-warning-v01` | BufferBar sắp đầy · Undo hết (0) · Extra Space không dùng được | v01 | đã duyệt (Phat, 06-10-2026) |
| `gameplay-ftue-l1-v01` | FTUE `ftue.l1.step1` · 2 đích · chưa có booster | v01 | đã duyệt (Phat, 06-10-2026) |
| `home-v01` | MainScreen · bình thường | v01 | đã duyệt (Phat, 06-10-2026) |
| `home-final-v01` | MainScreen · đã hết 30 level | v01 | đã duyệt (Phat, 06-10-2026) |
| `home-9x20-v01` | MainScreen · máy dài | v01 | đã duyệt (Phat, 06-10-2026) |
| `win-v01` | WinDialog · bình thường | v01 | đã duyệt (Phat, 06-10-2026) |
| `win-hard-v01` | WinDialog · level khó | v01 | đã duyệt (Phat, 06-10-2026) |
| `win-final-v01` | WinDialog · level cuối | v01 | đã duyệt (Phat, 06-10-2026) |
| `lose-offer-v01` | LoseDialog · offer | v01 | đã duyệt (Phat, 06-10-2026) |
| `lose-offer-poor-v01` | LoseDialog · offer, không đủ xu | v01 | đã duyệt (Phat, 06-10-2026) |
| `lose-failed-v01` | LoseDialog · failed | v01 | đã duyệt (Phat, 06-10-2026) |
| `pause-v01` | PauseDialog | v01 | đã duyệt (Phat, 06-10-2026) |
| `settings-v01` | SettingsDialog (Music đang tắt) | v01 | đã duyệt (Phat, 06-10-2026) |
| `booster-buy-v01` | BoosterBuyDialog · đủ xu | v01 | đã duyệt (Phat, 06-10-2026) |
| `booster-buy-poor-v01` | BoosterBuyDialog · không đủ xu · quảng cáo không sẵn sàng | v01 | đã duyệt (Phat, 06-10-2026) |
| `unlock-undo-v01` | BoosterUnlockDialog · Undo | v01 | đã duyệt (Phat, 06-10-2026) |
| `unlock-space-v01` | BoosterUnlockDialog · Extra Space | v01 | đã duyệt (Phat, 06-10-2026) |
| `restart-confirm-v01` | Xác nhận chơi lại (nút ↻) | v01 | đã duyệt (Phat, 06-10-2026) |

**Chưa có mock-up riêng (dựng từ spec):** `CoinCounter` đang đếm lên và hoạt ảnh lá bay / đích nổ. Đây là chuyển động, để Bước 4 lo.
