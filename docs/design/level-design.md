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
| 1 | mở đầu | 2 | 0 | 1 | Bản thử theo IMG_3750: 4 chồng × 9 lá, lưới 2×2, 2 cọc; FTUE vẫn đang tắt |
| 2–4 | dễ | 3 | 0–2 | 1 | L2: lưới 2×3; L3: lưới + chồng lệch tầng; L4: chữ I; mọi cọc đều thấy |
| 5 | hàng cọc chờ | 4 | 1 | 1 | 2 cột, cụm bài giữa + 2 cụm xoè quạt phía dưới |
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

## Bản thử bố cục level 1–5 (08-10-2026)

Theo yêu cầu trực tiếp của Phat: bám cách đặt bài trong IMG_3750. Level 1–5 được dựng thủ công, revision nội dung 4, seed 0 (không phải đầu ra generator). Giữ cọc 18 lá và ô chờ 26 rãnh; màu sắc, camera và kích thước lá theo CardSlot hiện tại. Số lá và các chồng bị che được cân bằng theo luật hiện tại, không khẳng định khớp từng lá trong video.

Trường tuỳ chọn `fan` trên một stack chỉ chọn cách vẽ xoè quạt; thiếu trường = false. Không đổi luật che phủ bằng hình chữ nhật. Codec giữ trường khi xuất lại. Level 6–30 giữ nguyên. Không chạy lại generator ghi đè 5 level thủ công này nếu chưa chủ động chọn thay thế.

Bằng chứng ảnh: `docs/captures/video-layouts-1-5/`. Lời giải lưu trong từng JSON được gate headless chạy lại. FTUE vẫn tắt; cần kiểm tra lại kịch bản FTUE khi bật về sau.

### Tăng độ xoè theo phản hồi Phat (08-10-2026)
Level 1–5 revision 5: trường tuỳ chọn `spread: true` chọn khoảng lệch 10 đơn vị mỗi lá (mặc định cũ 1.5); quạt 6 lá mở 60° thay vì 20°. Các giá trị nằm trong DesignTokens. Hình chữ nhật chồng thẳng mở rộng theo toàn bộ dải bài, dời cột L1/L2 và L5 để không chồng sai tầng. Khay tính cả góc lá xoay để chứa quạt mở rộng. `spread` thiếu/false giữ cách vẽ cũ cho level 6–30.


### Quy tắc nối chồng bài · 08-10-2026
`on_stack` (string, tùy chọn) xác định chồng dưới mà chồng này nối tiếp. Chồng dưới phải tồn tại, giao rectangle và có layer nhỏ hơn; chuỗi layer giảm nên không thể tạo vòng. Controller truy về chồng gốc và cộng số lá ban đầu của các chồng dưới thành PoseOffset. View dùng tâm, kiểu xoè, spacing và layer hình học của gốc. Chồng vàng s10 nối s8, s11 nối s9 ở level 5 revision 6.

Với quạt gốc N lá, bước góc d=min(maxStep,maxSpread/max(1,N-1)); lá thứ b của chồng nối có góc -d(N-1)/2+(PoseOffset+b)d, cùng tâm với gốc. Cao độ =22+rootLayer*60+(PoseOffset+b)*(9+1.2). Do đó lá vàng đầu tiên tiếp tục ngay sau lá đỏ trên cùng một bước góc và một bước cao độ. Quạt gốc 6 lá hiện dùng 12 độ/lá; giới hạn spread áp dụng cho chồng gốc, chuỗi nối tiếp không mở lại một quạt riêng. Chồng thẳng nối cùng bước dịch X của gốc. Chỉ số dùng số lá authored, không co quạt hoặc đổi vị trí khay khi lấy bài; layer tương tác vẫn là layer authored của từng chồng. Rectangle tiếp tục quyết định che phủ như trước.


