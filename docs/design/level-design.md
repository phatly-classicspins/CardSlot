# Thiết kế level — CardSlot

> v1.0 · Bước 1 · đã duyệt (Phat, 06-10-2026). Luật: `GDD.md` §2. Con số: `economy-sheet.md`.

## 1. Cấu trúc

- 30 level tuyến tính, không chia chapter (D-010).
- Một level dài 30–90 giây, khoảng 8–30 `tap`.
- Mỗi level là một file dữ liệu có tên, revision nội dung và các trường có khoảng hợp lệ (G20). Không viết cứng level trong code.

## 2. Dữ liệu một level

```json
{
  "id": "level_007",
  "revision": 1,
  "seed": 918273,
  "n_slots": 3,
  "buffer_capacity": 12,
  "targets": [ {"color": "color_0", "capacity": 3}, {"color": "color_1", "capacity": 3} ],
  "stacks": [ {"id": "s0", "x": -300, "y": -500, "w": 180, "h": 240, "layer": 0,
               "cards": ["color_0", "color_0", "color_1"]} ],
  "ftue": null
}
```

- Toạ độ là **đơn vị thế giới** trên cùng thước của rig (1 px tham chiếu = 1 đơn vị), đặt trong vùng `board` của safe rect. Hình chữ nhật dùng cho R-3 (che phủ).
- `seed` là seed của generator đã sinh ra level, giữ lại để tái tạo.

| Trường | Khoảng hợp lệ |
|---|---|
| `n_slots` | 2–4 |
| `buffer_capacity` | 6–20 |
| `targets[].capacity` | 2–6 |
| số màu | 2–6 |
| số `stack` | 2–24 |
| lá mỗi `stack` | 1–8 |
| `layer` | 0–4 |

## 3. Các yếu tố tạo độ khó

| Yếu tố | Dễ → khó |
|---|---|
| Số màu | 2 → 6 |
| Độ sâu che phủ (số `layer`) | 0 → 4 |
| `buffer_capacity` | 16 → 8 |
| Độ lẫn màu trong một `stack` | `run` dài 3–4 lá → `run` 1–2 lá xen kẽ |
| Thứ tự `target_queue` | khớp với lá đang lộ → lệch, buộc người chơi phải nhìn trước |
| `n_slots` | 3 (2 ở Level 1–2) |

## 4. Đường cong độ khó

Dạng **răng cưa**: tăng dần trong mỗi nhóm 5 level, level thứ 5 là level khó, level ngay sau là level nghỉ.

```
khó │                    ▲        ▲        ▲
    │          ▲       ╱ │      ╱ │      ╱ │
    │        ╱ │     ╱   │    ╱   │    ╱   │
    │  ▲   ╱   │   ╱     │  ╱     │  ╱     │
    │╱ │ ╱     │ ╱       │╱       │╱       │
dễ  └──┴───────┴─────────┴────────┴────────┴──
     1  5      10        15       20  25   30   level
```

| Level | Vai trò | Màu | Layer | Buffer | Cơ chế mới |
|---|---|---|---|---|---|
| 1 | FTUE: chạm | 2 | 0 | 16 | `tap`, `target` |
| 2 | FTUE: ô tạm | 2 | 0 | 14 | `buffer` |
| 3 | dễ | 3 | 0 | 14 | `target_queue` (đích thay mới) |
| 4 | dễ | 3 | 1 | 14 | che phủ; mở `booster_undo` |
| 5 | **khó** | 3 | 1 | 12 | |
| 6 | nghỉ | 3 | 1 | 14 | |
| 7 | trung bình | 4 | 1 | 12 | mở `booster_add_slot` |
| 8–9 | trung bình | 4 | 2 | 12 | |
| 10 | **khó** | 4 | 2 | 10 | |
| 11 | nghỉ | 4 | 1 | 12 | |
| 12–14 | trung bình | 5 | 2 | 11 | `run` ngắn xen kẽ |
| 15 | **khó** | 5 | 3 | 10 | |
| 16 | nghỉ | 4 | 2 | 12 | |
| 17–19 | trung bình | 5 | 3 | 10 | |
| 20 | **khó** | 5 | 3 | 9 | |
| 21 | nghỉ | 5 | 2 | 11 | |
| 22–24 | khó | 6 | 3 | 10 | |
| 25 | **khó** | 6 | 4 | 9 | |
| 26 | nghỉ | 5 | 3 | 11 | |
| 27–29 | khó | 6 | 4 | 9 | |
| 30 | **khó (cuối)** | 6 | 4 | 8 | |

## 5. Cách làm level: generator + solver

1. **Generator** (engine-free, `IRandom` có seed): sinh bố cục `stack`, chia lá theo màu, dựng `target_queue` sao cho thoả R-1, theo tham số của bảng §4.
2. **Solver** (engine-free, chạy bằng `dotnet test`): tìm kiếm trên chuỗi `tap` (DFS có ghi nhớ trạng thái). Một level chỉ được nhận nếu:
   - có ít nhất một lời giải **không dùng booster**;
   - không có bế tắc (R-13) trên đường đi tối ưu.
3. **Chỉ số độ khó** solver ghi lại cho từng level: số `tap` tối thiểu, số `buffer` cao nhất trên lời giải tốt nhất, tỉ lệ nước đi dẫn tới thua (bot chọn ngẫu nhiên, N lần), số lời giải.
4. Người duyệt chơi thử và **chọn tay** 30 level từ các ứng viên. Generator có thể được chỉnh lại; dữ liệu level đã ship thì có revision mới, không sửa lặng lẽ.

> Tỉ lệ thắng của bot không phải tỉ lệ thắng của người chơi (G5). Chỉ số solver dùng để xếp thứ tự, không thay cho chơi thử ở Bước 8.

## 6. Level FTUE

- Level 1: 2 màu, 4 `stack` không che phủ, mỗi `stack` là một `run` duy nhất. Mọi chuỗi chạm đều thắng.
- Level 2: có đúng một `run` buộc phải vào `buffer` trước khi đích của nó xuất hiện, để người chơi thấy ô tạm hoạt động.
