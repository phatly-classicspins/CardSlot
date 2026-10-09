# CardSlot — Game Design Document

> **v2.0 · nháp, chờ Phat duyệt** · CR-012: làm theo GDD anh Tánh (https://hvtanh07.github.io/Card-Slot-GDD/, bản chụp `reference-gdd-tanh.md`). Chỗ GDD đó chưa nói thì giữ v1.7 (CR-012 mục 14).
> v1.x đã duyệt: v1.0 (D-015) … v1.7 (CR-010). Lịch sử ở cuối file.
> ID chuẩn: `glossary.md`. Đi kèm: `level-design.md`, `economy-sheet.md`, `screen-inventory.md`, `features/*.md` — các tài liệu này lên v2 theo từng giai đoạn của CR-012.
> **[XÁC NHẬN]** = AI đề xuất ở chỗ GDD anh Tánh chưa nói; cần Phat duyệt cùng v2.0. Chỗ nên hỏi lại anh Tánh ghi **[HỎI TÁNH]**.
> Tên trong code / level data giữ ID cũ để không đổi hàng loạt: `target` = **Pole** (cọc), `target_slot` = cọc ở đáy hàng, `target_queue` = hàng cọc, `buffer` = **Waiting slots** (ô chờ).

## 1. Tổng quan

Puzzle một chạm, màn dọc 1080×1920. Người chơi chạm các `stack` đang `open` trên `board`; cả chồng bay lên `target` cùng màu đang ở đáy hàng, không có thì vào `buffer`. Lá trong `buffer` tự bay lên khi có `target` cùng màu. Dọn hết bài và hết cọc là thắng. MVP có 30 level tuyến tính (D-010); từ level 5 mở dần 3 booster và 3 element (§4.1).

## 2. Luật lõi

Mọi luật dưới đây là luật của **model thuần** (không phụ thuộc engine, G16). Mỗi luật có mã `R-x` để test tham chiếu. Mã đã bỏ giữ nguyên số, gạch đi, không dùng lại.

### 2.1 Trạng thái một level

| Thành phần | Nội dung |
|---|---|
| `board` | Danh sách `stack`: `{id, x, y, w, h, layer, color, count, hidden?, lock?}`. **Mỗi chồng một màu** (v2.0). |
| `target_slot` | `n_slots` vị trí (mặc định 3) = cọc ở đáy mỗi cột; mỗi vị trí trống hoặc chứa `{color, capacity, filled, hidden?}`. |
| `target_queue` | Hàng cọc chờ, chia theo cột (R-2). |
| `buffer` | Danh sách lá theo thứ tự vào (FIFO); `buffer_capacity` lá. |
| `pending` | Lá của lần chạm đang dở khi `overflow` (R-7.2); rỗng khi đang chơi bình thường. |
| Kết quả | `playing` · `won` · `lost` (lý do: `overflow` hoặc `stuck`) |

- **R-1 Hợp lệ khi bắt đầu.** Với mỗi màu, tổng số lá trên `board` = tổng `capacity` của mọi `target` màu đó. Level vi phạm bị loader từ chối.
- **R-1b** Hai `stack` cùng `layer` không được giao nhau (diện tích > 0). Level vi phạm bị loader từ chối.
- **R-1c** (v2.0) Mỗi `stack` chỉ có **một màu** (`color`) và `count ≥ 1` lá. Thay mảng `cards[]` nhiều màu của v1.x.
- **R-2 Khởi tạo.** `target` thứ i thuộc cột i mod `n_slots`. `target` đầu mỗi cột vào `target_slot` của cột đó; các `target` sau đứng chờ phía sau trong cùng cột; `buffer` rỗng.

### 2.2 Chạm được hay không

- **R-3** (v2.0) Chồng A là `covered` nếu có chồng B còn lá, `layer` **lớn hơn**, hình chữ nhật giao nhau với diện tích > 0, **và B khác màu A**. Chồng cùng màu nằm trên không che. **[XÁC NHẬN]** Chồng Hidden (R-24) đang giấu màu thì che mọi chồng bên dưới, bất kể màu — để người chơi không đoán ra màu thật qua việc chồng dưới chạm được hay không.
- **R-4** `tap` chỉ hợp lệ trên chồng còn lá, không `covered`, không bị khoá (R-26), khi kết quả là `playing`. `tap` không hợp lệ bị bỏ qua: không đổi trạng thái, không tính là nước đi.
- **R-5** Chồng hết lá thì bị xoá khỏi `board`; chồng bên dưới có thể thành `open`.

### 2.3 Xử lý một `tap`

- **R-6** (v2.0) Lấy **cả chồng** được chạm (mọi lá, cùng màu). Bỏ `max_run` của CR-008.
- **R-7** Đặt **từng lá**, lần lượt. Với mỗi lá:
  1. Có `target` cùng màu trong `target_slot` còn chỗ (`filled < capacity`) thì vào đó; nhiều cái thì chọn `target_slot` trái nhất. Cọc trong hàng chờ phía sau không nhận lá.
  2. Không có thì vào cuối `buffer`. `buffer` đã đủ `buffer_capacity` lá thì xảy ra `overflow`: lá này và các lá chưa đặt vào `pending`, kết quả `lost` (`overflow`), dừng xử lý (CR-012 mục 4 = tràn là thua).
  3. Chạy `settle` (§2.4) rồi mới đặt lá kế tiếp.
- **R-8** Model xử lý một `tap` **tức thì và trọn vẹn**; hoạt ảnh chỉ hiển thị lại kết quả. Trong lúc hoạt ảnh chạy người chơi vẫn được chạm tiếp; các `tap` được xử lý theo thứ tự.

### 2.4 `settle`

Lặp cho tới khi không còn gì thay đổi:

- **R-9** Mỗi `target` có `filled == capacity` thì hoàn thành: xoá khỏi `target_slot`; `target` đứng sau trong cùng cột tiến lên lấp chỗ. Mỗi lần một `target` hoàn thành (kể cả do Remove, R-19) thì chạy R-26 (đếm khoá).
- **R-10** Với mỗi `target` còn chỗ, kéo các lá cùng màu trong `buffer` theo FIFO vào tới khi `target` đầy hoặc `buffer` hết lá màu đó. Xét `target_slot` từ trái sang phải.

### 2.5 Thắng và thua

- **R-11 Thắng** khi `board`, `buffer` rỗng **và** mọi `target` đã hoàn thành. Kiểm tra sau mỗi `tap` và sau mỗi booster.
- **R-12 Thua do tràn**: `overflow` (R-7.2).
- **R-13** (v2.0) **Thua do kẹt** (`stuck`): chưa thắng và **mọi** nước còn lại đều không cứu được — không còn `tap` hợp lệ nào, hoặc mọi `tap` hợp lệ đều dẫn tới `overflow`. Đây là điều kiện thua của GDD anh Tánh ("ô chờ đầy, không lá nào khớp cọc đang nhận"). Model tự kiểm tra sau mỗi `tap`; booster không tính là nước đi (người chơi vẫn được dùng booster ở màn thua).
- **R-14** Không có sao hay điểm số. Thắng là qua level và nhận xu (§7).

### 2.6 Ngẫu nhiên

- **R-15** (v2.0) Bố cục level là dữ liệu cố định. Yếu tố ngẫu nhiên duy nhất lúc chơi là **Shuffle** (R-21). Model nhận `IRandom` qua constructor; seed sinh ở điểm composition mỗi `attempt` và được log (rule #14). Generator level (offline) cũng dùng `IRandom` có seed.

### 2.7 Booster (luật ở model)

Booster dùng được khi kết quả là `playing`, hoặc ở màn thua (khi đó nếu booster làm hết kẹt thì kết quả về `playing`). Booster không tính là `tap`.

- ~~**R-16 Undo**~~, ~~**R-17 Extra Space**~~ (bỏ ở CR-005; v2.0 không dùng lại).
- ~~**R-18 Continue**~~ (bỏ ở v2.0, thay bằng Revive R-22 và RV Slot R-23 — code xong ở giai đoạn B).
- **R-19 Remove** (mở ở level 10). Người chơi chọn **bất kỳ** `target` nào — ở đáy hoặc trong hàng chờ, kể cả Hidden (lộ màu ngay). `target` đó lấy lá cùng màu cho tới đầy, theo thứ tự:
  1. (D-035) `pending` rồi `buffer` (FIFO) — GDD chỉ nói "từ bàn"; lấy ô chờ trước vì đó là chỗ người chơi đang kẹt.
  2. Chồng thường không bị che → chồng thường bị che → chồng Hidden → chồng Locked (bị lấy thì mở khoá ngay).
  Trong cùng một bậc: `layer` cao trước, rồi `id`. Lấy một phần chồng thì chồng giữ phần còn lại. Theo R-1 luôn đủ lá để cọc đầy. Rồi `target` hoàn thành (R-9); cọc phía sau trong cùng cột dồn lên. Chọn cọc ở giữa hàng thì các cọc sau nó dồn lên một chỗ.
- **R-20 Hand** (mở ở level 5). Người chơi chọn **bất kỳ** chồng nào còn lá, kể cả bị che, Hidden (lộ màu) hay Locked (mở khoá ngay); cả chồng được gửi lên như R-6, R-7.
- **R-21 Shuffle** (mở ở level 8). Lần lượt:
  1. Chọn tối đa 3 màu: theo thứ tự lá trong `buffer` (FIFO, mỗi màu tính một lần); thiếu thì lấy thêm màu từ chồng `open` (`layer` cao trước, rồi `id`) (D-035).
  2. Đưa cọc của các màu đó xuống đáy hàng: mỗi `target_slot` có cọc **chưa nhận lá nào** (`filled == 0`) được thay bằng một cọc của màu đã chọn, ưu tiên cọc thường rồi mới tới cọc Hidden. (D-035) Cọc đã có lá (`filled > 0`) giữ nguyên chỗ, không bị xáo — xáo nó sẽ làm mất lá đã xếp.
  3. Xáo ngẫu nhiên (R-15) các cọc còn lại có `filled == 0`; (D-035) mỗi cột giữ nguyên số cọc.
  Sau đó `settle`.

### 2.8 Revive và RV Slot

- **R-22 Revive.** Khi `lost`: dùng **Remove miễn phí** (R-19) lên **2** `target_slot` đang có ít lá nhất (`filled` nhỏ nhất; bằng nhau thì trái trước). Đặt lại `pending` theo R-7. Nếu vẫn kẹt (R-13) hoặc lại tràn thì Remove miễn phí thêm 1 cọc nữa, lặp tới khi chơi được hoặc thắng. Không trừ Remove của người chơi, không cần sở hữu Remove. Trả bằng xu hoặc quảng cáo thưởng; giá và giới hạn ở §7.
- **R-23 RV Slot.** Xem quảng cáo thưởng: `buffer_capacity += 8`; tối đa **2** lần mỗi `attempt` **[XÁC NHẬN: GDD ghi "mỗi level"; mình hiểu là mỗi lượt chơi, chơi lại thì có lại 2 lần]**. Dùng được lúc đang chơi và ở màn thua do tràn (khi đó `pending` được đặt lại theo R-7).

### 2.9 Element (luật ở model)

- **R-24 Hidden Card Stack** (từ level 7). Chồng có `hidden: true` hiện mặt úp, không lộ màu khi đang `covered`; vừa hết bị che thì lộ màu thật, mãi mãi. Hand chọn được (lộ màu khi gửi), Remove lấy được.
- **R-25 Hidden Color Pole** (từ level 12). Cọc có `hidden: true` không lộ màu khi còn trong hàng chờ; vào `target_slot` thì lộ màu. Shuffle ưu tiên cọc thường trước cọc Hidden.
- **R-26 Locked Card Stack** (từ level 17). Chồng có `lock: n` (n ≥ 1): hiện màu, không chạm được, vẫn che như chồng thường (R-3).
  - Bộ đếm bắt đầu **khi chồng hết bị che lần đầu**; cọc hoàn thành trước đó không tính.
  - Sau đó mỗi `target` hoàn thành (kể cả do Remove, Revive) thì `lock −= 1`; về 0 thì mở khoá.
  - Nếu mọi `target` còn lại (ở đáy và trong hàng chờ) đều có màu của các chồng đang khoá thì các chồng đó mở khoá ngay.
  - Hand chọn được (mở khoá ngay và gửi lên); Remove lấy tới thì mở khoá ngay.
  - **[XÁC NHẬN]** Locked đang bị che thì vẫn hiện bộ đếm, nhưng số chưa giảm.

## 3. Thiết kế level

- Giữ của v1.7: 30 level, **3 cột**, cọc chứa **18 lá**, ô chờ **26** rãnh, sinh theo lưới (CR-011), level đầu dễ.
- **Kích thước chồng [XÁC NHẬN, HỎI TÁNH]:** chồng một màu, 6 / 12 / 18 lá (bội của 6, như video tham khảo IMG_3748 thấy nhóm 6 lá).
- **8 màu** (thêm `color_6` Pink, `color_7` Cyan vào `glossary.md`): level 1–4 dùng 2–3 màu, tăng dần, từ level 20 dùng tới 7–8 màu **[XÁC NHẬN]**.
- Element xuất hiện từ level mở khoá trở đi: level mở khoá có đúng một element đó, dễ, để người chơi học; các level sau trộn dần.
- Solver phải chứng minh mỗi level thắng được **không cần booster, Revive hay RV Slot** (giữ chuẩn v1.x).
- Định dạng JSON level v3: `level-design.md` v2 (giai đoạn A). Chưa phát hành nên không cần migrator cho level.

## 4. Tính năng (MVP)

| Tính năng | Spec | Giai đoạn CR-012 |
|---|---|---|
| Gameplay lõi + HUD | `features/core-gameplay.md` v2 | A |
| Revive, RV Slot | `features/revive.md` (thay `continue.md`) | B — xong, chờ duyệt |
| Xu, booster Hand / Shuffle / Remove, mua booster | `features/boosters.md` v2, `economy-sheet.md` v2 | C |
| Element + thanh tiến độ mở element | `features/elements.md` (mới) | D |
| Tim | `features/lives.md` (mới) | E |
| FTUE, T&C, popup mở khoá | `features/ftue.md` v2 | F |
| Âm thanh, rung, remote config | §9, `economy-sheet.md` v2 | G |
| Thắng, tiến trình level | `features/level-progression.md` | C (thêm xu, nhân thưởng) |
| Tạm dừng, Cài đặt | `features/pause-settings.md` | E (Restart / Home trừ tim) |
| Quảng cáo giả lập | `features/ads.md` | B, C |

**Không có trong MVP:** shop riêng, IAP, bản đồ level, daily reward, nhiệm vụ, hồ sơ, bảng xếp hạng, bộ sưu tập, sự kiện.

### 4.1 Thứ tự mở khoá

| Level | Mở | Loại | Quà |
|---|---|---|---|
| 1 | Hướng dẫn đầu tiên | FTUE | — |
| 5 | Hand | Booster | 3 |
| 7 | Hidden Card Stack | Element | — |
| 8 | Shuffle | Booster | 3 |
| 10 | Remove | Booster | 3 |
| 12 | Hidden Color Pole | Element | — |
| 17 | Locked Card Stack | Element | — |

Mở khoá khi **bắt đầu** level đó lần đầu (popup trước khi chơi).

### 4.2 Thanh tiến độ mở element (màn Win)

- Thanh ngang tô xanh, giữa ghi `{current}/{total}`; ô vuông bên phải hiện icon element sắp mở.
- `total = level mở kế tiếp − level mở trước đó` (chưa có thì lấy level 1) · `current = level vừa thắng − level mở trước đó + 1`.
- **[XÁC NHẬN]** Mốc tính là **3 element** (7, 12, 17) như tên mục trong GDD anh Tánh; booster không tính. Thắng từ level 17 trở đi thì không còn mốc ⇒ ẩn cả cụm, căn giữa phần còn lại.
- Ví dụ: sau level 1 → 1/6, …, sau level 6 → 6/6; sau level 7 → 1/5.

### 4.3 Tim

- Tối đa **5**, hồi **1 tim / 20 phút** khi dưới 5; đủ 5 thì dừng đếm.
- Mất 1 tim khi một lượt chơi kết thúc không thắng: thua rồi chơi lại · tự Restart · thoát về Home (từ Pause hoặc màn thua).
- **[XÁC NHẬN]** Cách làm: **trừ 1 tim lúc bắt đầu lượt**, **trả lại khi thắng**. Kết quả giống GDD, và tắt app giữa level cũng mất tim (không lách được bằng cách tắt app).
- Hết tim: bấm Play / Retry thì hiện dialog **Out of Lives**: đếm ngược tới tim kế tiếp + nút xem quảng cáo nhận 1 tim **[XÁC NHẬN, GDD chưa nói]**.
- Giờ: lưu mốc UTC bắt đầu hồi; nếu giờ máy lùi về trước mốc thì đặt lại mốc = bây giờ (không cộng tim).

## 5. Tài khoản và lưu game

- Chỉ khách, lưu trên máy, không cloud save; chơi offline được (quảng cáo giả lập).
- **Lần chạy đầu:** tạo save mặc định → vào Level 1 → dialog Terms & Conditions / Privacy Policy → FTUE.
- **Save v2** (chưa phát hành ⇒ đổi thẳng, không migrator, như CR-005):

| Trường | Kiểu | Mặc định |
|---|---|---|
| `schema_version` | int | 2 |
| `current_level` | int (1..30) | 1 |
| `highest_cleared` | int (0..30) | 0 |
| `attempts` | int ≥ 0 | 0 |
| `coins` | int ≥ 0 | `economy.start_coins` |
| `boosters.hand` / `.shuffle` / `.remove` | int ≥ 0 | 0 (quà mở khoá cộng vào) |
| `unlocked` | set (`booster_*`, `element_*`) | rỗng |
| `lives` | int 0..5 | 5 |
| `lives_refill_from_utc` | long (0 = không đếm) | 0 |
| `terms_accepted` | bool | false |
| `ftue.completed_steps` | set | rỗng |
| `ads.daily` | `{date, multiplier, revive, booster, life}` | hôm nay, 0 |
| `settings.sound` / `music` / `haptics` | bool | true |
| `ads.wins_since_interstitial` | int | 0 |

- **Không lưu giữa level.** Tắt app giữa level thì lần sau chơi lại từ đầu; booster đã dùng và tim đã trừ không hoàn lại.
- Mọi thay đổi bền vững (xu, booster, tim, level) là commit nguyên tử: đổi → lưu → lưu lỗi thì khôi phục (G18).

## 6. FTUE

Theo GDD anh Tánh §2 (chi tiết ở `features/ftue.md` v2, giai đoạn F):

- **Cơ chế chung:** lớp mờ chặn chạm ngoài vùng đích (HUD vẫn dùng được) · máy trạng thái từng bước, sang bước sau khi đạt điều kiện · khung highlight · một số bước lưu tiến độ, mở lại app thì bỏ qua bước đã lưu.
- **Level 1:** vào level → đồng ý T&C / Privacy → chạm chồng 1, 2, 3 (highlight, "Tap to send cards") → thắng (**lưu**). Level 1 cần đúng 3 chồng chạm theo thứ tự, mỗi chồng đi thẳng lên cọc.
- **Popup element:** Hidden Card Stack "Unblock the stack to reveal its color." (7) · Hidden Color Pole "Move to the bottom to reveal its color." (12) · Locked Card Stack "Send cards up to unlock the stack." (17).
- **Popup booster:** Hand "Pick any stack of cards!" (5) · Shuffle "Shuffle the poles in queues!" (8) · Remove "Tap the pole to sort it instantly!" (10). **[XÁC NHẬN]** Sau popup, nút booster được highlight một lần; không bắt buộc dùng.
- Link T&C / Privacy: **[HỎI TÁNH / Phat cung cấp]**; tạm dùng URL giữ chỗ trong config.

## 7. Kinh tế

Có lại xu và booster (đảo CR-005). Mọi số là `ConfigKey` (remote config, rule: `pf-add-key`). Bảng đầy đủ ở `economy-sheet.md` v2 (giai đoạn C). Giá trị mặc định **đã duyệt (D-031)**; C1 đã code phần xu (khởi đầu, thưởng thắng, ×2, giá Revive), C2 làm phần booster:

| Key | Mặc định | Ý nghĩa (GDD anh Tánh §5) |
|---|---|---|
| `economy.start_coins` | 100 | xu khi chạy lần đầu (GDD chưa nói) |
| `economy.win_reward` | 20 | xu mỗi level |
| `economy.win_ad_multiplier` | 2 | xem quảng cáo ở màn Win nhân thưởng |
| `booster.hand.price` / `.amount` | 100 / 1 | giá xu một lần mua / số booster mỗi lần mua |
| `booster.shuffle.price` / `.amount` | 80 / 1 | |
| `booster.remove.price` / `.amount` | 120 / 1 | |
| `booster.unlock_gift` | 3 | quà khi mở khoá mỗi booster |
| `revive.first_price` | 100 | giá xu Revive lần đầu trong một lượt |
| `revive.price_multiplier` | 2 | Revive lần sau = lần trước × hệ số |
| `revive.max_per_level` | 3 | 0 = không giới hạn |
| `rv_slot.amount` / `.max_per_attempt` | 8 / 2 | R-23 |
| `ads.daily_max.multiplier` / `.revive` / `.booster` | 0 / 0 / 0 | 0 = không giới hạn |
| `lives.max` / `lives.refill_minutes` | 5 / 20 | §4.3 |

- Hết booster: bấm nút booster → dialog mua bằng xu hoặc xem quảng cáo nhận 1.
- Không có gì mua bằng tiền thật trong MVP (D-007).
- Tên booster bán trong remote config của GDD anh Tánh (Paper Box, Magnet) lệch với Hand / Shuffle — dùng Hand / Shuffle / Remove **[HỎI TÁNH]**.

## 8. Luồng UI và màn hình

Danh sách màn: `screen-inventory.md` v2. Thêm so với v1.7: thanh booster (3 nút, khoá trước khi mở), nút RV Slot cạnh ô chờ, HUD xu, tim ở Home, dialog T&C, Out of Lives, popup mở khoá booster / element, dialog mua booster, thanh tiến độ trên màn Win, nút nhân thưởng ở màn Win. Mock-up mới qua Bước 2 trước khi code từng giai đoạn.

```
Boot → Home (tim, xu) ──Play──▶ [hết tim? → Out of Lives] ──▶ Gameplay
Gameplay ──thắng──▶ [Win: xu, ×2 qua quảng cáo, thanh tiến độ] ──Next──▶ Gameplay (level+1)
         │                                                      └──Home──▶ Home
         ├──thua──▶ [Lose: Revive (xu / quảng cáo) · RV Slot (nếu tràn) · booster] ──Give up──▶ [Lose: failed] ──Retry──▶ Gameplay (−1 tim)
         └──II──▶ [Pause] ──Settings──▶ [Settings]
                     ├──Restart──▶ Gameplay (−1 tim)
                     └──Home──▶ Home (−1 tim)
```

## 9. Âm thanh và rung

Theo bảng GDD anh Tánh §4:

| ID | Khi nào | Rung |
|---|---|---|
| `music_gameplay` | bắt đầu chơi | – |
| `music_home` | ở Home | – |
| `sfx_win` | thắng | mạnh |
| `sfx_lose` | thua (kẹt hoặc tràn) | mạnh |
| `sfx_ui_click` | chạm UI | nhẹ |
| `sfx_coin` | nhận xu | – |
| `sfx_booster_reward` | nhận booster | – |
| `sfx_tap` | chạm chồng hợp lệ | vừa |
| `sfx_card_land_target` | lá trượt vào cọc | – |
| `sfx_target_complete` | cọc đầy và biến mất | vừa |
| `sfx_reveal` | Hidden stack / pole lộ màu | – |
| `sfx_unlock` | Locked stack mở khoá | – |
| `sfx_shuffle` | xáo cọc | – |

**[XÁC NHẬN]** Giữ thêm từ v1.7: `sfx_tap_invalid` (chạm chồng bị che / khoá), `sfx_card_land_buffer`, `sfx_buffer_warning` (ô chờ còn ≤ 6).

## 10. Text

- **Giọng văn:** ngắn, vui, động từ trước ("Tap to send cards"). Không tiếng lóng.
- **Key:** `<màn>.<phần tử>[.<biến thể>]`, chữ thường. Mọi text qua `LocKey` (rule #4).
- **Độ dài tối đa:** tiêu đề 26 ký tự, nút 12 ký tự, dòng hướng dẫn / popup 40 ký tự (popup được 2 dòng).

## 11. Kiếm tiền và quảng cáo (giả lập, D-007)

- **Rewarded:** Revive · RV Slot · nhân thưởng ở màn Win · nhận 1 booster khi hết · nhận 1 tim khi hết. Giới hạn mỗi ngày theo §7 (0 = không giới hạn).
- **Interstitial:** giữ v1.7 — sau khi bấm Next ở màn Win, từ Level 5, cứ 2 lần thắng một lần, cách nhau tối thiểu 60 giây; không hiện sau khi thua.
- Thưởng chỉ trao khi quảng cáo báo **hoàn thành** (G19).

## 12. Sự kiện analytics

| Sự kiện | Tham số |
|---|---|
| `level_started` | `level_index`, `attempt_no` |
| `level_won` | `level_index`, `taps`, `duration_s`, `boosters_used`, `revives`, `rv_slots` |
| `level_failed` | `level_index`, `taps`, `duration_s`, `reason` (`overflow`/`stuck`), `cards_left` |
| `level_quit` | `level_index`, `taps` |
| `booster_used` | `booster_id`, `level_index` |
| `booster_bought` | `booster_id`, `source` (`coins`/`ad`) |
| `revive_used` | `level_index`, `source` (`coins`/`ad`), `removes` |
| `rv_slot_used` | `level_index`, `index` (1/2) |
| `feature_unlocked` | `feature_id`, `level_index` |
| `life_spent` / `life_refilled` | `lives_after` |
| `terms_accepted` | — |
| `ad_rewarded_completed` / `ad_interstitial_shown` | `placement`, `level_index` |
| `coins_changed` | `delta`, `reason`, `balance` |

Các sự kiện đều ở thì quá khứ (rule #1).

## 13. Trường hợp biên

| Trường hợp | Xử lý |
|---|---|
| App bị tắt giữa level | Chơi lại level từ đầu; booster đã dùng, tim đã trừ không hoàn lại. |
| App bị tắt khi nhận thưởng thắng | Xu, tim trả lại và `current_level` được lưu **trước** khi hiện Win (G19). |
| App bị tắt giữa rewarded ad | Không có callback hoàn thành ⇒ không thưởng. |
| Lưu lỗi | Khôi phục trạng thái trước thay đổi, log lỗi, không hiện thưởng (G18). |
| Đổi giờ máy | Tim: giờ lùi ⇒ đặt lại mốc, không cộng tim; giờ tiến ⇒ hồi theo giờ mới (chấp nhận cho MVP). Giới hạn quảng cáo theo ngày dùng ngày máy. Interstitial dùng đồng hồ đơn điệu trong phiên. |
| Chạm nhiều ngón cùng lúc | Chỉ nhận một `tap` mỗi khung hình. |
| Đang chọn mục tiêu cho Hand / Remove | Chạm ra ngoài hoặc nút Back thì huỷ, không trừ booster. |
| Revive Remove hết cọc | Có thể dẫn tới thắng ngay — hiện Win như thường. |
| Hết 30 level | Màn Win của Level 30 hiện "More levels coming soon"; Play ở Home mở lại Level 30. |
| Không đủ xu | Nút giá xu bị khoá; vẫn còn lựa chọn xem quảng cáo (nếu chưa chạm giới hạn ngày). |

## Lịch sử thay đổi

| Phiên bản | Ngày | Thay đổi |
|---|---|---|
| v1.0 | 06-10-2026 | Duyệt lần đầu (D-015). |
| v1.1 | 06-10-2026 | CR-001 (phát sinh ở Bước 2): giới hạn tiêu đề 18 → 26 ký tự, ribbon tự thu nhỏ chữ; thêm key `gameplay.restart.note`. Ảnh hưởng: `features/core-gameplay.md`, mock-up `win-hard`, `win-final`, `unlock-space`, `restart-confirm`. |
| v1.2 | 06-10-2026 | CR-002 (phát sinh ở Bước 5): R-1b cấm hai chồng cùng layer giao nhau; level khó = trường `hard` trong file level; save thêm `attempts`, đổi `max_level_reached` → `highest_cleared`. Ảnh hưởng: `level-design.md` §2, `features/level-progression.md`. |
| v1.3 | 06-10-2026 | CR-005 (phát sinh ở Bước 6): bỏ booster (R-16, R-17, thanh booster, dialog mở khoá / mua) và bỏ xu (HUD, thưởng thắng, Continue bằng xu); Continue chỉ bằng quảng cáo thưởng; khay bàn chơi kéo xuống chỗ thanh booster. CR-004: một lá của luật vẽ thành tập 6 lá mỏng (đích x/18). |
| v1.4 | 06-10-2026 | CR-006 (phát sinh ở Bước 6): đếm từng lá — đích 18 lá, ô tạm 26 rãnh (1 lá / rãnh, đứng dọc) cho mọi level, Continue +6; 30 level sinh lại (revision 2). Thay hệ số ×6 của CR-004. |
| v1.5 | 07-10-2026 | CR-008: R-6 — một lần chạm lấy tối đa `max_run` lá (6) của run cùng màu; level có trường `max_run`; 30 level sinh lại. |
| v1.6 | 07-10-2026 | CR-009: hàng đợi đích chia theo cột (target i → cột i mod n_slots); cọc đầy thì cọc phía sau cùng cột tiến lên, không đổi sang màu kế tiếp của cả hàng đợi; 30 level sinh lại. |
| v1.7 | 07-10-2026 | CR-010: không chia độ khó — bỏ level khó (badge HARD, tiêu đề thắng riêng, cờ `hard`). |
| v2.0 | 08-10-2026 | **Nháp, chờ duyệt.** CR-012: theo GDD anh Tánh. Luật: chồng một màu (R-1c), chỉ chồng khác màu mới che (R-3), chạm gửi cả chồng (R-6, bỏ `max_run`), thua do kẹt (R-13), Shuffle ngẫu nhiên (R-15). Bỏ Continue (R-18); thêm Remove, Hand, Shuffle (R-19…R-21), Revive (R-22), RV Slot (R-23), 3 element (R-24…R-26). Có lại xu + booster (đảo CR-005), thêm tim, thanh tiến độ mở element, FTUE mới + T&C, 8 màu, bảng âm thanh / rung theo GDD, remote config. Save v2. |
