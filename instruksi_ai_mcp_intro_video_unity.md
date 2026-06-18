# Instruksi AI MCP — Intro Level 1 dengan 4 Video Looping di Unity

## Tujuan

Buat sistem **Intro Level 1** di Unity yang menampilkan 4 section video secara berurutan.  
Setiap video harus **looping terus** sampai pemain menekan **klik kiri mouse**, tombol **Space**, atau tombol UI **Next**.

Alur intro:

```text
Video Scene 1 → klik/Next → Video Scene 2 → klik/Next → Video Scene 3 → klik/Next → Video Scene 4 → klik/Next → masuk ke Level 1
```

---

## Kondisi Project

Project menggunakan:

```text
Unity 6.3 LTS
Scene intro: IntroLevel1
Target scene setelah intro: Level1
```

Asset video berada di folder:

```text
Assets/Asset/VideoScene/
```

Isi video:

```text
scene1.mp4
scene2.mp4
scene3.mp4
scene4.mp4
```

Jika asset masih berupa gambar, ubah dulu menjadi video `.mp4` looping pendek dengan motion effect sederhana seperti mannequin challenge / stop motion ambience.

---

## Struktur GameObject yang Dibutuhkan

Buat struktur di Hierarchy seperti ini:

```text
IntroLevel1
├── Main Camera
├── Directional Light
├── Canvas
│   ├── VideoScreen
│   └── NextButton
├── EventSystem
└── IntroManager
```

Keterangan:

- `VideoScreen` menggunakan komponen **RawImage**.
- `NextButton` digunakan untuk lanjut ke video berikutnya.
- `IntroManager` menyimpan script dan VideoPlayer.

---

## Render Texture

Buat Render Texture baru:

```text
Assets/RenderTextures/IntroVideoRenderTexture.renderTexture
```

Setting Render Texture:

```text
Size: 1920 x 1080
Depth Buffer: 24 bit
Color Format: Default
```

Lalu pasang Render Texture ke:

```text
Canvas > VideoScreen > RawImage > Texture
```

---

## Setup RawImage

Pada object:

```text
Canvas > VideoScreen
```

Gunakan komponen:

```text
RawImage
```

Atur RectTransform:

```text
Anchor: Stretch Full Screen
Left: 0
Right: 0
Top: 0
Bottom: 0
```

Tujuannya agar video memenuhi layar game.

---

## Setup VideoPlayer

Pada object:

```text
IntroManager
```

Tambahkan component:

```text
Video Player
```

Setting VideoPlayer:

```text
Play On Awake: false
Wait For First Frame: true
Loop: true
Render Mode: Render Texture
Target Texture: IntroVideoRenderTexture
Audio Output Mode: Direct
```

Jika ingin audio dikontrol lewat AudioSource, gunakan:

```text
Audio Output Mode: Audio Source
```

Lalu tambahkan component `AudioSource` pada object `IntroManager`.

---

## Script yang Harus Dibuat

Buat script:

```text
Assets/Scripts/IntroVideoController.cs
```

Isi script:

```csharp
using UnityEngine;
using UnityEngine.Video;
using UnityEngine.SceneManagement;

public class IntroVideoController : MonoBehaviour
{
    [Header("Video Player")]
    public VideoPlayer videoPlayer;

    [Header("Daftar Video Intro")]
    public VideoClip[] videoClips;

    [Header("Scene Setelah Intro")]
    public string nextSceneName = "Level1";

    [Header("Input")]
    public bool allowMouseClick = true;
    public bool allowSpaceKey = true;

    private int currentVideoIndex = 0;
    private bool isChangingVideo = false;

    private void Start()
    {
        if (videoPlayer == null)
        {
            Debug.LogError("VideoPlayer belum dipasang di IntroVideoController.");
            return;
        }

        if (videoClips == null || videoClips.Length == 0)
        {
            Debug.LogError("Daftar video intro masih kosong.");
            return;
        }

        PlayCurrentVideo();
    }

    private void Update()
    {
        if (isChangingVideo)
            return;

        if (allowMouseClick && Input.GetMouseButtonDown(0))
        {
            NextVideo();
        }

        if (allowSpaceKey && Input.GetKeyDown(KeyCode.Space))
        {
            NextVideo();
        }
    }

    public void NextVideo()
    {
        if (isChangingVideo)
            return;

        isChangingVideo = true;

        currentVideoIndex++;

        if (currentVideoIndex < videoClips.Length)
        {
            PlayCurrentVideo();
        }
        else
        {
            LoadNextScene();
        }

        isChangingVideo = false;
    }

    private void PlayCurrentVideo()
    {
        videoPlayer.Stop();

        videoPlayer.clip = videoClips[currentVideoIndex];
        videoPlayer.isLooping = true;

        videoPlayer.Prepare();
        videoPlayer.prepareCompleted += OnVideoPrepared;
    }

    private void OnVideoPrepared(VideoPlayer source)
    {
        source.prepareCompleted -= OnVideoPrepared;
        source.Play();
    }

    private void LoadNextScene()
    {
        if (string.IsNullOrEmpty(nextSceneName))
        {
            Debug.LogError("Nama scene tujuan belum diisi.");
            return;
        }

        SceneManager.LoadScene(nextSceneName);
    }
}
```

