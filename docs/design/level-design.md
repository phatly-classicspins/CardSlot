# Thiết kế level — CardSlot

> **v2.0 · nháp, chờ Phat duyệt** (CR-012 giai đoạn A). v1.0 đã duyệt (Phat, 06-10-2026). Luật: `GDD.md` v2.0 §2. Con số: `economy-sheet.md`.
> v2.0 gộp luôn những gì CR-004…CR-011 đã đổi mà v1.0 chưa ghi (đếm từng lá, cọc 18, ô chờ 26, hàng cọc theo cột, lưới, bỏ level khó).

## 1. Cấu trúc

- 30 level tuyến tính, không chia chapter (D-010).
- Mỗi level là một file dữ liệu có tên, revision nội dung và các trường có khoảng hợp lệ (G20). Không viết cứng level trong code.
- Kích thước cố định cho mọi level: **cọc chứa 18 lá**, **ô chờ 26 rãnh**, **3 cột** (level 1–5 có ngoại lệ, §4).

## 2. Dữ liệu một level (định dạng v3)

```json
{
  "id": "level_007",
  "revision": 3,
  "seed": 700012,
  "n_slots": 3,
  "buffer_capacity": 26,
  "targets": [ {"color": "color_0", "capacity": 18}, {"color": "color_1", "capacity": 18} ],
  "stacks": [ {"id": "s0", "x": 0, "y": 0, "w": 150, "h": 206, "layer": 0, "color": "color_0", "count": 12} ],
  "ftue": null,
  "solution": ["s3", "s0", "…"]
}
```

- **v3 (CR-012):** một chồng là **một màu + số lá** (`color`, `count`), thay mảng `cards` nhiều màu của v2. Bỏ `max_run` (chạm gửi cả chồng) và `hard` (CR-010).
- `targets` theo thứ tự hàng đợi; `target` thứ i thuộc cột i mod `n_slots` (CR-009).
- Toạ độ là **đơn vị thế giới** (1 px tham chiếu = 1 đơn vị), tính trong khay; hình chữ nhật dùng cho R-3 (che phủ). View tự căn khay vừa bố cục (CR-011).
- `seed` là seed generator đã sinh level, giữ lại để tái tạo. `solution` là lời giải solver đã chứng minh; test `ShippedLevelTests` chạy lại mỗi lần `dotnet test`.
- Chưa phát hành ⇒ đổi định dạng không cần migrator; level cũ được sinh lại.

| Trường | Khoảng hợp lệ (`LevelValidator`) |
|---|---|
| `n_slots` | 2–4 |
| `buffer_capacity` | 1–60 |
| `targets[].capacity` | 2–36 |
| số màu | 2–8 |
| số `stack` | 2–30 |
| `count` | 1–48 |
| `layer` | 0–4 |

## 3. Các yếu tố tạo độ khó

| Yếu tố | Dễ → khó |
|---|---|
| Số màu | 3 → 8 |
| Độ sâu che phủ (số `layer`) | 1 → 5 |
| Số cọc mỗi màu | 1 → 2 |
| Kích thước chồng | 18 lá (1 chồng / cọc) → 6–18 lá trộn |
| Thứ tự hàng cọc | khớp với chồng đang lộ → lệch, buộc phải nhìn trước |
| Element (giai đoạn D) | không có → Hidden / Locked |

Ô chờ cố định 26 rãnh: độ khó đến từ bố cục, không từ việc thu nhỏ ô chờ.

## 4. Đường cong

Răng cưa: tăng dần trong mỗi nhóm 5 level, level ngay sau là level nghỉ. Không có nhãn "khó" (CR-010). D muốn / D đạt và số liệu thật: `level-report.md` (sinh tự động).

| Level | Vai trò | Màu | Layer tối đa | Cọc / màu | Ghi chú |
|---|---|---|---|---|---|
| 1 | mở đầu (FTUE) | 3 | 0 | 1 | **3 chồng 18 lá**, mỗi chồng một cọc; FTUE chạm 3 chồng (GDD §6) |
| 2–4 | dễ | 3 | 0–1 | 1 | mọi cọc đều thấy, chưa có hàng cọc chờ |
| 5 | hàng cọc chờ | 4 | 1 | 1 | 2 cột ⇒ cọc chờ phía sau bắt đầu xuất hiện |
| 6 | nghỉ | 4 | 1 | 2 | |
| 7–9 | trung bình | 4–5 | 2 | 2 | (element Hidden Card Stack từ 7 — giai đoạn D) |
| 10 | khó | 5 | 2 | 2 | |
| 11 | nghỉ | 5 | 1 | 2 | |
| 12–14 | trung bình | 6 | 2 | 2 | (Hidden Color Pole từ 12) |
| 15 | khó | 6 | 3 | 2 | |
| 16 | nghỉ | 6 | 2 | 2 | |
| 17–19 | trung bình | 7 | 2–3 | 2 | (Locked Card Stack từ 17) |
| 20 | khó | 8 | 3 | 2 | |
| 21 | nghỉ | 7 | 2 | 2 | |
| 22–24 | khó | 8 | 3 | 2 | |
| 25 | khó | 8 | 4 | 2 | |
| 26 | nghỉ | 7 | 3 | 2 | |
| 27–30 | khó | 8 | 4 | 2 | |

## 5. Cách làm level: generator + solver

1. **Generator** (`LevelGenerator`, engine-free, `IRandom` có seed): làm việc theo **tập 6 lá** — mỗi màu có số tập = tổng sức chứa cọc màu đó; cắt thành chồng một màu **1–3 tập** (6 / 12 / 18 lá), xáo thứ tự, đặt lên **lưới** (bước 176 × 232, tối đa 5 × 3 ở tầng dưới); tầng trên lệch (42, 52) so với chồng tầng dưới, số chồng giảm dần lên trên (CR-011).
2. `ExpandToCards` nhân mỗi tập thành 6 lá, cọc ×6 (= 18), ô chờ 26 rãnh. 26 rãnh tràn đúng ở tập thứ 5, như ô chờ 4 tập, nên lời giải của level tập thắng level lá từng chạm (CR-006).
3. **Solver** (`LevelSolver`, DFS có ghi nhớ trạng thái): một level chỉ được nhận nếu có lời giải **không dùng booster, Revive hay RV Slot**. Ghi lại: lời giải, ô chờ tối thiểu, tỉ lệ thắng của bot chạm ngẫu nhiên.
4. **Sweep** (`LevelGenSweep`, chạy tay `PF_RUN_SLOW=1 dotnet test SkuHeadlessTests --filter FullyQualifiedName~LevelGenSweep`): mỗi level thử tới 600 seed, giữ tối đa 40 ứng viên thắng được, chọn cái có D gần D muốn nhất, ghi file level + `level-report.md`. Chạy lại cho ra file giống hệt.

> Tỉ lệ thắng của bot không phải tỉ lệ thắng của người chơi (G5). Chỉ số solver dùng để xếp thứ tự, không thay cho chơi thử.

## 6. Level FTUE

- Level 1: 3 màu, 3 cột, 3 chồng 18 lá không che nhau, mỗi chồng khớp một cọc đang hiện ⇒ mọi thứ tự chạm đều thắng; FTUE (giai đoạn F) chỉ định thứ tự chạm.
- Level 2: giữ cờ `ftue.l2` (giới thiệu ô chờ) — FTUE đang tắt (`GameplayScreen.FtueEnabled = false`), nội dung làm lại ở giai đoạn F.
