# Ghi chú kiến trúc — CardSlot (Bước 5)

> v1 · Bước 5 · đã duyệt (Phat, 06-10-2026, D-024). Theo rule của PrototypeFramework (`AGENTS.md`), D-023, GDD v1.2.

## Module

| Thư mục | Assembly | Nội dung |
|---|---|---|
| `Assets/CardSlot/Features/Board/Domain/` | `Game.Domain` (engine-free) | `LevelData`, `LevelValidator` (R-1, R-1b, khoảng hợp lệ), `BoardModel` (R-2…R-18), `LevelSolver`, `LevelGenerator` |
| `Assets/CardSlot/Features/Board/Infrastructure/` | `Game.Infrastructure` | `LevelJson`: đọc/ghi file level (Newtonsoft). Không phụ thuộc engine nên test headless compile thẳng file này |
| `Assets/CardSlot/Features/Meta/Application/` | `Game.Application` (engine-free) | `ProgressModel`, `SettingsModel`, cổng `IProgressStore`, `EconomyTuning`, `LevelProgressService`, `BoosterService`, `AdPacing` |
| `Assets/CardSlot/Content/Levels/level_001…030.json` | dữ liệu | 30 level đã qua solver; mỗi file có `seed` và `solution` |
| `SkuHeadlessTests/{Board,Meta,Levels}/` | test `dotnet test` | luật, meta, chạy lại lời giải từng level; `LevelGenSweep` là tool sinh level (`[Slow]`) |

## Luồng dữ liệu

```
chạm (View → controller, Bước 6)
  → BoardModel.Tap(stack) ── trả về BoardStep[] ──→ View phát lại animation (R-8: model không chờ view)
  → thắng: LevelProgressService.CompleteLevel  ── lưu xong mới trả WinInfo ──→ WinDialog (G19)
  → booster / Continue: BoosterService.TryUse / TryContinue
        kiểm tra board nhận được → trừ ví (IWalletService) → áp vào board
```

- **Luật thuần, tất định (G16, R-15):** `BoardModel` không đọc thời gian, không có random. Random chỉ có trong generator, qua `IRandom` (`Pcg32`) với seed ghi trong file level (rule #14).
- **Undo:** mỗi lần chạm lưu một snapshot nhỏ (số lá đã lấy mỗi chồng, ô đích, ô tạm). Undo khôi phục snapshot, trừ sức chứa ô tạm (R-16).
- **Solver:** DFS có ghi nhớ trạng thái, ưu tiên nước đi mà cả `run` vào thẳng đích. Đo thêm ô tạm tối thiểu và tỉ lệ thắng của bot ngẫu nhiên (`level-report.md`).

## Save và kinh tế (D-023)

- **Xu và booster** là tài nguyên trong `IWalletService` (`coin`, `booster_undo`, `booster_add_slot`): cổng số dư duy nhất của framework (AD-5), trừ tiền theo kiểu được hết hoặc không gì cả.
- **Tiến trình / FTUE / nhịp quảng cáo** nằm ở `ProgressModel`; **cài đặt** ở `SettingsModel`. Cả hai là `IUserModel` lưu trong envelope `IUserData`, schema v1.
- **Cổng `IProgressStore`:** `Game.Application` không tham chiếu UniTask nên không dùng thẳng `IUserData`. Adapter trên `IUserData` sẽ viết ở Bước 6.
- **Giao dịch (G18):** đổi trong bộ nhớ → grant ví → lưu. Lưu lỗi (exception) thì khôi phục model và lấy lại phần đã grant (`GrantSource.Compensation`).
- **Giới hạn đã biết:** `IUserData.Save()` của framework ghi trễ (debounced) và không trả kết quả, nên lỗi ghi đĩa thật có thể không thành exception tại chỗ gọi. Phần rollback ở trên chỉ bảo vệ những lỗi lộ ra được qua `IProgressStore`.

## Việc của Bước 6

Adapter `IProgressStore` → `IUserData` và đăng ký model mặc định. Đưa `EconomyTuning` sang config key (`game.configkeys.json`). Nạp file level qua Addressables. Controller và View cho Gameplay / Home / dialog. `DesignTokens.cs` (gồm `Motion`). Gắn analytics theo GDD §12.
