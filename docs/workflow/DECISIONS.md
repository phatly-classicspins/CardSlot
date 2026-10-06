# Quyết định — CardSlot

Mỗi mục: câu hỏi, phương án, quyết định, người chốt, lý do, ảnh hưởng. Quyết định chưa có ở đây coi như chưa có (G2).

### D-001 · 06-10-2026 · Bước 0 · Framework
Câu hỏi: Dựng game trên nền gì?
Quyết định: AI Prototype Framework (`com.classicspins.prototype-framework`, repo `FoxCubGames/ai_prototype_framework`) + quy trình BMAD — người chốt: Phat
Lý do: framework được thiết kế để AI code phần lớn game; rule kỹ thuật của framework được ưu tiên hơn rule chung của `AI-GAME-WORKFLOW` (§ đầu tài liệu).
Ảnh hưởng: kiến trúc Domain/Application/Presentation, VContainer, codegen theo manifest, addressable.

### D-002 · 06-10-2026 · Bước 0 · Cách gắn framework
Phương án: a) git URL HTTPS · b) git URL SSH (alias `github-classicspins`) · c) dev-link `file:`
Quyết định: a) `https://github.com/FoxCubGames/ai_prototype_framework.git?path=/Packages/com.classicspins.prototype-framework#master` — người chốt: Phat
Lý do: máy chưa có SSH alias; HTTPS đã có quyền truy cập. Không cần sửa framework nên không dùng dev-link.
Ảnh hưởng: commit `Packages/packages-lock.json` để pin commit framework.

### D-003 · 06-10-2026 · Bước 0 · Phiên bản Unity
Phương án: a) giữ 6000.3.13f1 · b) đổi sang 6000.3.12f1 (bản framework đã kiểm chứng)
Quyết định: a) — người chốt: Phat
Lý do: package chỉ yêu cầu `6000.3`; xử lý nếu Doctor báo lỗi.

### D-004 · 06-10-2026 · Bước 0 · Unity MCP
Quyết định: cài Unity-MCP của IvanMurzak (`com.ivanmurzak.unity.mcp`) cho khớp quy trình framework — người chốt: Phat

### D-005 · 06-10-2026 · Bước 0 · BMAD
Quyết định: copy `_bmad/`, `.bmad-loop/` (hook + profile) và `.claude/skills/bmad-*` từ repo framework — người chốt: Phat
Ghi chú: loop tự động (`bmad-loop`) cần Python + uv + `bmad-loop init`; máy hiện chưa có Python nên chưa bật hook bmad-loop trong `.claude/settings.json`.

### D-006 · 06-10-2026 · Bước 0 · Nền tảng
Phương án: a) Android + iOS, dọc · b) chỉ Android, dọc · c) mobile ngang
Quyết định: a) Android + iOS, màn dọc, độ phân giải chuẩn 1080×1920 — người chốt: Phat
Lý do: khớp sẵn rig của framework (safe rect x ±540 · y ±960), không phải chỉnh cấu hình.

### D-007 · 06-10-2026 · Bước 0 · Kiếm tiền MVP
Phương án: a) có nhưng giả lập · b) không · c) quảng cáo + IAP thật
Quyết định: a) luồng quảng cáo/IAP chạy trên Fake Ads/IAP của framework, chưa nối SDK thật — người chốt: Phat
Ảnh hưởng: chưa cần tài khoản AdMob/Store; nối SDK sau qua service module (`pf-add-service-module`).

### D-008 · 06-10-2026 · Bước 0 · Ngôn ngữ ra mắt
Quyết định: chỉ tiếng Anh; mọi text vẫn đi qua `LocKey` để thêm ngôn ngữ sau không phải sửa code — người chốt: Phat

### D-009 · 06-10-2026 · Bước 0 · Ý tưởng và game tham khảo
Quyết định: puzzle "chạm để xếp" màu với lá bài, đích và ô tạm có giới hạn; tham khảo Card Slots (https://apps.apple.com/us/app/card-slots/id6757999544) — người chốt: Phat
Ảnh hưởng: `docs/design/concept.md`, `docs/design/reference-card-slots.md`. Chỉ học vòng chơi và nhịp độ, không lấy art/level (G8).

### D-010 · 06-10-2026 · Bước 0 · Phạm vi MVP
Phương án số level: a) 30 · b) 15 · c) 50+ — tính năng: a) giữ đề xuất · b) bỏ booster · c) thêm Xáo bài
Quyết định: 30 level đã qua solver; 6 màn hình (Home, Gameplay, Thắng, Thua/Tiếp tục, Tạm dừng, Cài đặt); booster Thêm ô + Hoàn tác; xu lưu trên máy; Tiếp tục khi thua bằng rewarded/xu; interstitial; tutorial bàn tay level 1–2 — người chốt: Phat
Để sau MVP: bản đồ level, daily reward, IAP/SDK thật, sự kiện, leaderboard, cloud save, thêm ngôn ngữ, layout tablet.

