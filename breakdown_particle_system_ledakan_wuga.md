# Breakdown 4 Overlay Image untuk Efek Ledakan di Unity Particle System

Dokumen ini menjelaskan cara menerapkan **4 asset overlay ledakan** ke dalam **Particle System Unity**.  
Semua image/texture diasumsikan disimpan di folder:

```text
Assets/Asset/vfx
```

Efek ini cocok untuk game **Petualangan IK / WUGA** dengan tema cyber, digital, biru-cyan, dan sedikit orange api.

---

## 1. Konsep Efek Ledakan

Efek ledakan jangan dibuat dari satu particle saja. Lebih bagus dibuat dari beberapa layer:

```text
ExplosionVFX
├── FlashCore
├── SparkBurst
├── ShockwaveRing
└── SmokeFireBurst
```

Fungsi masing-masing asset:

| Asset Overlay | Fungsi |
|---|---|
| Explosion Flash | cahaya ledakan utama, muncul sangat cepat |
| Spark Burst | percikan api/cyber spark yang menyebar keluar |
| Shockwave Ring | gelombang lingkaran ledakan |
| Smoke Fire Burst | asap + api sebagai ledakan utama yang lebih tebal |

---

## 2. Penamaan File Asset

Simpan 4 image overlay di:

```text
Assets/Asset/vfx
```

Gunakan nama file seperti ini agar rapi:

```text
explosion_flash.png
explosion_sparks.png
explosion_shockwave.png
explosion_smoke_fire.png
```

Struktur folder:

```text
Assets
└── Asset
    └── vfx
        ├── explosion_flash.png
        ├── explosion_sparks.png
        ├── explosion_shockwave.png
        ├── explosion_smoke_fire.png
        ├── M_ExplosionFlash.mat
        ├── M_ExplosionSparks.mat
        ├── M_ExplosionShockwave.mat
        └── M_ExplosionSmokeFire.mat
```

---

## 3. Import Setting Texture

Klik setiap file image overlay di Unity, lalu atur di Inspector:

```text
Texture Type: Sprite (2D and UI)
Alpha Source: Input Texture Alpha
Alpha Is Transparency: On
sRGB: On
Generate Mip Maps: Off
Filter Mode: Bilinear
Compression: None / High Quality
Max Size: 2048
```

Kalau image masih punya background hitam, tidak masalah.  
Nanti background hitam bisa hilang dengan material **Additive**.

---

## 4. Buat Material untuk Setiap Overlay

Buat 4 material di folder:

```text
Assets/Asset/vfx
```

Nama material:

```text
M_ExplosionFlash
M_ExplosionSparks
M_ExplosionShockwave
M_ExplosionSmokeFire
```

Cara membuat:

```text
Right Click di Project > Create > Material
```

---

## 5. Setting Shader Material di URP

Karena project kamu pakai Unity 6 / URP, gunakan shader:

```text
Universal Render Pipeline/Particles/Unlit
```

Untuk setiap material, atur:

```text
Surface Type: Transparent
Blending Mode: Additive
Render Face: Both
Base Map: masukkan texture masing-masing
```

Mapping material:

| Material | Base Map |
|---|---|
| M_ExplosionFlash | explosion_flash.png |
| M_ExplosionSparks | explosion_sparks.png |
| M_ExplosionShockwave | explosion_shockwave.png |
| M_ExplosionSmokeFire | explosion_smoke_fire.png |

Catatan:

```text
Additive = background hitam menjadi tidak terlihat.
Bagian terang seperti api, petir, dan cahaya tetap muncul.
```

Kalau smoke terlalu hilang karena Additive, khusus `M_ExplosionSmokeFire` boleh pakai:

```text
Blending Mode: Alpha
```

Tapi untuk overlay background hitam, biasanya tetap lebih mudah pakai:

```text
Blending Mode: Additive
```

---

## 6. Buat GameObject ExplosionVFX

