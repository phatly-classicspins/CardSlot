# CardSlot — Game Design Document

> v1.0 · Bước 1 · đã duyệt (Phat, 06-10-2026), chưa duyệt. Đầu vào: `concept.md` v0.2 (đã duyệt), D-006…D-014.
> ID chuẩn: `glossary.md`. Đi kèm: `level-design.md`, `economy-sheet.md`, `screen-inventory.md`, `features/*.md`.
> Các mục **[XÁC NHẬN]** do AI đề xuất đã được duyệt cùng GDD (D-015).

## 1. Tổng quan

Puzzle một chạm, màn dọc 1080×1920. Người chơi chạm các `stack` đang `open` trên `board`. `run` trên cùng bay lên `target` cùng màu; lá không có đích thì vào `buffer`. Dọn hết bài là thắng; `buffer` tràn là thua. MVP có 30 level tuyến tính (D-010).

## 2. Luật lõi

Mọi luật dưới đây là luật của **model thuần** (không phụ thuộc engine, G16). Mỗi luật có mã `R-x` để test tham chiếu.

### 2.1 Trạng thái một level

| Thành phần | Nội dung |
|---|---|
| `board` | Danh sách `stack`: `{id, x, y, w, h, layer, cards[]}`. `cards[0]` là lá trên cùng. |
| `target_slot` | `n_slots` vị trí (mặc định 3); mỗi vị trí trống hoặc chứa `{color, capacity, filled}`. |
| `target_queue` | Các `target` chưa xuất hiện, theo thứ tự. |
| `buffer` | Danh sách lá theo thứ tự vào (FIFO); `buffer_capacity` lá. |
| Kết quả | `playing` · `won` · `lost` |

- **R-1 Hợp lệ khi bắt đầu.** Với mỗi màu, tổng số lá trên `board` = tổng `capacity` của mọi `target` màu đó. Level vi phạm sẽ bị loader từ chối.
- **R-2 Khởi tạo.** `n_slots` `target` đầu của `target_queue` lấp các `target_slot` 0..n−1 theo thứ tự; `buffer` rỗng.

### 2.2 Chạm được hay không

- **R-3** Một `stack` là `covered` nếu có `stack` khác còn lá, có `layer` **lớn hơn**, và hình chữ nhật hai chồng giao nhau với diện tích > 0.
- **R-4** `tap` chỉ hợp lệ trên `stack` còn lá, không `covered`, khi kết quả là `playing`. `tap` không hợp lệ thì bị bỏ qua: không đổi trạng thái, không tính là nước đi.
- **R-5** `stack` hết lá thì bị xoá khỏi `board`; các chồng bên dưới có thể trở thành `open`.

### 2.3 Xử lý một `tap`

- **R-6** Lấy `run` của `stack` được chạm (mọi lá cùng màu liền nhau tính từ trên cùng) ra khỏi `stack`.
- **R-7** Đặt **từng lá** của `run`, lần lượt từ lá trên cùng. Với mỗi lá:
  1. Nếu có `target` cùng màu còn chỗ (`filled < capacity`), lá vào `target` đó. Có nhiều `target` như vậy thì chọn `target_slot` **có số nhỏ nhất** (trái nhất).
  2. Nếu không có, lá vào cuối `buffer`. Nếu `buffer` đã có đủ `buffer_capacity` lá thì xảy ra `overflow`: kết quả là `lost`, dừng xử lý.
  3. Chạy `settle` (§2.4) rồi mới đặt lá kế tiếp.
- **R-8** Model xử lý một `tap` **tức thì và trọn vẹn**. Hoạt ảnh chỉ là hiển thị lại kết quả. [XÁC NHẬN] Trong lúc hoạt ảnh chạy, người chơi vẫn được chạm tiếp; các `tap` được xử lý theo thứ tự.

### 2.4 `settle`

Lặp cho tới khi không còn gì thay đổi:

- **R-9** Mỗi `target` có `filled == capacity` thì hoàn thành: xoá khỏi `target_slot`. Nếu `target_queue` còn, `target` kế tiếp lấp đúng vị trí đó.
- **R-10** Với mỗi `target` còn chỗ, kéo các lá cùng màu trong `buffer` theo thứ tự FIFO vào cho tới khi `target` đầy hoặc `buffer` hết lá màu đó. Xét các `target` theo thứ tự `target_slot` tăng dần.

### 2.5 Thắng và thua

- **R-11 Thắng** khi `board` rỗng **và** `buffer` rỗng. Theo R-1, lúc đó mọi `target` đã hoàn thành. Kiểm tra sau mỗi `tap`.
- **R-12 Thua** chỉ do `overflow` (R-7.2). Không giới hạn thời gian hay số nước (D-014).
- **R-13 Bế tắc.** Nếu `buffer` chưa đầy nhưng không còn `stack` nào chạm được, mà chưa thắng (chỉ xảy ra với level lỗi), model báo `lost`. Solver phải loại các level như vậy.
- **R-14** Không có sao hay điểm số. Thắng là qua level và nhận xu (`economy-sheet.md`).

