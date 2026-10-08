# Báo cáo xuất art — CardSlot

> Sinh tự động bởi `art/tools/build-art.ps1` từ `art/source/art.html` (design-tokens v1.0), 08-10-2026 09:25. Không sửa tay (G12).

## Tổng

- Sprite: **81** · lỗi chặn: **0**
- Bộ nhớ texture: 6.27 MB nếu RGBA32 · ~0.71 MB với ASTC 6×6 (ngân sách 16 MB, D-019)
- Atlas: 
`` 81 sprite
- Kiểm tra: đúng kích thước spec · kích thước chẵn · cắt mép canvas · pixel lạc · quầng tối (sprite đục) · soi gương (sprite đối xứng) · đường nối 9-slice · ≤ 64 KB/sprite. Mọi sprite được bù màu alpha (alpha bleed) để không có viền tối khi lọc.

## Từng sprite

| Sprite | Atlas | Kích thước | 9-slice (L,T,R,B) | Loại | KB | Kết quả | Tự sửa |
|---|---|---|---|---|---|---|---|
| `card_face_0` | gameplay | 150×216 | — | opaque | 6.9 | đạt | — |
| `card_under_0` | gameplay | 150×214 | — | opaque | 2.5 | đạt | — |
| `card_mini_0` | gameplay | 66×146 | — | opaque | 3.9 | đạt | — |
| `chip_0` | gameplay | 34×50 | — | opaque | 0.9 | đạt | — |
| `target_base_0` | gameplay | 230×80 | 40,0,40,0 | opaque | 3.9 | đạt | — |
| `card_face_1` | gameplay | 150×216 | — | opaque | 6.5 | đạt | — |
| `card_under_1` | gameplay | 150×214 | — | opaque | 2.4 | đạt | — |
| `card_mini_1` | gameplay | 66×146 | — | opaque | 3.9 | đạt | — |
| `chip_1` | gameplay | 34×50 | — | opaque | 0.8 | đạt | — |
| `target_base_1` | gameplay | 230×80 | 40,0,40,0 | opaque | 3.6 | đạt | — |
| `card_face_2` | gameplay | 150×216 | — | opaque | 6.8 | đạt | — |
| `card_under_2` | gameplay | 150×214 | — | opaque | 2.4 | đạt | — |
| `card_mini_2` | gameplay | 66×146 | — | opaque | 3.9 | đạt | — |
| `chip_2` | gameplay | 34×50 | — | opaque | 0.8 | đạt | — |
| `target_base_2` | gameplay | 230×80 | 40,0,40,0 | opaque | 3.5 | đạt | — |
| `card_face_3` | gameplay | 150×216 | — | opaque | 7.8 | đạt | — |
| `card_under_3` | gameplay | 150×214 | — | opaque | 2.5 | đạt | — |
| `card_mini_3` | gameplay | 66×146 | — | opaque | 4.5 | đạt | — |
| `chip_3` | gameplay | 34×50 | — | opaque | 0.9 | đạt | — |
| `target_base_3` | gameplay | 230×80 | 40,0,40,0 | opaque | 3.9 | đạt | — |
| `card_face_4` | gameplay | 150×216 | — | opaque | 7.2 | đạt | — |
| `card_under_4` | gameplay | 150×214 | — | opaque | 2.5 | đạt | — |
| `card_mini_4` | gameplay | 66×146 | — | opaque | 4.3 | đạt | — |
| `chip_4` | gameplay | 34×50 | — | opaque | 0.9 | đạt | — |
| `target_base_4` | gameplay | 230×80 | 40,0,40,0 | opaque | 3.8 | đạt | — |
| `card_face_5` | gameplay | 150×216 | — | opaque | 6.6 | đạt | — |
| `card_under_5` | gameplay | 150×214 | — | opaque | 2.4 | đạt | — |
| `card_mini_5` | gameplay | 66×146 | — | opaque | 3.7 | đạt | — |
| `chip_5` | gameplay | 34×50 | — | opaque | 0.8 | đạt | — |
| `target_base_5` | gameplay | 230×80 | 40,0,40,0 | opaque | 3.6 | đạt | — |
| `card_face_6` | gameplay | 150×216 | — | opaque | 6.6 | đạt | — |
| `card_under_6` | gameplay | 150×214 | — | opaque | 2.4 | đạt | — |
| `card_mini_6` | gameplay | 66×146 | — | opaque | 3.7 | đạt | — |
| `chip_6` | gameplay | 34×50 | — | opaque | 0.9 | đạt | — |
| `target_base_6` | gameplay | 230×80 | 40,0,40,0 | opaque | 3.5 | đạt | — |
| `card_face_7` | gameplay | 150×216 | — | opaque | 7.6 | đạt | — |
| `card_under_7` | gameplay | 150×214 | — | opaque | 2.5 | đạt | — |
| `card_mini_7` | gameplay | 66×146 | — | opaque | 4.7 | đạt | — |
| `chip_7` | gameplay | 34×50 | — | opaque | 0.9 | đạt | — |
| `target_base_7` | gameplay | 230×80 | 40,0,40,0 | opaque | 3.8 | đạt | — |
| `card_covered_hatch` | gameplay | 150×206 | — | overlay | 1.6 | đạt | — |
| `target_slot_bg` | gameplay | 120×128 | 44,50,44,48 | overlay | 2.5 | đạt | — |
| `tray_rim` | gameplay | 160×174 | 56,56,56,70 | opaque | 4.4 | đạt | — |
| `tray_inner` | gameplay | 120×120 | 40,40,40,40 | opaque | 2.6 | đạt | — |
| `buffer_rail` | gameplay | 160×160 | 56,56,56,56 | opaque | 3.7 | đạt | — |
| `buffer_rail_warn` | gameplay | 160×160 | 56,56,56,56 | opaque | 3.8 | đạt | — |
| `buffer_cell` | gameplay | 66×140 | — | opaque | 1.3 | đạt | — |
| `buffer_cell_warn` | gameplay | 66×140 | — | opaque | 1.3 | đạt | — |
| `shadow_soft` | gameplay | 128×128 | 52,52,52,52 | shadow | 4 | đạt | — |
| `btn_primary` | ui | 200×164 | 64,60,64,74 | opaque | 7 | đạt | — |
| `btn_secondary` | ui | 200×164 | 64,60,64,74 | opaque | 7 | đạt | — |
| `btn_disabled` | ui | 200×164 | 64,60,64,74 | opaque | 6.3 | đạt | — |
| `pill_surface` | ui | 140×104 | 50,0,50,0 | opaque | 3.5 | đạt | — |
| `pill_sunken` | ui | 140×128 | 62,0,62,0 | opaque | 5 | đạt | — |
| `pill_ink` | ui | 80×56 | 28,0,28,0 | opaque | 1.7 | đạt | — |
| `rbtn_secondary` | ui | 104×114 | — | opaque | 3.7 | đạt | — |
| `panel` | ui | 200×218 | 70,70,70,88 | opaque | 5.6 | đạt | — |
| `ribbon_secondary` | ui | 200×184 | 50,0,50,0 | opaque | 4.2 | đạt | — |
| `ribbon_danger` | ui | 200×184 | 50,0,50,0 | opaque | 4.2 | đạt | — |
| `boost_tile` | ui | 170×182 | — | opaque | 4 | đạt | — |
| `badge` | ui | 64×70 | 32,0,32,0 | opaque | 2.5 | đạt | — |
| `badge_primary` | ui | 64×70 | 32,0,32,0 | opaque | 2.5 | đạt | — |
| `close_btn` | ui | 110×120 | — | opaque | 6 | đạt | — |
| `row_sunken` | ui | 120×130 | 44,44,44,44 | opaque | 2.9 | đạt | — |
| `toggle_on` | ui | 170×88 | — | opaque | 3.1 | đạt | — |
| `toggle_off` | ui | 170×88 | — | opaque | 3.1 | đạt | — |
| `toggle_knob` | ui | 72×78 | — | opaque | 2.2 | đạt | padded 72x77 -> 72x78 (even size) |
| `icon_holder` | ui | 260×272 | — | opaque | 6.7 | đạt | — |
| `coin` | ui | 96×96 | — | opaque | 11.6 | đạt | — |
| `icon_pause` | ui | 100×100 | — | icon | 1.4 | đạt | — |
| `icon_restart` | ui | 100×100 | — | icon | 2.3 | đạt | — |
| `icon_gear` | ui | 100×100 | — | icon | 1.8 | đạt | — |
| `icon_play` | ui | 100×100 | — | icon | 0.8 | đạt | — |
| `icon_close` | ui | 100×100 | — | icon | 1.3 | đạt | — |
| `icon_lock` | ui | 100×100 | — | icon | 1.1 | đạt | — |
| `icon_undo` | ui | 100×100 | — | icon | 1.3 | đạt | — |
| `icon_space` | ui | 100×100 | — | icon | 0.8 | đạt | — |
| `hand_pointer` | ui | 150×172 | — | opaque | 7.3 | đạt | — |
| `ftue_ring` | ui | 170×170 | — | overlay | 5.2 | đạt | — |
| `confetti` | ui | 26×44 | — | icon | 0.3 | đạt | — |
| `bg_ground` | bg | 270×480 | — | opaque | 34.4 | đạt | — |
