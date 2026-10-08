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

### CR-002 · phát sinh ở Bước 5 · ảnh hưởng GDD v1.1 §2.2, §5, §12 và `level-design.md` §2 (đã duyệt)
Vấn đề: khi code phát hiện 4 chỗ GDD chưa trả lời.
Quyết định (người chốt: Phat) — đồng ý cả 4 đề xuất:
1. Hai `stack` cùng `layer` không được giao nhau; level vi phạm bị loader từ chối (R-1b).
2. Level khó đánh dấu bằng trường `hard: true` trong file level (G20), không suy từ số thứ tự.
3. `attempt_no` = số lần bắt đầu level hiện tại, lưu trong save (`attempts`), về 0 khi thắng.
4. Save lưu `highest_cleared` (level cao nhất đã thắng) thay cho `max_level_reached`; thắng level ≤ `highest_cleared` là chơi lại (L30: `replay_reward`).
GDD lên v1.2.

### D-023 · 06-10-2026 · Bước 5 · Save, nội dung level, độ sâu test
Quyết định (người chốt: Phat):
- Save: `IUserData` của framework (envelope JSON). Schema v1 = bản ship đầu; đổi định dạng = tăng `SaveSchema` + migrator mới + fixture, không sửa migrator đã ship (rule #2). Xu và booster là tài nguyên trong `IWalletService` (cổng duy nhất cho số dư, AD-5); tiến trình là model `IUserModel` của SKU. Nội dung level đổi revision không ảnh hưởng save (lưu theo số level, không lưu giữa level). Save mới hơn app → không ghi đè.
- Level: generator + solver sinh ứng viên theo `level-design.md` §4, AI chọn 30 level khớp đường cong; Phat duyệt báo cáo độ khó (và chơi thử ở Bước 6).
- Test: đủ theo quy trình — luật R-1…R-18, trường hợp biên, lưu/khôi phục, giao dịch kinh tế có giả lập lưu lỗi, chạy lại lời giải đã kiểm chứng của cả 30 level.

### D-024 · 06-10-2026 · Bước 5 · Duyệt logic và level
Quyết định: code luật/meta, 30 level (`level-report.md`, không level nào lệch đường cong > 0.12) và `architecture-notes.md` được duyệt, đóng cổng Bước 5 — người chốt: Phat
Ghi chú: Bước 6 chia mốc 6a (chơi được trong Editor) → 6b (đủ tính năng) → 6c (hoàn thiện) → 6d (APK, tuỳ chọn).

### CR-003 · phát sinh ở Bước 6 (mốc 6a) · ảnh hưởng D-011, mock-up gameplay v01 (Bước 2), art v1 (Bước 3), animation (Bước 4)
Vấn đề: Phat cho biết game là **3D**, trong khi D-011 chốt "đồ chơi 3D-look" (art 2D) và mọi đầu ra đã duyệt làm theo hướng đó.
Quyết định (người chốt: Phat):
- Mức 3D: a) **bàn chơi 3D thật** (lá, chồng, đích, khay, ô tạm là vật thể 3D, camera nhìn nghiêng từ trên, ánh sáng + bóng), **HUD / Home / dialog giữ 2D uGUI** · b) toàn bộ 3D · c) 2.5D → **a**.
- Nguồn model: a) **AI dựng mesh bằng code trong Unity** (khối đơn giản, màu từ design token) · b) Blender + pf-model-gen · c) artist FBX → **a**.
- Làm mock-up 3D trước: render thử 1 level với 2–3 góc camera ở 1080×1920 để duyệt → mock-up gameplay lên v02.
- Mốc 6a commit làm nền: luồng màn hình, nạp level, controller, HUD, thẻ kết quả dùng lại; chỉ `BoardView` 2D sẽ được thay bằng View 3D.
Ảnh hưởng: D-011 đổi thành "đồ chơi 3D sáng — bàn chơi 3D thật, UI 2D"; mock-up gameplay*, win/lose nền bàn chơi → v02; art: sprite UI giữ, thêm mesh 3D cho bàn chơi (sprite bàn chơi 2D chỉ còn là tham chiếu màu); animation: danh sách và token giữ nguyên, đường bay/hạt tính trong không gian 3D. Luật, level, kinh tế, save (Bước 5) không đổi.