Di Hierarchy:

```text
Right Click > Create Empty
```

Rename menjadi:

```text
ExplosionVFX
```

Lalu buat 4 Particle System sebagai child:

```text
ExplosionVFX
├── FlashCore
├── SparkBurst
├── ShockwaveRing
└── SmokeFireBurst
```

Caranya:

```text
Right Click ExplosionVFX > Effects > Particle System
```

Buat 4 kali dan rename sesuai nama di atas.

---

# 7. Setting Particle System: FlashCore

FlashCore adalah cahaya ledakan utama. Muncul cepat, besar, lalu hilang.

Pilih object:

```text
ExplosionVFX > FlashCore
```

## Main

```text
Duration: 0.2
Looping: Off
Start Lifetime: 0.12
Start Speed: 0
Start Size: 5
Start Rotation: Random Between Two Constants (-180 sampai 180)
Simulation Space: Local
Play On Awake: On
```

## Emission

```text
Rate over Time: 0
Bursts:
- Time: 0
- Count: 1
```

## Shape

```text
Shape: Sphere
Radius: 0.1
```

## Color over Lifetime

Aktifkan:

```text
Color over Lifetime: On
```

Gradient:

```text
Awal: putih/cyan terang, alpha 1
Akhir: putih/cyan, alpha 0
```

## Size over Lifetime

Aktifkan:

```text
Size over Lifetime: On
```

Kurva:

```text
Awal: 0.5
Tengah: 1.2
Akhir: 0
```

## Renderer

```text
Render Mode: Billboard
Material: M_ExplosionFlash
Sorting Fudge: 2
```

---

# 8. Setting Particle System: SparkBurst

SparkBurst adalah percikan api dan energi biru yang menyebar keluar.

Pilih object:

```text
ExplosionVFX > SparkBurst
```

## Main

```text
Duration: 0.6
Looping: Off
Start Lifetime: Random Between Two Constants (0.25 - 0.6)
Start Speed: Random Between Two Constants (2 - 5)
Start Size: Random Between Two Constants (1.5 - 3)
Start Rotation: Random Between Two Constants (-180 sampai 180)
Simulation Space: Local
Play On Awake: On
```

## Emission

```text
Rate over Time: 0
Bursts:
- Time: 0
- Count: 8
```

## Shape

```text
Shape: Sphere
Radius: 0.2
```

## Color over Lifetime

Aktifkan:

```text
Color over Lifetime: On
```

Gradient:

```text
Awal: orange/cyan, alpha 1
Akhir: orange/cyan, alpha 0
```

## Size over Lifetime

Aktifkan:

```text
Size over Lifetime: On
```

Kurva:

```text
Awal: 0.8
Tengah: 1
Akhir: 0
```

## Renderer

```text
Render Mode: Billboard
Material: M_ExplosionSparks
Sorting Fudge: 3
```

---

# 9. Setting Particle System: ShockwaveRing

ShockwaveRing adalah gelombang lingkaran ledakan. Cocok untuk efek cyber/digital.

Pilih object:

```text
ExplosionVFX > ShockwaveRing
```

## Main

```text
Duration: 0.5
Looping: Off
Start Lifetime: 0.45
Start Speed: 0
Start Size: 1
Start Rotation: Random Between Two Constants (-30 sampai 30)
Simulation Space: Local
Play On Awake: On
```

## Emission

```text
Rate over Time: 0
Bursts:
- Time: 0
- Count: 1
```

## Shape

```text
Shape: Sphere
Radius: 0.1
```

## Color over Lifetime

Aktifkan:

```text
Color over Lifetime: On
```

Gradient:

```text
Awal: cyan putih, alpha 1
Akhir: cyan, alpha 0
```

## Size over Lifetime

Aktifkan:

```text
Size over Lifetime: On
```

Kurva:

```text
Awal: 0.3
Tengah: 2.5
Akhir: 4
```

