# Spec: Tạm dừng và Cài đặt

> v1.0 · Bước 1 · đã duyệt (Phat, 06-10-2026).

**Mục đích.** Cho người chơi dừng, chơi lại, về Home và chỉnh âm thanh/rung.

## Luồng người chơi

- **Pause:** nút II trên TopBar, hoặc app về nền giữa level → `PauseDialog`: **Resume**, **Restart**, **Home**, biểu tượng **Settings**.
  - Restart / Home giữa level: không hỏi lại (đã nằm trong dialog Pause), bỏ `attempt` hiện tại → phát `level_quit`.
- **Settings:** mở từ Pause hoặc Home → ba công tắc: **Sound**, **Music**, **Haptics**. Thay đổi có hiệu lực và được lưu ngay.
- Nút Back (Android): đóng dialog trên cùng. Ở Gameplay không có dialog thì mở Pause. Ở Home thì không làm gì [XÁC NHẬN].

## Dữ liệu lưu

`settings.sound`, `settings.music`, `settings.haptics`.

## Trường hợp biên

- Pause trong lúc hoạt ảnh đang chạy: hoạt ảnh dừng theo pause gate của framework (rule #15), model không đổi.
- Mở Pause khi đang có `LoseDialog`/`WinDialog`: không mở.

## Key text

`pause.title` ("Paused") · `pause.resume` · `pause.restart` · `pause.home` · `settings.title` · `settings.sound` · `settings.music` · `settings.haptics`

## Visual Contract

- Công tắc có hai trạng thái phân biệt được bằng **cả** màu và vị trí núm.
- Pause làm mờ bàn chơi phía sau.

## Acceptance

- [ ] Test: đổi công tắc → giá trị được lưu và đọc lại đúng sau khi khởi động lại.
- [ ] Test: Restart từ Pause bắt đầu `attempt` mới với `buffer_capacity` gốc của level.
