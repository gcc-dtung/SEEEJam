# Task 01 - Checklist Regression Gameplay Hiện Tại

## Phạm vi

Tài liệu này ghi lại behavior drag-and-drop trước khi refactor hệ thống drag.
Đây là mốc để kiểm tra regression, không phải đề xuất thiết kế.

## Khởi tạo runtime

- `LevelRuntimeLoader` tải JSON đã gán trong `Start` khi `loadOnStart` được bật.
- Loader tạo các ô đất, kết nối neighbor, sau đó tạo một wait slot và một item
  kéo thả được cho mỗi cây trong level data.
- Mỗi item bắt đầu tại wait slot tương ứng của nó.
- Nếu camera đang dùng chưa có `Physics2DRaycaster`, hệ thống sẽ tự thêm vào.

## Vòng đời drag

1. Pointer down dừng position tween đang chạy, lưu pointer offset, phóng to
   item, giảm opacity và tắt collider của item.
2. Tooltip điều kiện của item hiện trong thời gian đang kéo.
3. Tất cả slot vào visual state active-drag. Slot hợp lệ hiện indicator; slot
   nằm dưới con trỏ hiện hover indicator đầy đủ.
4. Trong lúc kéo, item đi theo con trỏ và giữ lại grab offset ban đầu.
5. Pointer up trên slot hợp lệ chuyển item vào slot đó và cập nhật màu điều
   kiện của tất cả item.
6. Pointer up ngoài slot, trên slot không hợp lệ, hoặc slot đã có item sẽ đưa
   item về vị trí trước khi kéo mà không đổi board state.
7. Pointer up luôn khôi phục scale, opacity, collider, visual của slot và ẩn
   tooltip.

## Luật đặt item

- Wait slot trống có thể nhận bất kỳ item nào.
- Land slot chỉ nhận item khi `SlotType` của slot trùng với loại slot mà item
  yêu cầu và slot đang trống.
- Item chỉ bị xóa khỏi source slot sau khi target slot được xác nhận hợp lệ.
- Thả item lại source slot của chính nó là hợp lệ và không xóa item khỏi slot.

## Luật condition

- Item trong wait slot hiện màu trắng.
- Item trong land slot hiện màu xanh khi tất cả condition đều đúng.
- Item trong land slot hiện màu đỏ khi bất kỳ condition nào sai.
- Level JSON hiện hỗ trợ: số cây xung quanh, cây cụ thể ở neighbor, edge,
  corner và preference về mùi của neighbor.
- Mỗi lần move thành công cập nhật màu của tất cả item vì một item có thể thay
  đổi condition của các item kề bên.

## Smoke test thủ công

- Mở `TestScene` với `Test Level.json`; tất cả cây xuất hiện ở waiting area.
- Kéo cây vào land slot trống và đúng type; cây snap vào slot và màu của các
  item liên quan được cập nhật.
- Kéo cây qua slot đã occupied; slot không hiện target indicator hợp lệ và cây
  quay về slot cũ khi thả.
- Kéo cây qua slot sai type; cây quay về slot cũ khi thả.
- Kéo cây đã đặt sang slot hợp lệ khác; slot cũ của cây trở thành trống.
- Thả cây ngoài board; cây quay về slot cũ.
- Bắt đầu kéo bất kỳ cây nào; tooltip xuất hiện. Thả cây; tooltip bị xóa.
- Kiểm tra move của `tree_02` có thể ảnh hưởng đến `tree_03`: level test của
  `tree_03` cần kề bên một cây cụ thể và có đúng một cây xung quanh.

## Giới hạn hiện tại

- Chưa có đánh giá win/lose, move counter, undo, reset flow hay game state.
- Drag input, board mutation, visual tween, slot highlight và tooltip
  notification đang coupling qua static Unity event của `DragItem`.
- Build settings hiện trỏ đến `SampleScene`; JSON runtime loader được cấu hình
  trong `TestScene`.
