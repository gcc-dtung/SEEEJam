# Audio Manager

`AudioManager` quản lý toàn bộ nhạc nền (BGM), hiệu ứng âm thanh (SFX), âm lượng và trạng thái mute của game.

## Thiết lập một lần

1. Mở scene có đối tượng `Bootstrap` (hiện tại là scene khởi động của game).
2. Chạy game một lần. `Bootstrap` tự tạo `AudioManager`, vì vậy manager tồn tại xuyên scene và không cần đặt thủ công vào từng scene.
3. Trong khi đang chạy, chọn GameObject `AudioManager` trong Hierarchy. Để cấu hình cố định trong Inspector, tạo một GameObject tên `AudioManager` trong scene Bootstrap, gắn component `AudioManager` vào nó, rồi điền thư viện audio bên dưới.
4. Trong mục **Library**:
   - Thêm BGM vào `Music` và SFX vào `Sound Effects`.
   - Mỗi phần tử cần `Id` duy nhất, `Clip`, `Volume`, `Pitch` (mặc định `1`) và `Loop`.
   - Ví dụ SFX trong project: kéo các clip từ `Assets/Audio/SoundEffect/` vào danh sách, đặt id như `PlantPlaced`, `PlantPicked`, `ButtonClick`, `Win`.
5. (Tuỳ chọn) Tạo Audio Mixer với hai group Music/SFX, sau đó kéo chúng vào **Music Mixer Group** và **Sfx Mixer Group**.

> Không để hai clip trong cùng một danh sách có cùng `Id`; mục trùng sẽ bị bỏ qua và Unity ghi warning.

## Gọi từ code

```csharp
AudioManager.Instance.PlayMusic("MainTheme");
AudioManager.Instance.PlayMusic("MainTheme", 1f); // fade 1 giây
AudioManager.Instance.StopMusic(0.5f);

AudioManager.Instance.PlaySoundEffect("ButtonClick");
AudioManager.Instance.PlaySoundEffect("Win");
```

## Điều khiển từ UI Settings

Gắn các hàm sau vào `Slider.onValueChanged` hoặc `Toggle.onValueChanged`:

```csharp
public void OnMasterVolumeChanged(float value)
{
    AudioManager.Instance.SetMasterVolume(value);
}

public void OnMusicVolumeChanged(float value)
{
    AudioManager.Instance.SetMusicVolume(value);
}

public void OnSfxVolumeChanged(float value)
{
    AudioManager.Instance.SetSoundEffectVolume(value);
}

public void OnMuteChanged(bool value)
{
    AudioManager.Instance.SetMuted(value);
}
```

Giá trị volume dùng khoảng `0` đến `1`. Các tuỳ chọn được lưu bằng `PlayerPrefs` và tự khôi phục ở lần mở game tiếp theo.

## Lưu ý

- BGM chỉ phát một clip tại một thời điểm; đổi bài dùng cross-fade.
- SFX dùng pool mặc định 8 AudioSource để có thể phát chồng nhiều hiệu ứng. Khi pool đầy, source đầu tiên trong pool sẽ được tái sử dụng.
- `SetMasterVolume` ảnh hưởng toàn bộ âm thanh; Music/SFX là hai mức âm lượng riêng.
