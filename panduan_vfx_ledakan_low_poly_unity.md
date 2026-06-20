# Panduan Membuat VFX Ledakan Low Poly di Unity Tanpa Asset Eksternal

Dokumen ini menjelaskan cara membuat efek ledakan **low poly / stylized** untuk game **WUGA / Petualangan IK** hanya menggunakan asset dasar Unity, Particle System, material warna, dan bentuk sederhana.

Efek ini tidak memakai overlay realistis. Jadi lebih cocok untuk game low poly dibandingkan efek ledakan cinematic.

---

## 1. Tujuan Efek

Efek ledakan yang dibuat:

```text
LowPolyExplosionVFX
├── FlashCore
├── FireBurst
├── SmokePuff
├── SparkBurst
├── ShockwaveRing
└── DebrisCube
```

Karakter visual:

```text
- Low poly
- Warna flat
- Sedikit glow cyan
- Tidak realistis
- Cocok untuk efek komputer error / portal digital / ledakan lab WUGA
```

Warna utama:

```text
Cyan       = efek digital / listrik
Orange     = api ledakan
Yellow     = cahaya panas
Grey       = asap
Dark Grey  = asap akhir
White      = flash awal
```

---

## 2. Folder Project

Buat folder khusus untuk VFX:

```text
Assets/Asset/vfx
```

Di dalamnya nanti simpan:

```text
Assets/Asset/vfx
├── Materials
│   ├── M_FlashCore.mat
│   ├── M_FireOrange.mat
│   ├── M_FireYellow.mat
│   ├── M_SmokeGrey.mat
│   ├── M_SparkCyan.mat
│   ├── M_SparkOrange.mat
│   ├── M_ShockwaveCyan.mat
│   └── M_DebrisDark.mat
└── Prefabs
    └── LowPolyExplosionVFX.prefab
```

Kalau belum ada folder `Materials` dan `Prefabs`, buat manual:

```text
Right Click > Create > Folder
```

---

## 3. Buat Material Dasar

Buat beberapa material di:

```text
Assets/Asset/vfx/Materials
```

Cara membuat material:

```text
Right Click > Create > Material
```

---

## 4. Setting Shader Material

Karena project kamu memakai URP, gunakan shader:

```text
Universal Render Pipeline/Particles/Unlit
```

Kalau material untuk particle tidak menemukan shader itu, pakai:

```text
Universal Render Pipeline/Unlit
```

### Setting umum material particle

Untuk material seperti flash, api, spark, dan shockwave:

```text
Shader: Universal Render Pipeline/Particles/Unlit
Surface Type: Transparent
Blending Mode: Additive
Render Face: Both
Base Map: kosong / default putih
```

### Setting material asap

Untuk asap, gunakan:

```text
Shader: Universal Render Pipeline/Particles/Unlit
Surface Type: Transparent
Blending Mode: Alpha
Render Face: Both
Base Map: kosong / default putih
```

---

## 5. Daftar Material dan Warna

Buat material berikut:

| Material | Warna | Fungsi |
|---|---|---|
| M_FlashCore | Putih / Cyan terang | Cahaya ledakan awal |
| M_FireOrange | Orange | Api ledakan |
| M_FireYellow | Kuning | Api panas dekat pusat |
| M_SmokeGrey | Abu-abu | Asap low poly |
| M_SparkCyan | Cyan | Percikan listrik digital |
| M_SparkOrange | Orange terang | Percikan api |
| M_ShockwaveCyan | Cyan transparan | Ring shockwave |
| M_DebrisDark | Abu gelap / navy gelap | Pecahan kubus |

Contoh warna:

```text
M_FlashCore      = #EFFFFF
M_FireOrange     = #FF7A00
M_FireYellow     = #FFD447
M_SmokeGrey      = #666666
M_SparkCyan      = #00D9FF
M_SparkOrange    = #FF8A00
M_ShockwaveCyan  = #00D9FF
M_DebrisDark     = #1A1F2A
```

---

# 6. Membuat Object Utama VFX

Di Hierarchy:

```text
Right Click > Create Empty
```

Rename menjadi:

```text
LowPolyExplosionVFX
```

Reset Transform:

```text
Position: 0, 0, 0
Rotation: 0, 0, 0
Scale: 1, 1, 1
```

Lalu buat child object:

```text
LowPolyExplosionVFX
├── FlashCore
├── FireBurst
├── SmokePuff
├── SparkBurst
├── ShockwaveRing
└── DebrisCube
```

Setiap child dibuat dengan:

```text
Right Click LowPolyExplosionVFX > Effects > Particle System
```

---

# 7. FlashCore

FlashCore adalah kilatan awal ledakan. Efeknya muncul cepat, besar, lalu hilang.

Pilih:

```text
LowPolyExplosionVFX > FlashCore
```

## Main