## Renderer

```text
Render Mode: Billboard
Material: M_ExplosionShockwave
Sorting Fudge: 1
```

Catatan:

```text
Kalau shockwave terlalu besar, kecilkan Start Size atau kurva Size over Lifetime.
Kalau terlalu kecil, naikkan Start Size menjadi 2.
```

---

# 10. Setting Particle System: SmokeFireBurst

SmokeFireBurst adalah asap dan api utama ledakan.

Pilih object:

```text
ExplosionVFX > SmokeFireBurst
```

## Main

```text
Duration: 1.5
Looping: Off
Start Lifetime: Random Between Two Constants (0.8 - 1.4)
Start Speed: Random Between Two Constants (0.2 - 0.8)
Start Size: Random Between Two Constants (3 - 5)
Start Rotation: Random Between Two Constants (-180 sampai 180)
Simulation Space: Local
Play On Awake: On
```

## Emission

```text
Rate over Time: 0
Bursts:
- Time: 0.05
- Count: 3
```

## Shape

```text
Shape: Sphere
Radius: 0.4
```

## Color over Lifetime

Aktifkan:

```text
Color over Lifetime: On
```

Gradient:

```text
Awal: putih/orange, alpha 0.8
Tengah: orange/abu gelap, alpha 0.6
Akhir: abu gelap, alpha 0
```

## Size over Lifetime

Aktifkan:

```text
Size over Lifetime: On
```

Kurva:

```text
Awal: 0.8
Tengah: 1.3
Akhir: 1.8
```

## Renderer

```text
Render Mode: Billboard
Material: M_ExplosionSmokeFire
Sorting Fudge: 0
```

---

# 11. Urutan Layer Efek

Urutan visual yang disarankan:

```text
1. FlashCore        = cahaya utama di depan
2. SparkBurst       = percikan keluar
3. ShockwaveRing    = gelombang melebar
4. SmokeFireBurst   = asap/api agak belakang
```

Kalau di Unity layer terlihat salah, atur:

```text
Particle System > Renderer > Sorting Fudge
```

Saran:

```text
FlashCore Sorting Fudge: 3
SparkBurst Sorting Fudge: 2
ShockwaveRing Sorting Fudge: 1
SmokeFireBurst Sorting Fudge: 0
```

---

# 12. Script untuk Memutar Ledakan

Buat script:

```text
Assets/Asset/vfx/ExplosionVFXPlayer.cs
```

Isi:

```csharp
using UnityEngine;

public class ExplosionVFXPlayer : MonoBehaviour
{
    [Header("Semua Particle System Ledakan")]
    public ParticleSystem[] particles;

    [Header("Hancurkan object setelah selesai?")]
    public bool destroyAfterPlay = false;

    [Header("Waktu sebelum dihancurkan")]
    public float destroyDelay = 3f;

    public void PlayExplosion()
    {
        for (int i = 0; i < particles.Length; i++)
        {
            if (particles[i] == null)
                continue;

            particles[i].Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            particles[i].Play();
        }

        if (destroyAfterPlay)
        {
            Destroy(gameObject, destroyDelay);
        }
    }
}
```

Pasang script ke object:

```text
ExplosionVFX
```

Isi array `particles`:

```text
Element 0: FlashCore
Element 1: SparkBurst
Element 2: ShockwaveRing
Element 3: SmokeFireBurst
```

---

# 13. Cara Menjalankan Ledakan dari Script Lain

Contoh script untuk test:

```csharp
using UnityEngine;

public class TestExplosionTrigger : MonoBehaviour
{
    public ExplosionVFXPlayer explosionVFX;

    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.E))
        {
            explosionVFX.PlayExplosion();
        }
    }
}
```

Cara pakai:

```text
1. Buat Empty Object bernama TestExplosionTrigger.
2. Pasang script TestExplosionTrigger.
3. Drag ExplosionVFX ke field explosionVFX.
4. Play game.
5. Tekan E untuk memunculkan ledakan.
```