### D-025 · 06-10-2026 · Bước 6 · Góc nhìn bàn chơi 3D (CR-003)
Phương án: nghiêng 30° · 35° · 40° (50° loại: mặt lá bị dẹt) — ảnh `docs/mockups/3d/tilt-compare.png`
Quyết định: **35°**, camera GamePlay giữ orthographic của rig (phối cảnh cần sửa rig framework = thay đổi spine) — người chốt: Phat
Ghi chú: mock-up gameplay 3D v02 = `docs/mockups/3d/gameplay-3d-tilt35-v02.png`; shadowDistance của URP asset 50 → 2500 cho thước 1 px = 1 đơn vị.

### D-026 · 06-10-2026 · Bước 6 · Duyệt mốc 6b
Quyết định: mốc 6b (save thật qua IUserData, 7 dialog, thanh booster, Continue bằng xu / quảng cáo giả, FTUE L1–L2, dim theo token) được duyệt như bản đã kiểm tra trong play mode — ảnh `docs/captures/6b/` — người chốt: Phat
Ghi chú: booster mở khi **đã đến** level mở khoá (không còn "đúng level"), mỗi lần vào level mở tối đa 1 booster; Settings thay chỗ Pause rồi Pause mở lại.

### CR-004 · phát sinh ở Bước 6 (sau mốc 6b) · ảnh hưởng GDD v1.2 §2 (cách hiển thị), mock-up gameplay 3D v02, `Board3DView`, `LevelData`, `LevelGenerator`
Vấn đề: Phat so với game tham khảo: (1) đích đủ khi có **18 lá**, mỗi lần chạm thường đẩy lên một nhóm **6 lá**; (2) chồng bài không chỉ xếp thẳng mà còn **xoè** (vòng cung, hai bên).
Quyết định (người chốt: Phat):
- Đơn vị lá: a) **một "lá" trong luật = một tập 6 lá mỏng khi vẽ**; đích 3 tập = 18 lá, bộ đếm hiện x/18; ô tạm đếm theo tập · b) 18 lá rời thật (sửa luật, sinh lại level) → **a**.
- Kiểu bày: thêm **xoè vòng cung** (lá xoay quanh một tâm, lá trên cùng ở đầu cung); chồng thẳng hiện tại vẫn dùng. Xoè hai bên / so le: không làm.
- Thứ tự: commit 6b trước, rồi làm CR-004: mock-up 3D chồng xoè để duyệt → rồi mới code.
Ảnh hưởng: luật R-1…R-18, solver, kinh tế, save **không đổi**; 30 level giữ nguyên luật, chỉ có thể thêm trường hiển thị `style` cho chồng (luật bỏ qua); mock-up gameplay 3D lên v03.

### D-027 · 06-10-2026 · Bước 6 · Góc nhìn gần từ trên xuống + chồng lá nghiêng (CR-004)
Quyết định (người chốt: Phat): camera nhìn **gần như từ trên xuống** — nghiêng **10°** (thay 35° của D-025); trong chồng thẳng, mỗi lá mỏng **lệch liên tục** sang một bên 3.5 đơn vị so với lá dưới (21 / tập 6 lá), chồng ở nửa trái khay nghiêng sang trái, nửa phải nghiêng sang phải; lá dày 9 đơn vị; mọi lá có hoạ tiết mặt.
Ghi chú: góc gần thẳng đứng làm chiều cao chồng gần như không còn che ô tạm / nhãn đích như ở 35°. Sửa sau: 3.5/lá × 36 lá (6 tập) = 126 — gần bằng chiều rộng một lá, quá nhiều → giảm còn **1.5/lá** (9/tập).

