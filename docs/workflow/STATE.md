# Trạng thái project
Game: CardSlot (tên nội bộ, D-012) — puzzle chạm-để-xếp lá bài | Nền tảng: Android + iOS, màn dọc | Engine/framework: Unity 6000.3.13f1 (URP) + PrototypeFramework 0.4.2 | Độ phân giải chuẩn: 1080×1920
Người duyệt: Thiết kế Phat · Hình ảnh Phat · Art Phat · Kỹ thuật Phat · Phát hành Phat (D-013)

| Bước | Trạng thái | Đầu ra đã duyệt (phiên bản) | Người duyệt, ngày |
|---|---|---|---|
| 0 Khởi động | xong | concept.md v0.2, reference-card-slots.md | Phat, 06-10-2026 |
| 1 GDD | xong | GDD.md v1.0 + glossary, level-design, economy-sheet, screen-inventory, features/ (v1.0) | Phat, 06-10-2026 |
| 2 Mock-up | xong | 20 mock-up v01 (`docs/mockups/`), design-tokens v1.0 · GDD v1.1 (CR-001) | Phat, 06-10-2026 | |
| 3 Art | xong | art v1: 71 sprite (`art/export/`), báo cáo xuất 0 lỗi chặn | Phat, 06-10-2026 | |
| 4 Animation | đang làm | | |
| 5 Logic | chưa bắt đầu | | |
| 6 Ráp | chưa bắt đầu | | |
| 7 AI test | chưa bắt đầu | | |
| 8 Máy thật/Chơi thử/Phát hành | chưa bắt đầu | | |

## Câu hỏi đang chờ người làm
- [x] Game tham khảo: Card Slots (App Store) — D-009
- [x] Người duyệt: Phat duyệt tất cả — D-013
- [ ] Hạn chót, ngân sách (sinh ảnh AI, tool, license), mục tiêu hiệu năng (máy thấp nhất, FPS, dung lượng build)
- [x] concept.md v0.2 đã duyệt (Phat, 06-10-2026)
- [x] GDD v1.0 đã duyệt kèm 6 mục [XÁC NHẬN] — D-015
- [x] Kinh tế + đường cong độ khó đã duyệt — D-015
- [x] 20 mock-up v01 + design token đã duyệt — D-018
- [x] CR-001 chọn a: giới hạn tiêu đề 26 ký tự + key `gameplay.restart.note` — GDD v1.1
- [x] Art v1 + quy ước đặt tên đã duyệt — D-020

## Việc tiếp theo (theo thứ tự)
1. ~~Đóng Unity → sửa `Packages/manifest.json` (scoped registry OpenUPM + framework + Unity-MCP IvanMurzak).~~ xong 06-10-2026
2. ~~Mở Unity, resolve package, import TMP, Setup Wizard (SKU `CardSlot`).~~ xong 06-10-2026
3. ~~Kết nối Unity-MCP (IvanMurzak) của editor CardSlot với Claude Code (`.mcp.json`).~~ xong 06-10-2026
4. ~~Mở `Master.unity` → `Framework/Doctor`; Play, kiểm tra log `[MainScreen] entered.`~~ xong 06-10-2026
5. ~~Commit lần đầu (kèm `packages-lock.json`).~~ xong (f0c4fc8)
6. ~~Bước 0 nội dung: concept, game tham khảo, phạm vi MVP.~~ nháp xong 06-10-2026 (D-006…D-012)
7. ~~Chốt người duyệt + duyệt concept → đóng cổng Bước 0.~~ xong 06-10-2026
8. ~~Bước 1 GDD~~ xong 06-10-2026 (D-014, D-015)
9. ~~Bước 2 Mock-up~~ xong 06-10-2026 (D-016…D-018, CR-001)
10. ~~Bước 3 Art~~ xong 06-10-2026 (D-019, D-020)
11. Bước 4 Animation và hiệu ứng.

## Ghi chú kỹ thuật đang mở
- Boot báo `[Localization] no bundled JSON for locale 'en'` và `no bundled products.json` — đúng với SKU mới chưa có nội dung; xử lý khi thêm LocKey / catalog đầu tiên.

