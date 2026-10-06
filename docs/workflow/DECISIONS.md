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