### CR-005 · phát sinh ở Bước 6 (CR-004) · ảnh hưởng GDD v1.2 §2 (R-16, R-17), §4, §7, `economy-sheet.md`, `features/boosters.md`, `features/continue.md`, D-026 (6b)
Vấn đề: Phat: game tham khảo không có booster; khay bàn chơi có thể kéo xuống chỗ thanh booster.
Quyết định (người chốt: Phat):
- Booster: a) **bỏ hẳn** (Undo, Extra Space, thanh booster, dialog mở khoá / mua, giá booster) · b) chỉ ẩn → **a**.
- Xu: a) chỉ dùng cho Continue · b) **bỏ xu luôn** (HUD, thưởng thắng; Continue chỉ bằng quảng cáo) → **b**.
Đã làm: xoá `BoosterService`, `CardSlotResources`, `BoosterId`, R-16/R-17 trong `BoardModel` (solver vẫn dùng lịch sử nội bộ), dialog BoosterBuy/BoosterUnlock (manifest + file), key loc booster/xu, placement `rewarded_booster`; `ProgressModel` bỏ `StartCoinsGranted`, `UnlockedBoosters` (chưa phát hành nên không cần migrator); `DesignTokens.TrayHeight` 650 → 960, bố cục level được căn giữa trong khay; `gameplay.restart.note` → "Your progress on this level will be lost."
Còn lại: sprite booster/xu (`coin`, `icon_undo`, `icon_space`, `boost_tile`…) vẫn trong bộ art, không dùng; ví xu của framework trong save.json máy dev còn số cũ (không hiển thị).

### CR-006 · phát sinh ở Bước 6 (CR-004) · ảnh hưởng GDD v1.3 §2–3, `level-design.md`, 30 level, CR-004 (đơn vị "tập")
Vấn đề: Phat so khay hold với game tham khảo: khoảng **25–26 rãnh, mỗi rãnh 1 lá đứng dọc**; lúc đó ô tạm của mình đếm theo tập 6 lá và level có 6–14 tập.
Quyết định (người chốt: Phat): **"làm như game luôn"**.
Đã làm:
- Lá được đếm **từng lá** (bỏ hệ số ×6 của CR-004): đích cần **18 lá**, một lần chạm đẩy cả run cùng màu (R-6) — thường là 6 lá hoặc bội của 6; ô tạm **26 rãnh** cho mọi level; Continue +6 rãnh; ô tạm báo đỏ khi còn ≤ 6 rãnh.
- Luật (BoardModel) không đổi. Level sinh trong không gian "tập" với ô tạm 4 tập rồi `LevelGenerator.ExpandToCards` (×6 lá, đích ×6, ô tạm 26): 26 rãnh tràn đúng ở tập thứ 5 như 4 tập, nên lời giải của level tập thắng level lá từng chạm (test `CR006_…`). 30 level sinh lại (revision 2), solver chứng minh thắng được; `level-report.md` sinh lại — 9 level lệch đường cong > 0.12 (đa số khó hơn mong muốn: 22, 23, 24, 27, 28).
- `LevelValidator`: ô tạm 1..60, sức chứa đích 2..36, lá mỗi chồng 1..48.
- Hiển thị: mọi lá là lá mỏng; ô tạm vẽ 26 rãnh, mỗi lá đứng dọc trong một rãnh; nhãn x/18 chuyển xuống dưới bệ đích (không bị lá che).

