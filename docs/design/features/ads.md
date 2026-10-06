# Spec: Quảng cáo (giả lập)

> v1.0 · Bước 1 · đã duyệt (Phat, 06-10-2026). D-007: dùng Fake Ads của framework, chưa nối SDK. Con số: `economy-sheet.md` §2.

**Mục đích.** Dựng đủ luồng quảng cáo để đo vị trí và tần suất, nối SDK thật sau mà không phải sửa luồng.

## Vị trí

| Placement | Loại | Khi nào | Thưởng |
|---|---|---|---|
| `rewarded_continue` | rewarded | `LoseDialog` ở `offer` | Continue (R-18) |
| `rewarded_booster` | rewarded | `BoosterBuyDialog` | `ads.rewarded.booster_amount` booster, dùng ngay |
| `interstitial_level_end` | interstitial | sau khi bấm Next ở `WinDialog` | — |

## Luật interstitial

Hiện khi **đồng thời**: level vừa thắng ≥ `ads.interstitial.first_level`; `ads.wins_since_interstitial` ≥ `every_n_wins`; đã qua ≥ `min_interval_s` kể từ lần quảng cáo cuối (interstitial **hoặc rewarded**) trong phiên. Hiện xong thì đặt `wins_since_interstitial = 0`.

## Trường hợp biên

- Quảng cáo không sẵn sàng (fake có thể giả lập lỗi): nút rewarded mờ đi với dòng "Ad not available"; interstitial bị bỏ qua, không chặn người chơi.
- Thưởng chỉ trao trong callback **hoàn thành** (G19).

## Key text

`ads.not_available` ("Ad not available")

## Visual Contract

- Nút rewarded có biểu tượng ▶ thống nhất ở mọi nơi.

## Acceptance

- [ ] Test: luật interstitial với các tổ hợp level / số lần thắng / thời gian.
- [ ] Test: rewarded báo lỗi hoặc bị huỷ → không trao thưởng.
