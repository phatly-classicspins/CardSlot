# Spec: Booster — Undo và Extra Space

> v1.0 · Bước 1 · đã duyệt (Phat, 06-10-2026). Luật: R-16, R-17. Con số: `economy-sheet.md` §2.

**Mục đích.** Cho người chơi lối thoát khi sắp kẹt; là nơi tiêu xu và điểm chạm rewarded ad.

## Luồng người chơi

1. Hai `BoosterButton` ở đáy `GameplayScreen`, hiện từ lúc mở khoá (Undo: Level 4, Extra Space: Level 7).
2. Chạm booster:
   - **Còn trong kho:** dùng ngay, trừ 1, lưu → model áp dụng → phát `booster_used(source=inventory)`.
   - **Hết:** mở `BoosterBuyDialog` với hai lựa chọn: **mua bằng xu** (khoá nếu không đủ xu) hoặc **xem quảng cáo**.
     - Mua bằng xu: trừ xu + dùng ngay, một commit nguyên tử (G18).
     - Xem quảng cáo: chỉ khi quảng cáo báo hoàn thành thì mới dùng ngay (G19). Đóng hoặc huỷ thì không có gì xảy ra.
3. Booster không dùng được (Undo khi chưa có `tap`; Extra Space đã đạt `max_per_attempt`) thì nút mờ đi và chạm vào không có tác dụng.

## Trạng thái nút

khoá · có số lượng (huy hiệu số) · hết (hiện giá xu) · không dùng được.

## Dữ liệu lưu

`boosters.undo`, `boosters.add_slot`, `boosters_unlocked`, `coins`.

## Trường hợp biên

- Tắt app ngay sau khi dùng booster: booster đã bị trừ và đã lưu; level chơi lại không hoàn booster (GDD §5).
- Undo sau Continue: Undo lùi `tap` nhưng **không** lấy lại chỗ đã cộng từ Continue hay Extra Space. `buffer_capacity` chỉ tăng trong một `attempt`.
- Dùng Extra Space khi `buffer` chưa có lá: vẫn hợp lệ.

## Key text

`booster.undo.name` ("Undo") · `booster.add_slot.name` ("Extra Space") · `booster.buy.title` ("Need a boost?") · `booster.buy.coins` ("{0}") · `booster.buy.ad` ("Free") · `booster.buy.not_enough` ("Not enough coins")

## Visual Contract

- Huy hiệu số lượng đọc được ở góc nút. Khi hết thì đổi thành biểu tượng xu + giá.
- Undo: các lá vừa đi **bay ngược** về chồng cũ (cùng đường, tua lại), không biến mất rồi hiện lại.
- Extra Space: `BufferBar` dài ra với hoạt ảnh, chỗ mới loé sáng.

## Acceptance

- [ ] Test model: Undo khôi phục đúng trạng thái trước `tap` kể cả khi `tap` đó gây chuỗi `settle` nhiều bước (R-16).
- [ ] Test model: Extra Space tăng đúng `amount` và từ chối lần dùng vượt `max_per_attempt` (R-17).
- [ ] Test: mua bằng xu khi lưu lỗi thì xu và booster đều về giá trị cũ (G18).
- [ ] Test: rewarded bị huỷ thì không có booster nào được trao (G19).