---

# 14. Membuat Prefab ExplosionVFX

Setelah semua selesai:

```text
Drag object ExplosionVFX dari Hierarchy ke folder Assets/Asset/vfx
```

Maka akan menjadi prefab:

```text
Assets/Asset/vfx/ExplosionVFX.prefab
```

Nanti prefab ini bisa dipanggil kapan saja.

---

# 15. Cara Spawn Ledakan di Posisi Tertentu

Gunakan script ini kalau ingin memunculkan ledakan dari prefab.

```csharp
using UnityEngine;

public class SpawnExplosion : MonoBehaviour
{
    public GameObject explosionPrefab;

    public void SpawnAtPosition(Vector3 position)
    {
        GameObject explosionObject = Instantiate(
            explosionPrefab,
            position,
            Quaternion.identity
        );

        ExplosionVFXPlayer player = explosionObject.GetComponent<ExplosionVFXPlayer>();

        if (player != null)
        {
            player.PlayExplosion();
        }
    }
}
```

Contoh pemanggilan:

```csharp
SpawnAtPosition(transform.position);
```

---

# 16. Tips agar Cocok dengan Tema WUGA

Karena WUGA bertema cyber/digital, warna ledakan sebaiknya bukan api full orange saja.

Gunakan kombinasi:

```text
Cyan / biru listrik = error digital, portal, listrik
Orange = api ledakan
Putih = flash terang
Abu gelap = asap
```

Rekomendasi:

```text
FlashCore: dominan putih + cyan
SparkBurst: orange + cyan
ShockwaveRing: cyan
SmokeFireBurst: orange + smoke + cyan edge
```

Tambahan efek yang bisa dibuat nanti:

```text
Glitch particles
Pixel debris
Electric arcs
Small blue sparks
Screen shake
Camera flash
```

---

# 17. Masalah yang Sering Terjadi

## Background hitam masih terlihat

Penyebab:

```text
Material belum Additive.
```

Solusi:

```text
Shader: Universal Render Pipeline/Particles/Unlit
Surface Type: Transparent
Blending Mode: Additive
```

---

## Efek terlihat kotak

Penyebab:

```text
Texture tidak punya alpha dan material bukan Additive.
```

Solusi:

```text
Gunakan Additive.
Matikan Alpha Clipping.
Pastikan Base Map terisi texture yang benar.
```

---

## Efek terlalu terang

Solusi:

```text
Turunkan Start Color alpha.
Turunkan warna material.
Kurangi jumlah Burst Count.
Kurangi Start Size.
```

---

## Efek terlalu kecil

Solusi:

```text
Naikkan Start Size.
Naikkan Size over Lifetime.
Dekatkan object ExplosionVFX ke kamera.
```

---

## Efek tidak muncul

Cek:

```text
Particle System aktif.
Play On Awake aktif atau PlayExplosion() terpanggil.
Renderer Material sudah diisi.
Texture masuk ke material Base Map.
Camera menghadap object ledakan.
Object tidak terlalu jauh dari kamera.
```

---

# 18. Setting Cepat Jika Ingin Langsung Jadi

Gunakan angka ini dulu:

```text
FlashCore:
Start Lifetime 0.12
Start Speed 0
Start Size 5
Burst Count 1
Material M_ExplosionFlash

SparkBurst:
Start Lifetime 0.4
Start Speed 3
Start Size 2
Burst Count 8
Material M_ExplosionSparks

ShockwaveRing:
Start Lifetime 0.45
Start Speed 0
Start Size 1.5
Burst Count 1
Material M_ExplosionShockwave

SmokeFireBurst:
Start Lifetime 1.2
Start Speed 0.5
Start Size 4
Burst Count 3
Material M_ExplosionSmokeFire
```

Ini sudah cukup untuk efek ledakan cyber sederhana.
