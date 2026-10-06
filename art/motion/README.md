# Motion — CardSlot (Bước 4)

Animation làm bằng **tween code (LitMotion) + ParticleSystem** (D-021), không dùng Spine. Bước 4 giao spec, token và trang xem thử; code thật nằm ở `Game.Views`, làm ở Bước 6.

| File | Là gì |
|---|---|
| `docs/design/animation-list.md` | Danh sách animation (tên = API), pose cuối, hạt, token chuyển động |
| `preview.html` | Trang xem thử: phát từng animation trên sprite đã duyệt (`art/export`). Mở bằng trình duyệt; có "Tắt animation" (pose cuối) và "Chậm ×4" |
| `motion-gameplay-sheet.png`, `motion-ui-sheet.png` | Khung ở 0.15 s và pose cuối của 21 animation (bằng chứng xem bằng mắt) |

Chụp một khung: `preview.html?capture=540&anim=<tên>&t=<giây>[&reduced=1]` với cửa sổ 540×960 (Edge headless, `--allow-file-access-from-files`).

Trang này chỉ để duyệt cảm giác chuyển động. Cách làm trong trang (Web Animations) không phải code game. Game đọc thời lượng từ `DesignTokens.Motion`. Khi chỉnh cảm giác, sửa hằng `MOTION` trong trang và bảng token trong `animation-list.md` cùng lúc.
