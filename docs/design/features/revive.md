# Spec: Thua → Revive / RV Slot

> **v1.0 · nháp, chờ duyệt** (CR-012 giai đoạn B). Thay `continue.md`. Luật: GDD v2.0 R-12, R-13, R-19, R-22, R-23. Con số: GDD §7 (`EconomyTuning`).

**Mục đích.** Cho người chơi chơi tiếp thay vì mất cả level; là điểm chạm rewarded ad chính. Xu cho Revive đến ở giai đoạn C.

## Luồng người chơi

1. Thua do tràn (R-12) hoặc do kẹt (R-13) → mở `LoseDialog` ở trạng thái `offer`. Tiêu đề: "Out of space!" (tràn) / "No moves left!" (kẹt).
2. `offer` có tối đa ba lựa chọn:
   - **Revive** (quảng cáo) — dòng phụ "Clears 2 poles". Hiện khi còn lượt Revive trong `attempt` (mặc định 3). Hoàn thành → R-22: Remove miễn phí 2 cọc ở đáy hàng có ít lá nhất (bằng nhau thì bên trái), đặt lại lá dở; còn kẹt thì Remove thêm từng cọc. Huỷ quảng cáo → quay lại `offer`.
   - **+8 slots** (quảng cáo, nút phụ) — hiện khi còn lượt RV Slot (2 / `attempt`) và thêm chỗ còn cứu được (còn lá dở hoặc còn chồng chạm được). Hoàn thành → R-23.
   - **No thanks** → trạng thái `failed`.
   Quảng cáo chưa sẵn sàng: các nút hiện "Ad not available" và bị khoá.
3. Trạng thái `failed`: **Retry** và **Home**.
4. Không còn lựa chọn nào → vào thẳng `failed`.

## RV Slot lúc đang chơi

- Nút xanh "▶ +8 slots" phía trên ô chờ, bên phải giữa, không đè cọc. Chỉ hiện khi đang chơi và còn lượt.
- Bấm → quảng cáo; hoàn thành thì ô chờ +8 (26 → 34 → 42). Quảng cáo chưa sẵn sàng: không có gì xảy ra.

## Hoạt ảnh

Revive dùng chung hoạt ảnh của lần chạm: lá bay từ bàn / ô chờ vào cọc, cọc đầy đứng lại rồi biến mất, cọc sau trượt lên. Hoạt ảnh riêng "cọc bay về phía bàn" (GDD anh Tánh §3.4) làm cùng booster Remove ở giai đoạn C.

## Dữ liệu lưu

Không có (lượt Revive / RV Slot tính theo `attempt`, không lưu).

## Text

`lose.title`, `lose.title_stuck`, `lose.revive`, `lose.revive_note`, `lose.rv_slot`, `lose.no_thanks`, `lose.failed_title`, `lose.subtitle`, `lose.retry`, `lose.home`, `gameplay.rv_slot`, `ads.not_available`.

## Quảng cáo

`rewarded_revive`, `rewarded_rv_slot` (thay `rewarded_continue`). Thưởng chỉ khi quảng cáo báo hoàn thành (G19).

## Kiểm tra

Test headless `R22_*`, `R23_*` (`BoardModelTests`). Ảnh: `docs/captures/cr12b/`.
