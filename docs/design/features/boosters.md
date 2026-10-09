# Spec: Booster — Hand, Shuffle, Remove

> v2.0 · CR-012 giai đoạn C2 · đã duyệt (Phat, 08-10-2026, D-035). Luật: GDD v2.0 R-19, R-20, R-21. Con số: GDD v2.0 §7 (D-031). Mock-up: `docs/mockups/*-v02.png` (booster). Thay bản v1.0 (Undo / Extra Space, bỏ ở CR-005).

**Mục đích.** Cho người chơi lối thoát khi sắp kẹt; là nơi tiêu xu và điểm chạm quảng cáo thưởng.

## Booster

| Booster | Mở ở | Làm gì (luật) | Chọn mục tiêu |
|---|---|---|---|
| Hand | level 5 | R-20: gửi cả một chồng bất kỳ còn lá, kể cả bị che | có — một chồng |
| Shuffle | level 8 | R-21: đưa cọc màu cần xuống đáy, xáo các cọc chưa có lá | không — dùng ngay |
| Remove | level 10 | R-19: một cọc bất kỳ (đáy hoặc cọc chờ ngay sau) lấy lá cùng màu tới đầy rồi đi | có — một cọc |

## Luồng người chơi

1. Thanh 3 ô booster dưới khay (`gameplay-boosters-v02`). Chưa mở: ô xám, ổ khoá, "Lv N". Có hàng: huy hiệu số. Hết: nhãn giá xu.
2. Mở khoá: bắt đầu level 5 / 8 / 10 **lần đầu** → tặng `booster.unlock_gift` (3) và hiện `BoosterUnlockDialog` (`unlock-hand-v02`); ô booster mới sáng lên. Đóng popup thì chơi.
3. Chạm ô booster khi **còn hàng**:
   - Shuffle: dùng ngay — trừ 1 và lưu, rồi model xáo.
   - Hand / Remove: vào chế độ chọn (`gameplay-pick-*-v02`): mờ mọi thứ trừ vùng chọn được, dải chữ hướng dẫn, ô booster sáng. Chạm mục tiêu hợp lệ → trừ 1, lưu, model chạy. Chạm ra ngoài, chạm lại ô booster hoặc Back → huỷ, không trừ.
4. Chạm ô booster khi **hết**: `BoosterBuyDialog` (`booster-buy-v02`) — mua `amount` (1) bằng `price` xu (nút chính; khoá + "Not enough coins" khi thiếu xu) hoặc "▶ Free" (quảng cáo thưởng `rewarded_booster`, nhận 1). Mua xong thì booster vào kho, người chơi chạm lại để dùng.
5. Màn thua: thanh booster vẫn dùng được trên lớp mờ (`lose-offer-boosters-v02`). Dùng booster thì dialog thua đóng; nếu hết kẹt thì chơi tiếp, còn kẹt thì dialog thua hiện lại.

## Khi nào dùng được

- Trong lúc `playing`, khi không có dialog / quảng cáo / hoạt ảnh kết thúc đang chạy.
- Ở màn thua (R-13): Remove và Shuffle luôn được; Hand chỉ khi thua do kẹt (không còn lá dở chờ đặt) — lá dở của lần tràn chưa có chỗ thì gửi thêm một chồng chỉ tràn tiếp.
- Hand: chồng còn lá. Remove: cọc ở đáy, hoặc cọc chờ ngay sau một cọc đáy (cọc thấy được). Shuffle: còn ít nhất một cọc chưa nhận lá.

## Dữ liệu lưu

`boosters.hand` / `.shuffle` / `.remove` (int ≥ 0), `unlocked` (danh sách `booster_*`), xu (ví). Dùng / mua / nhận là commit nguyên tử: đổi → lưu → lưu lỗi thì khôi phục cả booster lẫn xu (G18). Quảng cáo chỉ trao khi báo hoàn thành (G19).

## Trường hợp biên

- Tắt app ngay sau khi dùng booster: booster đã bị trừ và đã lưu; level chơi lại không hoàn (GDD §5).
- Chọn mục tiêu rồi huỷ: không trừ booster.
- Shuffle khi không có gì đổi được (mọi cọc đã có lá): ô Shuffle không dùng được.
- Remove cọc chờ: cọc đó đầy và đi; các cọc sau nó trong cột dồn lên một chỗ. Cọc đáy giữ nguyên.
- Booster làm thắng ngay (ví dụ Remove dọn lá cuối): hiện Win như thường.

## Key text

`booster.hand.name` ("Hand") · `booster.shuffle.name` ("Shuffle") · `booster.remove.name` ("Remove") · `booster.hand.tip` ("Pick any stack of cards!") · `booster.shuffle.tip` ("Shuffle the poles in queues!") · `booster.remove.tip` ("Tap the pole to sort it instantly!") · `booster.locked` ("Lv {0}") · `booster.buy.title` ("Need a boost?") · `booster.buy.ad` ("Free") · `booster.buy.not_enough` ("Not enough coins") · `booster.unlock.title` ("New booster: {0}!") · `booster.unlock.ok` ("Got it") · `booster.pick.hand` ("Tap any stack") · `booster.pick.remove` ("Tap a pole to fill it") · `lose.use_booster` ("or use a booster ▼")

## Visual Contract

- Ô booster: `boost_tile`, glyph `icon_hand` / `icon_shuffle` / `icon_remove` tô màu Secondary; huy hiệu số màu Primary ở góc trên phải; hết hàng thì nhãn tối có xu + giá ở đáy ô.
- Chế độ chọn: lớp mờ như dialog trên phần không chọn được; ô booster đang dùng có viền trắng sáng.
- Hand / Remove: lá bay như một lần chạm (cùng đường bay, cùng peg rời đi). Shuffle: cọc đổi chỗ, lá ô chờ bay lên cọc mới khớp màu.

## Acceptance

- [x] Test model: Hand gửi cả chồng bị che; huỷ không đổi gì (R-20).
- [x] Test model: Remove cọc đáy và cọc chờ; thứ tự lấy lá dở → ô chờ → bàn (R-19).
- [x] Test model: Shuffle chọn màu theo ô chờ rồi chồng mở, giữ cọc đã có lá, giữ số cọc mỗi cột, cùng seed thì cùng kết quả (R-21, R-15).
- [x] Test: dùng / mua bằng xu khi lưu lỗi thì xu và booster đều về giá trị cũ (G18).
- [x] Test: mở khoá level 5 / 8 / 10 tặng 3, chỉ một lần.