---

## Cara Pasang Script di Unity

1. Klik object:

```text
IntroManager
```

2. Tambahkan script:

```text
IntroVideoController
```

3. Isi field di Inspector:

```text
Video Player: drag component VideoPlayer dari IntroManager
Video Clips Size: 4
Element 0: scene1
Element 1: scene2
Element 2: scene3
Element 3: scene4
Next Scene Name: Level1
Allow Mouse Click: true
Allow Space Key: true
```

---

## Setup Tombol Next

Pada object:

```text
Canvas > NextButton
```

Di component Button:

1. Buka bagian **On Click()**
2. Klik tombol `+`
3. Drag object `IntroManager` ke slot object
4. Pilih function:

```text
IntroVideoController > NextVideo()
```

Tombol ini harus memanggil fungsi yang sama dengan klik kiri mouse.

---

## Build Settings

Masukkan scene ke Build Settings / Build Profiles:

```text
IntroLevel1
Level1
```

Urutan disarankan:

```text
0. IntroLevel1
1. Level1
```

Pastikan nama di script:

```text
nextSceneName = "Level1"
```

sama persis dengan nama scene di Project.

---

## Setting Input Unity

Jika klik kiri atau tombol Space tidak terbaca, buka:

```text
Edit > Project Settings > Player
```

Cari:

```text
Active Input Handling
```

Ubah menjadi:

```text
Both
```

Lalu restart Unity jika diminta.

---

## Checklist Pengujian

Pastikan hasil akhirnya seperti ini:

- Saat scene `IntroLevel1` dijalankan, video pertama langsung tampil.
- Video pertama looping terus.
- Klik kiri mouse membuat video berpindah ke video kedua.
- Tombol Space membuat video berpindah ke video berikutnya.
- Tombol UI `Next` membuat video berpindah ke video berikutnya.
- Setiap video tetap looping sampai pemain menekan next.
- Setelah video keempat, game masuk ke scene `Level1`.
- Tidak ada error scene tidak ditemukan.
- Video tampil full screen di Game View.
- Audio video tidak bertabrakan saat pindah video.

---

## Catatan Tambahan

Jika ingin ada teks dialog di atas video, tambahkan UI Text / TextMeshPro di Canvas:

```text
Canvas
├── VideoScreen
├── DialogPanel
│   └── DialogText
└── NextButton
```

Nanti script bisa dikembangkan dengan array dialog:

```csharp
public string[] dialogTexts;
```

Lalu saat video berubah, teks dialog juga ikut berubah sesuai index video.

---

## Target Akhir untuk AI MCP

AI MCP harus membuat sistem intro video yang:

1. Memakai `VideoPlayer`.
2. Menampilkan video melalui `RawImage` dan `RenderTexture`.
3. Memiliki 4 video intro.
4. Setiap video berjalan looping.
5. Pindah video dengan klik kiri mouse, Space, atau tombol Next.
6. Setelah video terakhir selesai dilewati, berpindah ke scene `Level1`.
7. Struktur mudah diedit untuk menambah dialog atau video tambahan di kemudian hari.