## Phiên gần nhất
06-10-2026 (6) · Bước 2 đóng (D-018, CR-001, commit 2b2ed8e). Bước 3: chốt D-019; viết `art/source/art.html` (71 sprite SVG từ token) + `art/tools/build-art.ps1` (Edge headless → cắt → bù alpha → kiểm tra) · bằng chứng: build exit 0, 71 sprite, 0 lỗi chặn, ~0.63 MB ASTC (5.55 MB RGBA32); tự sửa toggle_knob 72×77 → 72×78; đã xem preview.png · sửa trong lúc làm: phép kiểm tra quầng tối ban đầu báo sai 12 sprite (so với pixel sáng nhất) → đổi sang pixel đục gần nhất, bỏ alpha < 64; toggle_knob đổi cạnh bóng đen bán trong suốt thành màu đục #D8CCBE (lệch thẩm mỹ nhỏ); target_slot_bg viền trên 9-slice 44 → 50 · chưa kiểm tra: chưa import vào Unity (Bước 6), chưa xem trên máy thật · đã duyệt (D-020), đã commit.

06-10-2026 (5) · Bước 1 đóng (D-015, commit 5c2e869). Bước 2: công cụ HTML→PNG (D-016); 3 hướng phong cách, chọn B Warm Toybox (D-017); dựng `screens.html`, render 20 mock-up (9:16 + 9:20) vào `docs/mockups/*-v01.png`; nháp `docs/design/design-tokens.md` · bằng chứng: đã xem từng ảnh qua 2 contact sheet, sửa 6 lỗi bố cục (màu lá ô tạm, chồng bị che, R-3 chồng vàng, nhãn booster bị cắt, z-index dialog, nút Yes bị đè) · chưa kiểm tra: chưa xem trên máy thật; chuyển động chưa có mock-up (Bước 4) · đã duyệt (D-018), đã commit.

06-10-2026 (4) · Bước 0 đóng (D-013, commit 4f70d1e); Bước 1: chốt luật lõi D-014; viết `docs/design/GDD.md`, `glossary.md`, `level-design.md`, `economy-sheet.md`, `screen-inventory.md`, `features/` (7 spec) · bằng chứng: chỉ tài liệu · chưa kiểm tra: luật chưa được thử bằng code/solver (làm ở Bước 5); con số kinh tế chưa qua chơi thử · đã duyệt (D-015), đã commit.

06-10-2026 (3) · Bước 0 nội dung: đọc trang App Store của Card Slots (mô tả + 4 ảnh), viết `docs/design/reference-card-slots.md` và `docs/design/concept.md`; chốt D-006…D-012 (nền tảng, kiếm tiền giả lập, tiếng Anh, ý tưởng, MVP 30 level, art 3D-look, tên nội bộ) · bằng chứng: chỉ tài liệu, không có thay đổi code · chưa kiểm tra: chưa chơi thử hay xem video gameplay game tham khảo (các điểm "suy ra" trong bản phân tích cần xác minh) · đã duyệt, đã commit.

06-10-2026 (2) · MCP `ai-game-developer` (localhost:28410) nối vào editor CardSlot; mở `Master.unity`; `Framework/Doctor` lần 1 đỏ check #10 (`.mcp.json` thiếu `timeout`) → chạy `Framework/Agent Docs/Sync` (ghi `"timeout": 900000`) → Doctor lần 2 xanh 10/10; Play một lần (boot smoke) · bằng chứng: console có `[MainScreen] entered.`, không có error/exception · chưa kiểm tra: EditMode tests (không có thay đổi Editor-side), PlayMode suite (của CI) · đã commit `.mcp.json`.

06-10-2026 (1) · manifest: OpenUPM + framework (HTTPS, commit 17912d9) + Unity-MCP 0.93.2; Setup Wizard compose xong (log `[Wizard] Compose complete for sku 'CardSlot'`, agent docs đã sync); sửa `SkuHeadlessTests/SkuEngineFree.props` (probe PackageCache không chạy ở chế độ git) · bằng chứng: `dotnet test SkuHeadlessTests` 31/31 pass; Editor.log không có `error CS` · chưa kiểm tra: Doctor, Play boot, EditMode tests (cần MCP nối vào CardSlot) · đã duyệt, đã commit.