### CR-007 · phát sinh ở Bước 6 · ảnh hưởng mock-up gameplay 3D v03, art v1 (mặt lá), `animation-list.md` (bay lá), `Board3DView`, `BoardView`
Vấn đề: Phat quay video game tham khảo (IMG_3748, level 23–25) — game mình sai nhiều chỗ. Ghi chú 11 điểm: `docs/design/reference-video-3748.md`, ảnh `docs/reference/video-3748/`.
Quyết định (người chốt: Phat):
- Làm theo game: **đích là cọc**, lá **chữ nhật đứng 3:4 bo góc, viền trắng mảnh, lỗ giữa** (sửa sau: lúc đầu làm vuông — Phat "không phải hình vuông") xỏ vào cọc, cọc đầy **đứng lại một lúc** rồi biến mất, cọc sau thay chỗ · **hàng cọc chờ phía sau** thay chip "next" · **lá bay từng lá thành dòng** khi chạm (kéo từ 6c lên) · **không tô tối** chồng bị che.
- Phong cách: **giữ Warm Toybox** (D-017) — đổi hình khối theo game (khay phẳng, lá trơn có lỗ), giữ bảng màu.
- Xu: **giữ không có xu** (CR-005).

### CR-008 · phát sinh ở Bước 6 · ảnh hưởng GDD v1.4 R-6, `LevelData`, 30 level
Vấn đề / quyết định (người chốt: Phat): **một lần chạm chỉ lấy 6 lá**; cọc cùng màu thì mỗi lần chạm lấy 6 lá, nên run 12 lá cùng màu phải chạm 2 lần.
Đã làm: `LevelData.MaxRun` (JSON `max_run`, 0 = cả run) — luật (BoardModel R-6) và solver dùng cùng trường nên luôn khớp; sinh level theo tập với `MaxRun = 1`, `ExpandToCards` nhân thành 6; 30 level sinh lại, solver chứng minh thắng được; còn 5 level lệch đường cong > 0.12 (12, 22, 24, 27, 28 — đều khó hơn). Test mới `R6_CR008_…` (run 12 → 2 lần chạm 6).

### CR-009 · phát sinh ở Bước 6 · ảnh hưởng GDD v1.5 R-2, R-9, 30 level
Vấn đề / quyết định (người chốt: Phat): "khi 1 cột xong, thì cột phía sau di chuyển lên cột trước, chứ không có đổi màu không" — mỗi cột có hàng đợi riêng như game tham khảo.
Đã làm: `BoardModel` chia `target_queue` theo cột (target i → cột i mod n_slots), `NextColorBehind(slot)` cho cọc hàng sau; dữ liệu level giữ nguyên dạng, chỉ đổi nghĩa ⇒ 30 level sinh lại (solver chứng minh thắng được). Hình: cọc đầy đứng lại, co đi, cọc phía sau trượt lên + lớn ra, rồi cọc mới hiện phía sau. Test mới `CR009_…`, sửa test R-10 cho đúng cột. Độ khó tăng: 11 level lệch đường cong > 0.12 (khó hơn) — cần chỉnh đường cong / tham số sweep khi chơi thử.

### CR-010 · phát sinh ở Bước 6 · ảnh hưởng CR-002 (level khó), GDD §3, `level-design.md` §4, D-023 (đường cong độ khó)
Quyết định (người chốt: Phat): "hiện tại game không chia độ khó" — **bỏ level khó** (badge HARD trên HUD, tiêu đề "Hard Level Cleared!", cờ `hard` trong level) và **đường cong độ khó không còn là việc phải làm**; sweep vẫn dùng bảng tham số để xếp 30 level nhưng không gắn nhãn khó.
Đã làm: xoá `LevelData.Hard`, `GenParams.Hard`, `WinInfo.Hard`, key `gameplay.hard`, `win.title_hard`, badge trên HUD; 30 level sinh lại không có trường `hard` (cùng seed, cùng bố cục); bỏ test "cờ hard ở mỗi level thứ 5" (30 ca).

