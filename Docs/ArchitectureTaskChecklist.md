# Architecture Task Checklist

Tai lieu nay la diem ban giao cho phien lam viec tiep theo. Cap nhat trang thai
o day sau moi task de tranh lap lai cong viec hoac nhay sai thu tu.

## Muc tieu

- Tach gameplay logic khoi visual va input.
- Giu behavior drag-and-drop hien tai trong luc refactor.
- Them he thong manager nho, co ownership ro rang.
- Tao nen tang cho undo, win condition va level flow sau nay.

## Kien truc hien tai

```text
Bootstrap (entry point)
  -> LevelManager (load/reload/next)
     -> LevelRuntimeLoader (spawn level runtime)

DragItem (pointer adapter)
  -> DragController (drag/drop flow)
     -> SlotQueryService (Physics2D query)
     -> PlacementService (kiem tra placement)
     -> MoveManager (command history)
        -> MoveItemCommand
           -> BoardManager (apply board change, BoardChangedEvent)

Item (data + condition) -> ItemView (sprite, scale, opacity)
ItemSlot (board state) -> ItemSlotView (hover indicator, tween)

GameManager / LevelManager / BoardManager / MoveManager -> SingletonMonoBehaviour
BoosterInventoryManager (shared persistent counts)
  -> BoosterManager (consume only after successful Undo / RemoveConditions / Hint)
EventBus -> typed event notification
```

## Da hoan thanh

- [x] Task 1: Audit behavior va tao regression checklist.
- [x] Task 2: Tach `ItemView` khoi `Item` va `DragItem`.
- [x] Task 3: Tach `ItemSlotView` khoi `ItemSlot`.
- [x] Task 4: Tach `DragItem` thanh pointer adapter, `DragController`,
  `SlotQueryService` va `PlacementService`.
- [x] Bo tooltip hien tai: script, event, condition text va UI root da bi tat.
- [x] Them `EventBus`, `GameManager`, `LevelManager`, `BoardManager` va
  `GameState`.
- [x] Sap xep lai folder `Runtime` va `Editor`.
- [x] Build C# da pass sau cac thay doi.

## Task tiep theo

### Task 5 - Board command va undo

- [x] Tao `MoveItemCommand` voi `Execute()` va `Undo()`.
- [x] Chuyen mutation source/target slot tu `DragController` sang command.
- [x] Tao `MoveManager` singleton luu history command.
- [x] Chi `BoardManager` duoc phep apply thay doi board.
- [ ] Kiem tra undo khoi phuc dung: slot, item current slot, position va color.
- [x] Them so luot theo level, tru luot khi move thanh cong va hoan luot khi undo.
- [x] Them dieu kien thua khi het luot va dieu kien thang khi tat ca item nam tren board va thoa man condition.

### Task 6 - Level va game lifecycle

- [x] Tao `Bootstrap` lam entry point ro rang.
- [x] `LevelManager` quan ly load, reload va next level.
- [x] Dung input khi state khong phai `Playing`.
- [x] Them win condition khi rule gameplay duoc chot.
- [x] `Lost` duoc dung khi level het luot.

### Task 7 - UI va test

- [ ] Tao UI cho reset, next level va undo khi feature tuong ung san sang.
- [x] Undo booster chi hoan lai nuoc di gan nhat, hoan luot va khong the undo hai lan lien tiep.
- [x] Remove Conditions booster chi bo qua condition tren item runtime trong mot lan choi.
- [x] Hint booster chon item va hien `solutionLandId` duoc author trong level.
- [x] Selection booster lam toi board, giu cac item noi bat va chan drag trong luc chon.
- [x] Kho booster dung chung duoc luu qua level/app va chi tru khi booster thanh cong.
- [ ] Lam tooltip moi tu dau, khong phuc hoi code tooltip cu.
- [ ] Viet EditMode test cho `PlacementService`, condition va move command.
- [ ] Chay smoke test drag valid, invalid, occupied, outside board, reset va undo.

## Regression checklist

- [ ] Keo item vao slot trong dung type: item snap vao slot va condition refresh.
- [ ] Keo vao slot occupied hoac sai type: item quay lai vi tri cu.
- [ ] Tha ngoai board: item quay lai vi tri cu.
- [ ] Keo item giu duoc offset chuot, scale va opacity thay doi roi khoi phuc.
- [ ] Slot hop le hien indicator active-drag; slot hover hien indicator day du.
- [ ] Slot khong hop le khong hien indicator.
- [ ] Move thanh cong cap nhat color cua tat ca item lien quan.
- [ ] `TestScene` load `Test Level.json` va spawn item o waiting area.

## Quy tac phat trien

- Khong them static event moi vao `DragItem`.
- Dung EventBus cho notification mot-den-nhieu; khong dung no de ra lenh mutate board.
- Manager singleton phai co mot ownership cu the, khong tao manager rong.
- `Item` va `ItemSlot` giu runtime state/rule; `ItemView` va `ItemSlotView` chi lo presentation.
- JSON level giu nguyen cho den khi co nhu cau author level bang Inspector.