```text
Duration: 0.2
Looping: Off
Prewarm: Off
Start Lifetime: 0.12
Start Speed: 0
Start Size: 2.8
Start Rotation: Random Between Two Constants
Start Rotation Min: -180
Start Rotation Max: 180
Start Color: White / Cyan terang
Gravity Modifier: 0
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
Awal: Putih/Cyan alpha 1
Akhir: Putih/Cyan alpha 0
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
Material: M_FlashCore
```

---

# 8. FireBurst

FireBurst adalah api low poly yang menyebar dari pusat ledakan.

Pilih:

```text
LowPolyExplosionVFX > FireBurst
```

## Main

```text
Duration: 0.6
Looping: Off
Start Lifetime: Random Between Two Constants
Start Lifetime Min: 0.35
Start Lifetime Max: 0.7
Start Speed: Random Between Two Constants
Start Speed Min: 1.5
Start Speed Max: 3.5
Start Size: Random Between Two Constants
Start Size Min: 0.45
Start Size Max: 1.1
Start Rotation: Random Between Two Constants
Start Rotation Min: -180
Start Rotation Max: 180
Start Color: Orange
Gravity Modifier: 0
Simulation Space: Local
Play On Awake: On
```

## Emission

```text
Rate over Time: 0
Bursts:
- Time: 0
- Count: 18
```

## Shape

```text
Shape: Sphere
Radius: 0.25
```

## Color over Lifetime

Aktifkan:

```text
Color over Lifetime: On
```

Gradient:

```text
Awal: Kuning/Orange alpha 1
Tengah: Orange alpha 0.8
Akhir: Merah gelap/Orange alpha 0
```

## Size over Lifetime

Aktifkan:

```text
Size over Lifetime: On
```

Kurva:

```text
Awal: 0.4
Tengah: 1
Akhir: 0
```

## Renderer

Gunakan billboard dulu agar mudah:

```text
Render Mode: Billboard
Material: M_FireOrange
```

Alternatif low poly lebih kuat:

```text
Render Mode: Mesh
Mesh: Sphere / Icosphere jika tersedia
Material: M_FireOrange
```

Catatan:

```text
Kalau pakai Mesh, bentuk api akan terlihat seperti bola low poly.
Kalau belum paham mesh particle, pakai Billboard dulu.
```

---

# 9. SmokePuff

SmokePuff adalah asap bulat sederhana, bukan asap realistis.

Pilih:

```text
LowPolyExplosionVFX > SmokePuff
```

## Main

```text
Duration: 1.5
Looping: Off
Start Lifetime: Random Between Two Constants
Start Lifetime Min: 0.9
Start Lifetime Max: 1.8
Start Speed: Random Between Two Constants
Start Speed Min: 0.4
Start Speed Max: 1.2
Start Size: Random Between Two Constants
Start Size Min: 0.8
Start Size Max: 1.8
Start Rotation: Random Between Two Constants
Start Rotation Min: -180
Start Rotation Max: 180
Start Color: Grey
Gravity Modifier: 0
Simulation Space: Local
Play On Awake: On
```

## Emission

```text
Rate over Time: 0
Bursts:
- Time: 0.05
- Count: 10
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
Awal: Abu-abu alpha 0.7
Tengah: Abu-abu gelap alpha 0.5
Akhir: Abu-abu gelap alpha 0
```

## Size over Lifetime

Aktifkan:

```text
Size over Lifetime: On
```

Kurva:

```text
Awal: 0.6
Tengah: 1.2
Akhir: 1.8
```

## Renderer

```text
Render Mode: Billboard
Material: M_SmokeGrey
```

Kalau ingin lebih low poly:

```text
Render Mode: Mesh
Mesh: Sphere
Material: M_SmokeGrey
```

---

# 10. SparkBurst

SparkBurst adalah percikan api dan listrik digital.

Pilih:

```text
LowPolyExplosionVFX > SparkBurst
```

## Main

```text
Duration: 0.8
Looping: Off
Start Lifetime: Random Between Two Constants
Start Lifetime Min: 0.3
Start Lifetime Max: 0.8
Start Speed: Random Between Two Constants
Start Speed Min: 4
Start Speed Max: 8
Start Size: Random Between Two Constants
Start Size Min: 0.08
Start Size Max: 0.22
Start Color: Cyan / Orange
Gravity Modifier: 0.5
Simulation Space: Local
Play On Awake: On
```

## Emission

```text
Rate over Time: 0
Bursts:
- Time: 0
- Count: 35
```

## Shape

```text
Shape: Sphere
Radius: 0.15
```

## Color over Lifetime

Aktifkan:

```text
Color over Lifetime: On
```

Gradient:

```text
Awal: Cyan/Orange alpha 1
Akhir: Cyan/Orange alpha 0
```

## Trails

Aktifkan:

```text
Trails: On
```

Setting:

```text
Lifetime: 0.15
Minimum Vertex Distance: 0.1
Width over Trail: mengecil ke ujung
Color over Trail: warna terang ke transparan
```

## Renderer

```text
Render Mode: Billboard
Material: M_SparkCyan
```

Untuk variasi warna:

```text
Buat SparkBurst_Cyan dan SparkBurst_Orange sebagai dua Particle System terpisah.
SparkBurst_Cyan pakai M_SparkCyan.
SparkBurst_Orange pakai M_SparkOrange.
```

---

# 11. ShockwaveRing Tanpa Texture

ShockwaveRing bisa dibuat tanpa gambar ring. Caranya menggunakan Particle System dengan Shape Circle.

Pilih:

```text
LowPolyExplosionVFX > ShockwaveRing
```

## Main

```text
Duration: 0.5
Looping: Off
Start Lifetime: 0.35
Start Speed: 0
Start Size: 0.15
Start Color: Cyan
Gravity Modifier: 0
Simulation Space: Local
Play On Awake: On
```

## Emission

```text
Rate over Time: 0
Bursts:
- Time: 0
- Count: 40
```

## Shape

```text
Shape: Circle
Radius: 0.5
Arc: 360
Emit From: Edge
```

Catatan:

```text
Jika tidak ada Shape Circle, gunakan Shape Sphere dengan Radius kecil.
Namun Circle lebih cocok untuk ring shockwave.
```

## Size over Lifetime

Aktifkan:

```text
Size over Lifetime: On
```

Kurva:

```text
Awal: 1
Akhir: 0
```

## Color over Lifetime

Aktifkan:

```text
Color over Lifetime: On
```

Gradient:

```text
Awal: Cyan alpha 0.8
Akhir: Cyan alpha 0
```

## Renderer

```text
Render Mode: Billboard
Material: M_ShockwaveCyan
```

## Membesarkan Ring

Karena particle hanya diam di circle, cara membuat ring terlihat melebar adalah:

```text
Naikkan Radius pada Shape menjadi 0.5 - 2 secara manual sulit tanpa script.
```

Alternatif tanpa script:

```text
Start Speed: 3
Shape: Circle
Emit From: Edge
```

Dengan ini partikel akan bergerak keluar dari lingkaran, sehingga tampak seperti ring melebar.

Setting alternatif:

```text
Start Speed: 3
Start Lifetime: 0.35
Shape Circle Radius: 0.3
```

---

# 12. DebrisCube

DebrisCube adalah pecahan kubus kecil yang mental keluar. Ini sangat cocok untuk style low poly.

Pilih:

```text
LowPolyExplosionVFX > DebrisCube
```

## Main

```text
Duration: 1
Looping: Off
Start Lifetime: Random Between Two Constants
Start Lifetime Min: 0.6
Start Lifetime Max: 1.2
Start Speed: Random Between Two Constants
Start Speed Min: 2
Start Speed Max: 5
Start Size: Random Between Two Constants
Start Size Min: 0.08
Start Size Max: 0.2
Start Rotation: Random Between Two Constants
Start Rotation Min: -180
Start Rotation Max: 180
Start Color: Dark Grey / Navy
Gravity Modifier: 1
Simulation Space: Local
Play On Awake: On
```

## Emission

```text
Rate over Time: 0
Bursts:
- Time: 0
- Count: 18
```

## Shape

```text
Shape: Sphere
Radius: 0.2
```

## Rotation over Lifetime

Aktifkan:

```text
Rotation over Lifetime: On
```

Isi:

```text
Angular Velocity: Random Between Two Constants
Min: -180
Max: 180
```

## Renderer

Untuk debris, gunakan mesh cube.

```text
Render Mode: Mesh
Mesh: Cube
Material: M_DebrisDark
```

Jika Mesh Cube tidak muncul, buat object cube biasa sebagai alternatif:

```text
GameObject > 3D Object > Cube
```

Lalu gunakan cube kecil manual sebagai pecahan, atau cari built-in mesh Cube di object picker pada Particle Renderer.

---

# 13. Urutan Layer Visual

Agar efek terlihat enak:

```text
SmokePuff      = belakang
ShockwaveRing  = tengah
FireBurst      = tengah-depan
SparkBurst     = depan
DebrisCube     = depan
FlashCore      = paling depan
```

Atur pada masing-masing Particle System:

```text
Renderer > Sorting Fudge
```

Rekomendasi:

```text
SmokePuff: 0
ShockwaveRing: 1
FireBurst: 2
SparkBurst: 3
DebrisCube: 3
FlashCore: 4
```

---

# 14. Setting Cepat Semua Particle

Kalau ingin langsung dicoba tanpa bingung, pakai setting ringkas ini:

| Particle | Lifetime | Speed | Size | Burst |
|---|---:|---:|---:|---:|
| FlashCore | 0.12 | 0 | 2.8 | 1 |
| FireBurst | 0.35 - 0.7 | 1.5 - 3.5 | 0.45 - 1.1 | 18 |
| SmokePuff | 0.9 - 1.8 | 0.4 - 1.2 | 0.8 - 1.8 | 10 |
| SparkBurst | 0.3 - 0.8 | 4 - 8 | 0.08 - 0.22 | 35 |
| ShockwaveRing | 0.35 | 3 | 0.15 | 40 |
| DebrisCube | 0.6 - 1.2 | 2 - 5 | 0.08 - 0.2 | 18 |

---

# 15. Cara Test Tanpa Script

Kamu tidak wajib pakai script.

Agar ledakan langsung muncul saat Play:

```text
Setiap Particle System:
Play On Awake = On
Looping = Off
```

Lalu tekan:

```text
Play
```

Efek akan langsung berjalan dari awal scene.

---

# 16. Kapan Butuh Script?

Script hanya dibutuhkan jika ledakan ingin muncul saat event tertentu, misalnya:

```text
- Saat WUGA menekan tombol komputer
- Saat program error
- Saat portal digital muncul
- Saat musuh terkena hit
- Saat object meledak setelah dialog
```

Kalau hanya untuk test visual, script tidak perlu.

---

# 17. Kenapa Perlu Prefab?

Prefab juga tidak wajib.

Prefab berguna agar efek ledakan yang sudah jadi bisa dipakai ulang.

Contoh:

```text
LowPolyExplosionVFX dipakai di:
- Scene lab
- Scene komputer error
- Scene portal
- Enemy mati
- Objek rusak
```

Cara membuat prefab:

```text
Drag LowPolyExplosionVFX dari Hierarchy ke folder Assets/Asset/vfx/Prefabs
```

Nanti bisa dipakai ulang dengan drag prefab ke scene lain.

---

# 18. Tips agar VFX Terlihat Low Poly

Lakukan:

```text
- Gunakan warna solid
- Gunakan mesh cube untuk debris
- Gunakan sphere low poly untuk smoke/fire jika memungkinkan
- Gunakan cyan untuk efek digital
- Gunakan orange hanya sebagai aksen api
- Buat durasi pendek agar tidak terlihat berat
```

Hindari:

```text
- Texture api realistis
- Smoke overlay cinematic
- Explosion foto realistis
- Terlalu banyak glow
- Warna terlalu detail
```

---

# 19. Tips agar Cocok dengan Tema WUGA

Untuk tema WUGA, ledakan bisa terasa seperti error digital, bukan ledakan bom realistis.

Rekomendasi warna:

```text
FlashCore: putih + cyan
FireBurst: orange kecil
SmokePuff: abu gelap / navy
SparkBurst: cyan dominan
ShockwaveRing: cyan
DebrisCube: navy gelap
```

Rasio warna:

```text
60% cyan/digital
25% orange/api
15% grey/asap
```

---

# 20. Troubleshooting

## Particle tidak muncul

Cek:

```text
Object aktif
Particle System aktif
Play On Awake aktif
Emission Burst sudah diisi
Renderer Material sudah diisi
Camera mengarah ke Particle System
Particle tidak terlalu kecil
```

## Particle terlihat putih semua

Penyebab:

```text
Material belum diganti.
```

Solusi:

```text
Isi Renderer > Material dengan material yang benar.
```

## Particle terlihat kotak

Penyebab:

```text
Material transparansi belum benar.
```

Solusi:

```text
Shader: URP/Particles/Unlit
Surface Type: Transparent
Blending Mode: Additive / Alpha
```

## Asap terlalu terang

Solusi:

```text
Gunakan Blending Mode Alpha, bukan Additive.
Turunkan alpha pada Start Color atau Color over Lifetime.
```

## Efek terlalu realistis

Solusi:

```text
Kurangi jumlah particle.
Gunakan mesh cube/sphere.
Gunakan warna flat.
Hindari texture explosion.
```

## Efek terlalu kecil

Solusi:

```text
Naikkan Start Size.
Naikkan Start Speed.
Naikkan Shape Radius.
Pastikan camera tidak terlalu jauh.
```

---

# 21. Rekomendasi Final untuk Project Kamu

Untuk tahap awal, buat dulu 4 layer ini:

```text
FlashCore
FireBurst
SmokePuff
SparkBurst
```

Setelah sudah enak, baru tambahkan:

```text
ShockwaveRing
DebrisCube
```

Dengan begitu kamu tidak kebanyakan setting di awal.

Versi paling sederhana:

```text
LowPolyExplosionVFX
├── FlashCore
├── FireBurst
├── SmokePuff
└── SparkBurst
```

Ini sudah cukup untuk ledakan low poly di scene WUGA.
