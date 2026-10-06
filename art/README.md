# Art — CardSlot

> Bước 3 · v1 · đã duyệt (Phat, 06-10-2026, D-020). Quyết định: D-019 (SVG do AI dựng, 1x, atlas ≤ 2048, ASTC 6×6, ≤ 16 MB texture).
> Đầu vào: 20 mock-up v01 (`docs/mockups/`), `docs/design/design-tokens.md` v1.0.

## Cấu trúc

| Đường dẫn | Là gì | Sửa tay? |
|---|---|---|
| `source/art.html` | **File nguồn duy nhất.** Định nghĩa mọi sprite bằng SVG dựng từ token. | có |
| `source/svg/<id>.svg` | SVG nhiều layer của từng sprite, mỗi `<g id>` là một layer | không, sinh lại (G12) |
| `tools/build-art.ps1` | Build: dựng → chụp → cắt → tự sửa → kiểm tra → báo cáo | có |
| `export/<atlas>/<id>.png` | Sprite xuất 1x, đã bù màu alpha | không |
| `export/manifest.json` | Kích thước, pivot, 9-slice, atlas, loại, kết quả kiểm tra; Bước 6 đọc file này để import | không |
| `export/report.md` | **Báo cáo xuất** (cổng Bước 3) | không |
| `export/preview.png` | Toàn bộ sprite trên nền caro để duyệt bằng mắt | không |

Chạy lại sau mỗi lần sửa `art.html` hoặc token:

```bash
powershell -NoProfile -ExecutionPolicy Bypass -File art/tools/build-art.ps1
```

Mã thoát: `0` không có lỗi chặn · `1` có lỗi chặn (xem `report.md`) · `2` lỗi công cụ. Cần Microsoft Edge (có sẵn trên Windows), không cần cài thêm gì.

## Quy ước đặt tên

- **Sprite:** `<phần tử>[_<biến thể>][_<k>]`, chữ thường, gạch dưới. `k` là chỉ số màu `color_k` trong `glossary.md`. Ví dụ `card_face_3`, `buffer_rail_warn`, `btn_primary`, `icon_pause`.
- **Layer** (group trong SVG): `edge` (cạnh đáy 3D) · `face` (mặt) · `gloss` (vệt bóng) · `frame` (viền trong) · `emblem` / `emblem-shadow` · `base` / `inner-shadow` (bề mặt lõm) · `glyph` (icon).
- **Atlas:** `gameplay` (lá, đích, khay, ô tạm, bóng) · `ui` (nút, panel, pill, icon, FTUE) · `bg` (nền).

## Quy tắc

- **Không nướng chữ vào ảnh.** Nhãn nút, tiêu đề, số đếm, logo "CardSlot" đều là text sống (TMP) trong game, vì tên thương mại còn chưa chốt (D-012).
- **Icon trắng, tô màu trong game** bằng token (`icon_*`, `confetti`).
- **Trạng thái lá bị che** = sprite lá + tint độ sáng 68% + lớp phủ `card_covered_hatch` (design-tokens §2). Không có sprite riêng cho từng màu bị che.
- **Bóng mềm** là sprite riêng `shadow_soft` (9-slice) đặt dưới vật thể, không nướng vào sprite. Nhờ vậy sprite đục không có viền tối bán trong suốt.
- **Pivot** mặc định giữa (0.5, 0.5). **9-slice** ghi trong `manifest.json` theo thứ tự L, T, R, B (px).
- Import ở Bước 6: PPU 1, `alphaIsTransparency`, không mipmap cho UI, nén ASTC 6×6, đóng atlas theo cột `atlas`.

## Chưa có trong bộ này (cố ý)

- Lớp mờ FTUE có lỗ khoét quanh chồng được chỉ: làm ở Bước 6 bằng mask/4 hình chữ nhật, không cần sprite.
- Màu lớp mờ dialog: tô màu đặc từ token `Scrim`.
- Hạt hiệu ứng (đích nổ, confetti chuyển động): Bước 4.