### 2.6 Ngẫu nhiên

- **R-15** Gameplay **không có yếu tố ngẫu nhiên**: level là dữ liệu cố định, kết quả chỉ phụ thuộc chuỗi `tap`. `IRandom` chỉ dùng trong generator level (offline), có seed và log seed (rule #14).

### 2.7 Booster và Continue (luật ở model)

- **R-16 `booster_undo`.** Khôi phục trạng thái ngay trước `tap` gần nhất, gồm cả mọi `settle` do `tap` đó gây ra, **trừ** `buffer_capacity`: chỗ đã cộng từ Extra Space hay Continue được giữ nguyên. Chỉ lưu lịch sử của `attempt` hiện tại. Không dùng được khi chưa có `tap` nào, hoặc khi kết quả không phải `playing`. Có thể dùng nhiều lần liên tiếp, lùi được tới đầu level. [XÁC NHẬN]
- **R-17 `booster_add_slot`.** `buffer_capacity += add_slot_amount` (mặc định +4) cho tới hết `attempt`. Dùng tối đa `add_slot_max_per_attempt` lần (mặc định 1). [XÁC NHẬN]
- **R-18 `continue`.** Khi `lost` do `overflow`: `buffer_capacity += continue_slot_amount` (mặc định +4), lá gây `overflow` vào `buffer`, phần còn lại của `run` tiếp tục được đặt (R-7), kết quả quay về `playing`. Tối đa 1 lần mỗi `attempt`.

## 3. Thiết kế level

Xem `level-design.md`: 30 level, đường cong răng cưa, generator + solver.

## 4. Tính năng (MVP, D-010)

| Tính năng | Spec |
|---|---|
| Gameplay lõi + HUD | `features/core-gameplay.md` |
| Booster (Undo, Extra Space) | `features/boosters.md` |
| Thua → Continue | `features/continue.md` |
| Thắng, tiến trình level | `features/level-progression.md` |
| FTUE | `features/ftue.md` |
| Tạm dừng, Cài đặt | `features/pause-settings.md` |
| Quảng cáo giả lập | `features/ads.md` |

**Không có trong MVP:** shop, IAP, bản đồ level, daily reward, nhiệm vụ, hồ sơ, bảng xếp hạng, bộ sưu tập, sự kiện.

## 5. Tài khoản và lưu game

- **Nhận diện:** chỉ khách, lưu trên máy, không đăng nhập, không cloud save (giả định từ D-010, [XÁC NHẬN]).
- **Lần chạy đầu:** tạo save mặc định, vào Home, nút Play mở Level 1 (bắt đầu FTUE).
- **Cài lại / đổi máy:** mất tiến trình. Chấp nhận được cho MVP.
- **Offline:** chơi được hoàn toàn offline; quảng cáo giả lập không cần mạng.
- **Save v1** (qua lớp save và migrator của framework, rule #2):

| Trường | Kiểu | Mặc định |
|---|---|---|
| `schema_version` | int | 1 |
| `current_level` | int (1..30) | 1 |
| `max_level_reached` | int | 1 |
| `coins` | int ≥ 0 | `start_coins` |
| `boosters.undo` / `boosters.add_slot` | int ≥ 0 | 0 |
| `boosters_unlocked` | set | rỗng |
| `ftue.completed_steps` | set | rỗng |
| `settings.sound` / `music` / `haptics` | bool | true |
| `ads.wins_since_interstitial` | int | 0 |

- **Không lưu giữa level.** App bị tắt giữa level thì lần sau chơi lại level đó từ đầu; booster đã tiêu trong `attempt` dở dang **không được hoàn lại**. Vì số booster bị trừ và lưu ngay lúc dùng (G18), hoàn lại sẽ thành lỗ hổng gian lận.
- Mọi thay đổi bền vững (xu, booster, level) là một commit nguyên tử: đổi dữ liệu → lưu → lưu lỗi thì khôi phục (G18).

## 6. FTUE

Xem `features/ftue.md`. Tóm tắt: Level 1 có bàn tay chỉ vào chồng cần chạm, 2 bước, không skip. Level 2 giới thiệu `buffer`. Booster mở khoá kèm popup ở Level 4 (Undo) và Level 7 (Extra Space), mỗi loại tặng 2 lượt.

## 7. Kinh tế

Xem `economy-sheet.md`. Chỉ có một loại tiền là `coin`. Trong MVP không có gì mua bằng tiền thật (D-007, D-010).

## 8. Luồng UI và màn hình

Xem `screen-inventory.md`. Luồng chính:

```
Boot → Home ──Play──▶ Gameplay ──thắng──▶ [Win] ──Next──▶ Gameplay (level+1)
                        │  ▲                 └──Home──▶ Home
                        │  └──Continue──┐
                        ├──thua──▶ [Lose: offer] ──No thanks──▶ [Lose: failed] ──Retry──▶ Gameplay
                        └──II──▶ [Pause] ──Settings──▶ [Settings]
                                    ├──Restart──▶ Gameplay (cùng level)
                                    └──Home──▶ Home
```

## 9. Âm thanh

| ID | Khi nào | Ưu tiên |
|---|---|---|
| `sfx_tap` | `tap` hợp lệ | cao |
| `sfx_tap_invalid` | chạm vào chồng bị che | thấp |
| `sfx_card_land_target` | một lá chạm tới đích (cao độ tăng dần trong một `run`) | cao |
| `sfx_card_land_buffer` | lá vào `buffer` | trung bình |
| `sfx_target_complete` | `target` đầy, nổ | cao |
| `sfx_buffer_warning` | `buffer` còn ≤ 2 chỗ | trung bình |
| `sfx_win` / `sfx_lose` | kết thúc level | cao |
| `sfx_coin` | cộng xu | trung bình |
| `sfx_booster` | dùng booster | trung bình |
| `sfx_ui_click` | nút UI | thấp |
| `music_home` / `music_gameplay` | nhạc nền, lặp | thấp |

Rung (`haptics`): nhẹ khi `target` hoàn thành, vừa khi thua.

## 10. Text

- **Giọng văn:** ngắn, vui, động từ trước ("Tap to send cards!"). Không dùng tiếng lóng.
- **Key:** `<màn>.<phần tử>[.<biến thể>]`, chữ thường, ví dụ `home.play`, `win.title`, `ftue.l1.step1`. Mọi text đi qua `LocKey` (D-008, rule #4).
- **Độ dài tối đa:** tiêu đề 18 ký tự, nút 12 ký tự, dòng hướng dẫn 40 ký tự.
- Danh sách key nằm trong từng spec ở `features/`.

## 11. Kiếm tiền và quảng cáo (giả lập, D-007)

- **Rewarded:** Continue khi thua; nhận 1 booster khi hết booster (nút "Watch ad" thay cho giá xu).
- **Interstitial:** sau khi bấm Next ở màn Win, từ Level 5 trở đi, cứ 2 lần thắng một lần, cách nhau tối thiểu 60 giây. Không hiện sau khi thua.
- Phần thưởng của rewarded chỉ được trao khi quảng cáo báo **hoàn thành**. Đóng hoặc huỷ thì không có thưởng (G19).

## 12. Sự kiện analytics

| Sự kiện | Tham số |
|---|---|
| `level_started` | `level_index`, `attempt_no` |
| `level_won` | `level_index`, `taps`, `duration_s`, `boosters_used`, `continued` |
| `level_failed` | `level_index`, `taps`, `duration_s`, `cards_left` |
| `level_quit` | `level_index`, `taps` |
| `booster_used` | `booster_id`, `level_index`, `source` (`inventory`/`coins`/`ad`) |
| `continue_used` | `level_index`, `source` (`coins`/`ad`) |
| `ad_rewarded_completed` / `ad_interstitial_shown` | `placement`, `level_index` |
| `coins_changed` | `delta`, `reason`, `balance` |

Các sự kiện đều ở thì quá khứ, là sự thật đã xảy ra (rule #1).

## 13. Trường hợp biên

| Trường hợp | Xử lý |
|---|---|
| App bị tắt giữa level | Chơi lại level từ đầu; booster đã tiêu không hoàn lại. |
| App bị tắt khi đang nhận thưởng thắng | Xu và `current_level` được lưu **trước** khi hiện dialog Win (G19). Mở lại thì đã nhận thưởng, không nhận lần hai. |
| App bị tắt giữa rewarded ad | Không có callback hoàn thành nên không có thưởng; level chơi lại từ đầu. |
| Lưu lỗi | Khôi phục trạng thái trong bộ nhớ về trước thay đổi, ghi log lỗi, không hiện thưởng (G18). |
| Đổi giờ máy | Không ảnh hưởng: MVP không có tính năng theo thời gian. Khoảng cách interstitial dùng đồng hồ đơn điệu trong phiên. |
| Chạm nhiều ngón cùng lúc | Chỉ nhận một `tap` mỗi khung hình (con trỏ đầu tiên). |
| Hết 30 level | Màn Win của Level 30 hiện "More levels coming soon"; Play ở Home mở lại Level 30. [XÁC NHẬN] |
| Không đủ xu cho booster/Continue | Nút giá xu bị khoá; vẫn còn lựa chọn xem quảng cáo. |
