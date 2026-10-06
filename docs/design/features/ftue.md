# Spec: FTUE (hướng dẫn lần đầu) và mở khoá booster

> v1.0 · Bước 1 · đã duyệt (Phat, 06-10-2026). Level FTUE: `level-design.md` §6.

**Mục đích.** Dạy luật bằng cách cho chơi; người chơi hiểu cả vòng chơi sau 2 level mà không phải đọc nhiều.

## Các bước

| Bước | Level | Hiện gì | Chuyển bước khi | Skip? |
|---|---|---|---|---|
| `ftue.l1.step1` | 1 | Bàn tay chỉ vào một chồng; dòng "Tap a stack to send its cards!" | người chơi `tap` **chồng được chỉ** | không |
| `ftue.l1.step2` | 1 | Bàn tay chỉ chồng kế; "Fill the targets to clear them!" | `target` đầu tiên hoàn thành | không |
| `ftue.l2.buffer` | 2 | Lần đầu một lá vào `buffer`: `BufferBar` loé sáng; "No match? Cards wait here." | sau 2.5 giây hoặc `tap` kế tiếp | tự tắt |
| `ftue.l2.warn` | 2 | Không hiện bàn tay; `BufferBar` sáng lên một lần; "Don't let it fill up!" | `tap` kế tiếp | tự tắt |
| `ftue.unlock.undo` | 4 | `BoosterUnlockDialog` Undo, tặng `booster.unlock_gift` | bấm "Got it" | — |
| `ftue.unlock.add_slot` | 7 | `BoosterUnlockDialog` Extra Space, tặng `booster.unlock_gift` | bấm "Got it" | — |

- Chỉ **chạm** mới tính (trò chơi không có kéo thả).
- Trong `ftue.l1.step1`, chạm vào chồng khác chồng được chỉ thì bị bỏ qua, bàn tay nảy nhẹ để nhắc.
- Mỗi bước hoàn thành được ghi vào `ftue.completed_steps`. Bước đã hoàn thành thì không hiện lại, kể cả khi chơi lại level.
- Popup mở khoá xuất hiện **trước khi** người chơi chạm lần đầu của level đó, và chỉ một lần.

## Dữ liệu lưu

`ftue.completed_steps`, `boosters_unlocked`, `boosters.*` (quà tặng).

## Trường hợp biên

- Tắt app giữa `ftue.l1.step2`: step1 đã lưu là xong; lần sau Level 1 chơi lại và chỉ hiện step2.
- Quà tặng mở khoá được lưu cùng lúc với cờ `boosters_unlocked` (một commit nguyên tử). Không có cách nhận quà hai lần.

## Key text

`ftue.l1.step1` · `ftue.l1.step2` · `ftue.l2.buffer` · `ftue.l2.warn` · `unlock.undo.title` ("New booster: Undo!") · `unlock.undo.desc` ("Take back your last tap.") · `unlock.add_slot.title` ("New booster: Extra Space!") · `unlock.add_slot.desc` ("Get more room to hold cards.") · `unlock.got_it` ("Got it")

## Visual Contract

- Bàn tay nằm **trên** mọi thứ của bàn chơi nhưng dưới dialog. Phần còn lại của bàn chơi tối đi 40% trong `ftue.l1.step1`.
- Dòng hướng dẫn nằm trong một bong bóng ở vùng giữa, không che `target` hay chồng đang được chỉ.

## Acceptance

- [ ] Test: mỗi bước chỉ hiện một lần qua các lần chơi lại.
- [ ] Test: mở khoá cấp đúng số quà, chỉ một lần.
- [ ] Ảnh chụp Level 1 step1 và Level 4 popup khớp mock-up đã duyệt.