### CR-011 · phát sinh ở Bước 6 · ảnh hưởng `LevelGenerator`, 30 level, `Board3DView` (khay), `level-design.md` §4
Vấn đề: video game tham khảo IMG_3750 (level 1–5) — ghi chú `docs/design/reference-video-3750.md`, ảnh `docs/reference/video-3750/`.
Quyết định (người chốt: Phat): làm **khay vừa khít bố cục**, **chồng xếp lưới + lệch như game**, **level đầu dễ như game**. Không làm (lần này): vòng sáng khi cọc đầy.
Đã làm:
- Sinh level theo **lưới đều** (bước 176 × 232, tối đa 5 cột × 3 hàng, hàng cuối thiếu thì căn giữa); tầng trên nằm lệch **xuống-phải** (42, 52) so với chồng tầng dưới nên tầng dưới lộ ra ở góc trên-trái; số chồng giảm dần lên trên; hình chữ nhật chồng = một lá (150 × 206).
- Lá trong chồng lệch **sang phải** 1.5/lá (bỏ "nghiêng về nửa gần").
- **Khay vừa khít**: rộng / cao theo bố cục + đệm 36 (tối thiểu 560 × 420), căn giữa ngang, chừa thêm phía trên cho chồng cao (do bàn nghiêng 10°).
- **Level 1–5**: mỗi màu một đích; level 1: 2 cọc 2 màu; level 2–4: 3 cọc 3 màu, chưa có hàng cọc chờ; level 5: 4 màu trên 2 cọc → hàng cọc chờ bắt đầu. Bỏ ràng buộc "level 2 bắt dùng ô tạm" (FTUE đang tắt). Test mới `First_levels_are_easy_like_the_reference`.
Còn lại: ô tạm vẫn rộng gần hết màn hình (thu hẹp làm rãnh mỏng đi).

