# Spec: Thua → Continue

> v1.0 · Bước 1 · đã duyệt (Phat, 06-10-2026). Luật: R-12, R-18. Con số: `economy-sheet.md` §2.

**Mục đích.** Cho người chơi cơ hội chơi tiếp thay vì mất cả level; là điểm chạm rewarded ad chính.

## Luồng người chơi

1. `overflow` → model `lost` → view cho lá thừa nảy bật ra khỏi `BufferBar` → mở `LoseDialog` ở trạng thái `offer`.
2. `offer` có ba lựa chọn:
   - **Continue bằng xu** (`continue.price`; khoá nếu không đủ xu) → trừ xu, lưu → R-18 → đóng dialog, chơi tiếp.
   - **Continue bằng quảng cáo** → hoàn thành thì R-18; huỷ thì quay lại `offer`.
   - **No thanks** → chuyển sang trạng thái `failed`.
3. Trạng thái `failed` có hai nút: **Retry** (chơi lại level, `attempt` mới) và **Home**. Phát `level_failed`.
4. Đã dùng Continue trong `attempt` này mà thua lần nữa: vào thẳng `failed`.

## Dữ liệu lưu

`coins` (khi trả bằng xu). Trạng thái level không được lưu.

## Trường hợp biên

- Tắt app khi đang ở `offer`: lần sau chơi lại level từ đầu.
- Đóng dialog bằng nút Back (Android) ở `offer`: tương đương **No thanks**. Ở `failed`: tương đương **Home**.
- Đóng dialog vì lý do `Aborted` của framework: không trao gì, xử lý như No thanks.

## Key text

`lose.title` ("Out of space!") · `lose.continue.coins` ("Continue") · `lose.continue.ad` ("Continue free") · `lose.no_thanks` ("No thanks") · `lose.failed.title` ("Level failed") · `lose.retry` ("Retry") · `lose.home` ("Home")

## Visual Contract

- Ở `offer`, bàn chơi vẫn nhìn thấy mờ phía sau dialog để người chơi thấy mình còn bao nhiêu bài.
- Nút Continue bằng quảng cáo là nút chính (to nhất, màu nhấn). No thanks là liên kết chữ nhỏ, xuất hiện sau 1 giây.
- Sau Continue, `BufferBar` dài ra giống Extra Space và lá thừa bay vào chỗ mới.

## Acceptance

- [ ] Test model: Continue đưa lá gây tràn và phần còn lại của `run` vào đúng chỗ, kết quả về `playing` (R-18).
- [ ] Test model: Continue thứ hai trong cùng `attempt` bị từ chối.
- [ ] Test: rewarded bị huỷ thì không đổi trạng thái (G19).
