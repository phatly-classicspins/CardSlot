# CardSlot — ý tưởng một trang

> v0.2 · 06-10-2026 · Bước 0 · đã duyệt (Phat, 06-10-2026) · quyết định D-006…D-012.

## 1. Câu chuyện một dòng

Puzzle "chạm để xếp": chạm các lá bài đang lộ ra để gửi chúng lên đích cùng màu. Lá chưa có chỗ phải nằm chờ ở một hàng ô tạm có hạn. Hết ô thì thua.

## 2. Đã chốt

| Mục | Quyết định |
|---|---|
| Thể loại | Puzzle sắp xếp màu, một chạm (tham khảo: Card Slots, xem `reference-card-slots.md`) |
| Nền tảng | Android + iOS, màn dọc, chuẩn 1080×1920 (D-006) |
| Engine | Unity 6000.3.13f1 URP + PrototypeFramework 0.4.2 (D-001, D-003) |
| Kiếm tiền MVP | Giả lập: Fake Ads/IAP của framework (D-007) |
| Ngôn ngữ | Tiếng Anh; text đi qua `LocKey` (D-008) |
| Hướng art | Đồ chơi 3D-look sáng: khối bo tròn, bóng mềm, màu bão hoà, nền pastel (D-011) |
| Tên | "CardSlot" là tên nội bộ; tên thương mại chốt ở Bước 8 (D-012) |

## 3. Người chơi mục tiêu và phiên chơi

- Người chơi casual trên điện thoại, chơi trong lúc rảnh, một tay, cầm dọc.
- Một level dài 30–90 giây. Một phiên chơi 5–10 phút (khoảng 5–10 level).
- Không cần đọc: luật được dạy qua 2–3 level đầu.

## 4. Vòng chơi chính

```
nhìn bàn → chạm lá đang lộ ─┬─ có đích cùng màu, còn chỗ → lá bay lên đích
                            │      đích đầy → biến mất → đích mới hiện,
                            │      lá cùng màu trong ô tạm tự bay lên
                            └─ không có đích → lá vào ô tạm
                                   ô tạm đầy + không còn nước đi → THUA
dọn hết bài → THẮNG → nhận xu → level tiếp
```

**Các phần của một level (đều là dữ liệu, không hằng số trong code — G20):**

| Phần | Mô tả | Nút vặn độ khó |
|---|---|---|
| Khay bài | Lá bài xếp theo lớp; lá bị che không chạm được | số lớp, mật độ che |
| Hàng đích | 2–4 đích hiện cùng lúc; mỗi đích một màu và sức chứa *n* lá; phía sau có hàng đợi đích | số đích hiện, thứ tự hàng đợi |
| Ô tạm | *k* ô chờ | *k* (ví dụ 7 → 4) |
| Màu | Bảng màu dễ phân biệt, có thêm hoạ tiết cho người mù màu | số màu (2 → 6) |

Mọi level phải được **solver headless kiểm chứng là giải được** trước khi ship (engine-free, chạy bằng `dotnet test`).

## 5. Vòng ngoài (meta)

- **Xu:** thắng level nhận xu; dùng xu mua booster.
- **Booster** (D-010): *Thêm ô* (+1 ô tạm cho level hiện tại), *Hoàn tác* (trả lá vừa chạm về chỗ cũ).
- **Thua → Tiếp tục:** xem rewarded ad (giả lập) hoặc tiêu xu để có thêm ô và chơi tiếp, một lần mỗi level.
- **Interstitial** giả lập sau mỗi vài level (bắt đầu từ level 5+).

## 6. Phạm vi MVP (D-010)

**Có trong MVP**

| Hạng mục | Nội dung |
|---|---|
| Màn hình | Home (nút Play + số level), Gameplay, dialog Thắng, dialog Thua/Tiếp tục, Tạm dừng, Cài đặt (âm thanh, rung) |
| Level | 30 level, độ khó tăng dần, đều đã qua solver |
| Booster | Thêm ô, Hoàn tác |
| Kinh tế | Xu: thắng thì nhận, booster thì tốn; lưu trên máy |
| Kiếm tiền | Rewarded (tiếp tục, booster) + interstitial, đều giả lập |
| Cảm giác chơi | Lá bay lên đích có easing, đích đầy có hiệu ứng nổ, rung nhẹ, âm thanh cơ bản |
| Hướng dẫn | Level 1–2 có bàn tay chỉ vào lá cần chạm |

**Để sau MVP:** bản đồ level / meta trang trí, phần thưởng hàng ngày, shop IAP thật, SDK quảng cáo thật, sự kiện, bảng xếp hạng, cloud save, thêm ngôn ngữ, tablet layout riêng.

## 7. Rủi ro

| Rủi ro | Mức | Cách giảm |
|---|---|---|
| **Tên "CardSlot" gần trùng "Card Slots"** (đã có trên App Store) — dễ bị từ chối khi lên store hoặc vướng tranh chấp | cao nếu phát hành | đổi tên trước Bước 8; "CardSlot" chỉ là tên nội bộ |
| Level không giải được hoặc độ khó nhảy cóc | cao | solver headless + đồ thị độ khó (số nước tối thiểu, số lần dùng ô tạm) cho từng level |
| Thiếu cảm giác "đã tay" (lá bay, nổ đích) — đây là thứ bán game | trung bình | dành riêng thời gian ở Bước 4; mock-up có cả chuyển động |
| Clone quá sát game tham khảo (G8) | trung bình | art, bố cục, tên và level riêng; ghi lại những gì đã tham khảo |
| Đọc màu khó trên màn nhỏ / người mù màu | thấp | bảng màu tương phản cao + hoạ tiết riêng mỗi màu |

## 8. Ước lượng công sức (sơ bộ, theo bước quy trình)

| Bước | Ước lượng |
|---|---|
| 1 GDD | 1–2 ngày |
| 2 Mock-up | 2–3 ngày |
| 3 Art (lá bài, đích, khay, UI) | 3–5 ngày |
| 4 Animation / hiệu ứng | 2–3 ngày |
| 5 Logic (model, solver, 30 level) | 5–8 ngày |
| 6 Ráp | 3–4 ngày |
| 7 AI test | 2 ngày |
| **Tổng tới bản chơi thử nội bộ** | **khoảng 4–5 tuần** một người + AI |

Đây là ước lượng thô, sẽ chỉnh lại sau khi GDD được duyệt.