### CR-012 · phát sinh ở Bước 6 · ảnh hưởng GDD v1.7 (gần như toàn bộ), `glossary.md`, `level-design.md`, `economy-sheet.md`, `screen-inventory.md`, `features/*`, mock-up (Bước 2), art v1 (Bước 3), `animation-list.md` (Bước 4), `BoardModel` + solver + `LevelGenerator` + 30 level (Bước 5), save, CR-005, CR-008 — **Phat chốt 08-10-2026: làm theo GDD anh Tánh** (xem "Đã chốt" cuối mục)
Vấn đề: Phat đưa GDD "Card Slots" của anh Tánh (https://hvtanh07.github.io/Card-Slot-GDD/, bản chụp `docs/design/reference-gdd-tanh.md`) và chọn mở CR theo GDD này. GDD đó lệch GDD v1.7 ở luật lõi, có lại booster + xu (ngược CR-005), thêm tim, revive bằng Remove, RV Slot, 3 element, thanh tiến độ mở element, FTUE mới, 8 màu, bảng SFX/rung, remote config.
Khuyến nghị chung: commit phần CR-004…CR-011 đang chờ duyệt **trước**, rồi làm CR-012 theo từng giai đoạn (dưới), mỗi giai đoạn qua cổng duyệt riêng; GDD lên **v2.0** sau khi chốt mục 1–4.

Cần chốt (khuyến nghị in đậm):
1. **Vai trò GDD anh Tánh.** **a) là nguồn chuẩn: viết lại GDD.md thành v2.0 theo nó, chỗ GDD đó chưa nói thì giữ v1.7** · b) chỉ lấy từng phần Phat chọn.
2. **Màu trong chồng.** Hiện tại: chồng nhiều màu, chạm lấy run cùng màu trên cùng tối đa 6 lá (CR-008); level 12 có 7/9 chồng nhiều màu. GDD: "lá cùng màu nằm chung một chồng". **a) mỗi chồng một màu, chạm gửi cả chồng (bỏ `max_run`)** · b) mỗi chồng một màu nhưng vẫn lấy tối đa 6 lá mỗi lần chạm · c) giữ chồng nhiều màu (bỏ qua câu này của GDD). Ghi chú: video IMG_3748 (CR-008) thấy chạm lấy 6 lá — nên hỏi anh Tánh kích thước chồng thường gặp.
3. **Chồng che.** Hiện tại (R-3): mọi chồng tầng cao hơn giao nhau đều che. GDD: chỉ chồng **khác màu** mới che. **a) theo GDD** · b) giữ R-3.
4. **Thua khi ô chờ đầy.** Hiện tại (R-12): lá không còn chỗ trong ô chờ ⇒ thua ngay (tràn). GDD: thua khi ô chờ đầy **và** không còn nước đi khớp màu — GDD không nói khi một lần chạm có nhiều lá hơn số ô trống. a) chồng không vừa ô trống thì không cho chạm (chồng rung) · b) lá điền tới khi đầy, phần dư ở lại chồng · c) giữ tràn = thua. **Hỏi anh Tánh; nếu cần chốt ngay: c**, vì a/b đổi cảm giác chơi mà GDD không mô tả.
5. **Revive.** Hiện tại: Continue bằng quảng cáo, +6 ô chờ, 1 lần / attempt. GDD: 2 lần Remove miễn phí lên cọc đáy ít lá nhất, lặp tới khi hết kẹt; remote config có giá lần đầu + hệ số tăng + tối đa mỗi level. **a) theo GDD, trả bằng quảng cáo thưởng hoặc xu (giá tăng dần), tối đa theo remote config** · b) theo GDD nhưng chỉ bằng quảng cáo.
6. **RV Slot.** **a) theo GDD: 2 lần / level, mỗi lần +8 ô, nút nằm cạnh ô chờ** · b) không làm.
7. **Booster (ngược CR-005a).** **a) làm lại Hand (L5), Shuffle (L8), Remove (L10), mỗi loại tặng 3, có popup mở khoá** · b) không làm. Tên booster bán trong remote config (Paper Box / Magnet) lệch Hand / Shuffle — **hỏi anh Tánh; tạm dùng Hand / Shuffle / Remove**. Shuffle là ngẫu nhiên lúc chơi ⇒ R-15 đổi: model nhận `IRandom` (rule #14), seed ghi log.
8. **Xu (ngược CR-005b).** **a) có lại xu: thưởng thắng, nhân thưởng bằng quảng cáo, mua booster, trả revive** · b) không có xu (booster chỉ nhận từ tặng / quảng cáo). Giá trị: AI đề xuất trong `economy-sheet.md` v2, Phat duyệt.
9. **Tim.** **a) theo GDD: 5 tim, hồi 20 phút, mất khi thua-chơi-lại / restart / thoát về Home; hết tim thì có màn chờ** (cần mock-up mới) · b) không làm. Ghi chú: tính năng theo thời gian ⇒ trường hợp biên "đổi giờ máy" phải xử lý (dùng giờ lưu + chặn quay ngược).
10. **Element + thanh tiến độ.** **a) làm cả 3 (L7, L12, L17) + thanh tiến độ trên màn Win** · b) chỉ thanh tiến độ khi có element. Generator, solver và 30 level phải hỗ trợ element; level 7–30 sinh lại.
11. **FTUE.** **a) bật lại FTUE theo GDD: bước T&C / Privacy, 3 bước chạm có highlight + input masking, lưu bước; popup element / booster** · b) giữ tắt. T&C cần link thật (ai cung cấp?).
12. **Số màu.** Level hiện dùng 6 màu (`color_0`…`color_5`). **a) 8 màu theo GDD (thêm 2 màu vào token + art, giữ Warm Toybox)** · b) giữ 6.
13. **SFX / rung, animation, remote config.** **a) theo bảng GDD; remote config dùng ConfigKey của framework, giá trị mặc định do AI đề xuất** · b) để sau.
14. **Chỗ GDD chưa nói** — giữ của v1.7 cho tới khi anh Tánh bổ sung: cọc 18 lá, 3 cột, ô chờ 26 rãnh, 30 level, chạm khi đang có hoạt ảnh vẫn nhận (R-8). **a) giữ** · b) hỏi anh Tánh trước khi làm.

Giai đoạn đề xuất (mỗi giai đoạn: tài liệu → mock-up nếu có màn mới → code + test headless → play mode xem → duyệt):
- A. Luật lõi (mục 2, 3, 4, 12) → `BoardModel`, solver, generator, 30 level sinh lại.
- B. Revive + RV Slot (5, 6).
- C. Xu + booster (7, 8).
- D. Element + thanh tiến độ (10).
- E. Tim (9).
- F. FTUE (11).
- G. SFX / rung / remote config (13).

