# Trạng thái project
Game: CardSlot | Nền tảng: … | Engine/framework: Unity 6000.3.13f1 (URP) + PrototypeFramework 0.4.2 | Độ phân giải chuẩn: …
Người duyệt: Thiết kế … · Hình ảnh … · Art … · Kỹ thuật … · Phát hành …

| Bước | Trạng thái | Đầu ra đã duyệt (phiên bản) | Người duyệt, ngày |
|---|---|---|---|
| 0 Khởi động | đang làm (framework chạy được; còn nội dung) | | |
| 1 GDD | chưa bắt đầu | | |
| 2 Mock-up | chưa bắt đầu | | |
| 3 Art | chưa bắt đầu | | |
| 4 Animation | chưa bắt đầu | | |
| 5 Logic | chưa bắt đầu | | |
| 6 Ráp | chưa bắt đầu | | |
| 7 AI test | chưa bắt đầu | | |
| 8 Máy thật/Chơi thử/Phát hành | chưa bắt đầu | | |

## Câu hỏi đang chờ người làm
- [ ] Link Google Play game tham khảo (Bước 0, hỏi 06-10-2026)
- [ ] Người duyệt từng bước (Bước 0)

## Việc tiếp theo (theo thứ tự)
1. ~~Đóng Unity → sửa `Packages/manifest.json` (scoped registry OpenUPM + framework + Unity-MCP IvanMurzak).~~ xong 06-10-2026
2. ~~Mở Unity, resolve package, import TMP, Setup Wizard (SKU `CardSlot`).~~ xong 06-10-2026
3. ~~Kết nối Unity-MCP (IvanMurzak) của editor CardSlot với Claude Code (`.mcp.json`).~~ xong 06-10-2026
4. ~~Mở `Master.unity` → `Framework/Doctor`; Play, kiểm tra log `[MainScreen] entered.`~~ xong 06-10-2026
5. ~~Commit lần đầu (kèm `packages-lock.json`).~~ xong (f0c4fc8)
6. Bắt đầu Bước 0 nội dung: concept, game tham khảo, phạm vi MVP.

## Ghi chú kỹ thuật đang mở
- Boot báo `[Localization] no bundled JSON for locale 'en'` và `no bundled products.json` — đúng với SKU mới chưa có nội dung; xử lý khi thêm LocKey / catalog đầu tiên.

## Phiên gần nhất
06-10-2026 (2) · MCP `ai-game-developer` (localhost:28410) nối vào editor CardSlot; mở `Master.unity`; `Framework/Doctor` lần 1 đỏ check #10 (`.mcp.json` thiếu `timeout`) → chạy `Framework/Agent Docs/Sync` (ghi `"timeout": 900000`) → Doctor lần 2 xanh 10/10; Play một lần (boot smoke) · bằng chứng: console có `[MainScreen] entered.`, không có error/exception · chưa kiểm tra: EditMode tests (không có thay đổi Editor-side), PlayMode suite (của CI) · đã commit `.mcp.json`.

06-10-2026 (1) · manifest: OpenUPM + framework (HTTPS, commit 17912d9) + Unity-MCP 0.93.2; Setup Wizard compose xong (log `[Wizard] Compose complete for sku 'CardSlot'`, agent docs đã sync); sửa `SkuHeadlessTests/SkuEngineFree.props` (probe PackageCache không chạy ở chế độ git) · bằng chứng: `dotnet test SkuHeadlessTests` 31/31 pass; Editor.log không có `error CS` · chưa kiểm tra: Doctor, Play boot, EditMode tests (cần MCP nối vào CardSlot) · chưa commit.
