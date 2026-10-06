# Bảng kinh tế — CardSlot

> v1.0 · Bước 1 · đã duyệt (Phat, 06-10-2026). Mọi con số là config có tên (G20), sinh thành `ConfigKey` qua manifest (rule: `pf-add-key`). Trong MVP không có gì bán bằng tiền thật (D-007, D-010).

## 1. Tiền và vật phẩm

| ID | Loại | Lưu ở |
|---|---|---|
| `coin` | tiền mềm | save `coins` |
| `booster_undo` | vật phẩm tiêu hao | save `boosters.undo` |
| `booster_add_slot` | vật phẩm tiêu hao | save `boosters.add_slot` |

## 2. Tham số

| Key config | Mặc định | Khoảng hợp lệ | Ý nghĩa |
|---|---|---|---|
| `economy.start_coins` | 100 | 0–1000 | xu khi chạy lần đầu |
| `economy.win_reward` | 20 | 0–200 | xu mỗi lần thắng level (mọi level) |
| `economy.win_reward_hard_bonus` | 10 | 0–200 | cộng thêm ở level "khó" (5, 10, …, 30) |
| `economy.replay_reward` | 5 | 0–200 | xu khi thắng lại level đã qua (chỉ ở Level 30, xem GDD §13) |
| `booster.undo.price` | 60 | 1–1000 | giá xu một Undo khi hết trong kho |
| `booster.add_slot.price` | 100 | 1–1000 | giá xu một Extra Space |
| `booster.unlock_gift` | 2 | 0–10 | số lượt tặng khi mở khoá mỗi booster |
| `booster.add_slot.amount` | 4 | 1–10 | số chỗ `buffer` cộng thêm |
| `booster.add_slot.max_per_attempt` | 1 | 1–5 | |
| `continue.price` | 120 | 1–1000 | giá xu một Continue |
| `continue.slot_amount` | 4 | 1–10 | |
| `continue.max_per_attempt` | 1 | 1–3 | |
| `ads.interstitial.first_level` | 5 | 1–30 | |
| `ads.interstitial.every_n_wins` | 2 | 1–10 | |
| `ads.interstitial.min_interval_s` | 60 | 0–600 | |
| `ads.rewarded.booster_amount` | 1 | 1–3 | booster nhận khi xem quảng cáo |

## 3. Nguồn vào và nguồn tiêu

| Nguồn vào | Lượng |
|---|---|
| Xu khởi đầu | 100 |
| Thắng level | 20 (30 ở level khó) |
| Quà mở khoá booster | 2 Undo (Level 4), 2 Extra Space (Level 7) |
| Rewarded ad | 1 booster, hoặc 1 Continue miễn phí |

| Nguồn tiêu | Giá |
|---|---|
| Undo | 60 xu |
| Extra Space | 100 xu |
| Continue | 120 xu |

## 4. Thu nhập dự kiến qua 30 level

- Thắng cả 30 level: 30 × 20 + 6 × 10 = **660 xu**, cộng 100 khởi đầu = **760 xu** tổng nguồn vào.
- Mục tiêu thiết kế: người chơi trung bình dùng khoảng **6–8 lần trả phí** (booster hoặc Continue) bằng xu trong 30 level. Hết xu thì phải xem quảng cáo, đó là điểm chạm quảng cáo chính.
- Kiểm tra nhanh: 760 xu ≈ 3 Continue + 4 Extra Space (≈ 760). Có cảm giác đủ dùng ở đầu game và khan hiếm dần từ level 20.

Những con số này sẽ được chỉnh sau buổi chơi thử ở Bước 8. Chỉ đổi config, không sửa code.