### D-011 · 06-10-2026 · Bước 0 · Hướng art
Phương án: a) đồ chơi 3D-look sáng · b) bàn bài cổ điển · c) phẳng tối giản
Quyết định: a) khối bo tròn, bóng mềm, màu bão hoà trên nền pastel; mỗi màu có thêm hoạ tiết cho người mù màu — người chốt: Phat

### D-012 · 06-10-2026 · Bước 0 · Tên game
Quyết định: "CardSlot" chỉ là tên nội bộ (repo, SKU); đặt tên thương mại trước khi lên store ở Bước 8 — người chốt: Phat
Lý do: gần trùng "Card Slots" đã có trên App Store.

### D-013 · 06-10-2026 · Bước 0 · Người duyệt và cổng Bước 0
Quyết định: Phat duyệt tất cả các bước (Thiết kế, Hình ảnh, Art, Kỹ thuật, Phát hành); `concept.md` v0.2 được duyệt, đóng cổng Bước 0 — người chốt: Phat

### D-014 · 06-10-2026 · Bước 1 · Luật lõi
Câu hỏi và quyết định (người chốt: Phat):
- Đơn vị chạm: a) cả nhóm lá cùng màu liền nhau trên cùng của chồng · b) một lá → **a**
- Hàng đích: a) hàng đợi cố định theo level, 3 đích hiện cùng lúc, có hiện trước màu kế tiếp · b) cố định, không hiện trước · c) đích không thay → **a**
- Ô tạm: a) lá cùng màu tự bay lên khi có đích phù hợp · b) người chơi chạm để gửi → **a**
- Thua và chấm điểm: a) ô tạm tràn = thua; không sao, không giới hạn thời gian/nước · b) thêm 3 sao · c) thêm giới hạn thời gian → **a**
Lý do: giống nhịp game tham khảo; xác định hoàn toàn nên solver kiểm chứng được.
Ảnh hưởng: `docs/design/GDD.md` §2.

### D-015 · 06-10-2026 · Bước 1 · Duyệt GDD
Quyết định: GDD v1.0 và các tài liệu đi kèm (glossary, level-design, economy-sheet, screen-inventory, 7 spec trong `features/`) được duyệt nguyên trạng, gồm 6 mục [XÁC NHẬN]: chạm được khi đang hoạt ảnh (R-8); Undo lùi được tới đầu level, giữ chỗ buffer đã cộng (R-16); Extra Space +4, 1 lần/lượt (R-17); chỉ tài khoản khách, không cloud save; hết Level 30 → "More levels coming soon" + chơi lại L30; Back ở Home không làm gì. Con số kinh tế và đường cong độ khó dùng như bản nháp, chỉnh sau chơi thử Bước 8 — người chốt: Phat

### D-016 · 06-10-2026 · Bước 2 · Cách làm mock-up
Phương án công cụ: a) HTML/CSS → PNG (Edge headless) · b) cài Python + Codex, sinh ảnh image_gen · c) kết hợp
Quyết định: a) — tỉ lệ 9:16 (1080×1920) + 9:20 (1080×2400); không chừa vùng banner — người chốt: Phat
Lý do: không tốn quota, không cần cài thêm; bố cục và token chính xác. Hình minh hoạ 3D-look thật làm ở Bước 3.
Ảnh hưởng: mock-up là bố cục + design token, không phải art cuối; `pf-ux-asset-handoff` (tách sprite từ mock-up) không áp dụng — sprite đến từ Bước 3.

### D-017 · 06-10-2026 · Bước 2 · Hướng phong cách
Phương án: A Lavender Candy · B Warm Toybox · C Sky Mint (ảnh so sánh: `_bmad-output/planning-artifacts/ux-designs/ux-cardslot/.working/directions-sheet.png`)
Quyết định: B Warm Toybox — nền kem/đào ấm, khay gỗ, nút chính xanh ngọc, nút phụ đỏ cam; làm Gameplay + các trạng thái trước — người chốt: Phat
Lý do: khay gỗ tách bạch rõ nhất với lá bài màu; khác hẳn tông tím của game tham khảo (G8).