### Rule tự động toàn game · 08-10-2026 (thay phần gán on_stack bằng tay)
Mọi lần StartLevel gọi StackPlacementRules.Resolve trên LevelData, gồm level authored và level sinh mới/ExpandToCards. Tự tìm support trong các rectangle giao nhau ở layer thấp hơn: layer cao nhất → diện tích giao lớn nhất → tâm gần nhất → id ordinal. on_stack chỉ còn là tương thích dữ liệu cũ tùy chọn, không bắt buộc. L5 revision8 đã bỏ toàn bộ on_stack và bỏ giãn tay x600 ở cột phải.

Resolver trả Root/Support/Offset; đi theo thứ tự layer rồi id, cấp dải chỉ số lá nối tiếp theo root, tránh hai nhánh cùng gốc dùng chung pose. Controller truyền tâm/kiểu xoè/bước của root; view dựng quạt hoặc chồng thẳng bằng chỉ số tuyệt đối. Root dùng style Fan/Spread của chính nó. Count authored giữ nguyên toàn bộ vị trí khi lấy bài/revive; layer tương tác từng chồng vẫn là layer authored. Renderer tự tính envelope các cụm gồm góc xoay, tilt, cao độ và viền, giãn cụm theo hướng ngắn nhất để không giao nhau, co đồng nhất phần bài để vừa khay khi cần. Bay bài bắt đầu bằng scale thực của khay. Tất cả chỉ đổi bố cục trình bày; rectangle che phủ/lời giải hiện có giữ nguyên.


Spacing toàn game cập nhật theo Phat: bước ngang18 đơn vị cho mọi level, không còn phụ thuộc spread trong JSON khi chơi. Root vẫn quyết định kiểu quạt/thẳng; chuỗi cùng root kế thừa bước. Pack/fit giữ bài trong khay.


### Chiều xoè hai hướng · 08-10-2026
spread_direction: -1 đảo chiều (chồng thẳng sang trái / quạt đảo thứ tự góc), +1 chiều thuận, 0 hoặc thiếu = tự chọn hướng ra ngoài theo tâm cụm gốc so với trung điểm hai tâm gốc ngoài cùng. Gốc nằm đúng giữa mặc định+1. Mọi chồng nối kế thừa hướng của gốc, không đổi chiều giữa chuỗi. Quy tắc xác định một lần theo dữ liệu authored nên không đổi hướng khi lấy bài. Codec validator giới hạn -1..1, ExpandToCards giữ direction. Render/pack/fit cùng áp dụng dấu, gồm bounds phía trái; layout không thay rectangle luật che phủ.


### Bố cục 5 màn đầu đối chiếu IMG_3750 · 08-10-2026
Dựa các frame t002/t016/t034/t062/t082 trong docs/reference/video-3750. L1 2×2 đỏ/xanh, xoè vào giữa; L2 2×3 vàng/đỏ/xanh; L3 sáu cặp: đỏ→vàng, vàng→đỏ, xanh→đỏ, đỏ→xanh, vàng→xanh, xanh→vàng. L4 bốn cặp ở góc nối bằng dải dọc đỏ→xanh→xanh lá (chữ I). L5 bốn cặp ở cụm trên, dải dọc vàng→xanh lá, quạt trái đỏ→vàng và phải xanh→vàng hướng vào nhau. Dùng 9 lá/chồng ở L1/2, 6 ở L3–5; cân lại mục tiêu theo tổng lá trong bố cục. Đây là lựa chọn nội dung để dựng màu/bố cục từ video, không khẳng định đếm chính xác từng lá nhỏ trong clip.

Thêm spread_angle [-180,180] vào rule chung: 0 xoè ngang, 90 xoè dọc xuống, hướng âm/dương được nhân spread_direction; mặt lá vẫn thẳng đứng. Resolver chỉ nối các chồng cùng trục; dải khác trục bắt đầu root riêng. View pack giữ giao cắt có chủ ý giữa các root khác trục/khác tầng, còn cụm song song vẫn tránh giao nhau. Frame bounds tính theo dx/dy thực tế; H/W dữ liệu che phủ không kéo dài khay. Codec, ExpandToCards bảo toàn trục. Không cần on_stack thủ công.