Phần phía sau cần kiểm tra lại: GDD → v2.0 + lịch sử; `glossary.md` (pole, queue, waiting slot, element, booster); `level-design.md`; `economy-sheet.md` v2; `screen-inventory.md` (hết tim, T&C, popup element / booster, thanh tiến độ, nút RV Slot); mock-up mới; art (icon booster cũ không dùng lại được — Hand / Shuffle / Remove khác Undo / Extra Space; tim; băng; khói; 2 màu mới); `animation-list.md`; save (chưa phát hành ⇒ chưa cần migrator, như CR-005); test headless (R-3, R-6, R-12, R-15, R-18 đổi).

Đã chốt (người chốt: Phat, 08-10-2026): **"mình làm theo GDD của Tánh"** ⇒ mục 1 = a (GDD anh Tánh là nguồn chuẩn, GDD.md lên v2.0); mục 2, 3, 5–13 = phương án theo GDD (a); booster dùng tên Hand / Shuffle / Remove như thân GDD. Mục 14 = a (chỗ GDD chưa nói giữ của v1.7 — đúng theo 1a). Mục 4 = **c (tràn = thua, giữ R-12)** cho tới khi anh Tánh bổ sung. Thứ tự: commit CR-004…CR-011 trước (D-028), rồi làm giai đoạn A.

### D-028 · 08-10-2026 · Bước 6 · Duyệt CR-004…CR-011 và D-027
Quyết định (người chốt: Phat): duyệt phần đã làm của CR-004…CR-011 và D-027 (lá mỏng, bỏ booster + xu, đích 18 / ô tạm 26, cọc + lá có lỗ + bay từng lá, chạm lấy tối đa 6 lá, hàng đợi theo cột, bỏ độ khó, lưới + khay vừa khít, camera 10°) để commit trước khi bắt đầu CR-012.
Ghi chú: CR-012 sẽ đảo một phần (CR-005 booster + xu, CR-008 `max_run`) — commit này là mốc để quay lại nếu cần.

### D-029 · 08-10-2026 · Bước 6 · Duyệt CR-012 giai đoạn A (luật lõi)
Quyết định (người chốt: Phat, "ok, commit đi"): duyệt giai đoạn A — chồng một màu (R-1c, level JSON v3 `color` + `count`), chỉ chồng khác màu mới che (R-3), chạm gửi cả chồng (R-6, bỏ `max_run` của CR-008), thua do kẹt (R-13), 8 màu (Pink `#FF6FB5` bông hoa, Cyan `#2FCFE0` bông tuyết; art 2D thêm `*_6`, `*_7`), 30 level sinh lại (revision 3, tổng số cọc theo level), `level-design.md` v2.0, `glossary.md` v2.0.
Ghi chú: Continue (+6) giữ tạm tới giai đoạn B. Các mục [XÁC NHẬN] khác của GDD v2.0 (booster, tim, element, kinh tế…) duyệt khi tới giai đoạn của chúng.

### D-030 · 08-10-2026 · Bước 6 · Duyệt CR-012 giai đoạn B (Revive + RV Slot)
Quyết định (người chốt: Phat, "ok commit"): duyệt R-19 Remove (lõi), R-22 Revive (2 Remove miễn phí lên cọc ít lá nhất, lặp tới khi hết kẹt, tối đa 3 / lượt), R-23 RV Slot (+8, 2 / lượt, lúc chơi hoặc ở màn thua), bỏ Continue; màn thua 2 lựa chọn + nút "▶ +8 slots" trên ô chờ; `features/revive.md` v1.0; quảng cáo `rewarded_revive`, `rewarded_rv_slot`.
Ghi chú: Revive / RV Slot hiện chỉ bằng quảng cáo; trả bằng xu và hoạt ảnh "cọc bay về bàn" làm ở giai đoạn C.