### CR-001 · phát sinh ở Bước 2 · ảnh hưởng GDD v1.0 §10 và `features/core-gameplay.md` (đã duyệt)
Vấn đề: "Hard Level Cleared!" (19) và "New booster: Extra Space!" (25) vượt giới hạn tiêu đề 18 ký tự; dialog Restart cần nói rõ booster đã dùng không hoàn lại (GDD §5).
Phương án: a) nâng giới hạn lên 26 + ribbon tự thu nhỏ chữ; thêm key `gameplay.restart.note` · b) rút ngắn text
Quyết định: a) — người chốt: Phat · GDD lên v1.1.

### D-018 · 06-10-2026 · Bước 2 · Duyệt mock-up
Quyết định: 20 mock-up v01 (`docs/mockups/README.md`) và design token nháp (`docs/design/design-tokens.md`) được duyệt, đóng cổng Bước 2 — người chốt: Phat

### D-019 · 06-10-2026 · Bước 3 · Tool art và quy tắc xuất
Câu hỏi và quyết định (người chốt: Phat):
- Tool nguồn: a) SVG do AI dựng, group đặt tên chuẩn, màu lấy từ design token, xuất PNG bằng Edge headless · b) artist vẽ PSD · c) SVG + sinh ảnh minh hoạ → **a**. File gốc `art/source/` nằm trong repo.
- Xuất: 1x (1 px = 1 đơn vị, đúng kích thước mock-up), kích thước chẵn, atlas tối đa 2048×2048, nén ASTC 6×6 (Android + iOS), tổng texture game ≤ 16 MB trong RAM.
- AI tự sửa: lỗi kỹ thuật không đổi hình (kích thước lẻ, quầng alpha, cắt mép, pixel lạc, thiếu padding). Mọi thay đổi màu/hình/tỉ lệ so với mock-up phải báo lại.
- Chấp nhận cho MVP: chỉ lỗi thẩm mỹ nhỏ (lệch màu/bóng ≤ một bước token, bóng đơn giản hơn mock-up). Không chấp nhận: sai kích thước, quầng alpha, cắt mép, sai màu lá, thiếu hoạ tiết.
Ghi chú: thay cho `art/source/*.psd` của quy trình, file nguồn là SVG nhiều group (mỗi group = một layer).

### D-020 · 06-10-2026 · Bước 3 · Duyệt art
Quyết định: art v1 (71 sprite, `art/export/report.md` 0 lỗi chặn) và quy ước đặt tên sprite/layer trong `art/README.md` được duyệt, đóng cổng Bước 3 — người chốt: Phat

### D-021 · 06-10-2026 · Bước 4 · Animation, hiệu ứng, âm thanh
Câu hỏi và quyết định (người chốt: Phat):
- Tool: a) tween bằng code (LitMotion, đã có trong framework, MIT) + ParticleSystem qua `EffectService` · b) Spine → **a**. Không thêm runtime, không cần license.
- Phạm vi MVP: a) P0 + P1 (21 mục trong `animation-list.md`) · b) cả P2 · c) chỉ P0 → **a**. P2 để sau.
- "Tắt animation": a) code sẵn (mọi animation nhảy tới pose cuối khi bật cờ config `motion.reduced`), chưa có công tắc trong Settings · b) thêm công tắc (CR) → **a**. GDD và mock-up không đổi.
- Âm thanh: để sau — MVP chưa có âm thanh; vẫn gắn `AudioKey` theo GDD §9, file thêm trước Bước 8 (ghi chép license lúc đó).
Ảnh hưởng: Bước 4 giao spec + token chuyển động + trang xem thử; gắn vào Unity ở Bước 6 (cổng "chạy đúng trong runtime" kiểm ở đó).

### D-022 · 06-10-2026 · Bước 4 · Duyệt animation
Quyết định: `animation-list.md` v1.0 (21 animation P0+P1, 2 prefab hạt, token chuyển động) và cảm giác trong `art/motion/preview.html` được duyệt — người chốt: Phat
Ghi chú: cổng "chạy đúng trong runtime" của Bước 4 chuyển sang kiểm ở Bước 6 (animation là code trong `Game.Views`).
